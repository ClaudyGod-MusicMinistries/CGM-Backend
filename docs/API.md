# ClaudyGod Backend API Reference

This is the developer-facing reference for the ClaudyGod Music Ministries backend API — a .NET 8 Clean Architecture REST API. It covers the architecture, the auth model, the two response shapes you'll encounter, and every endpoint across all 18 controllers.

This document complements generated Swagger with operational conventions such as the secure fallback authorization policy, stable error codes, and endpoint-specific abuse controls.

---

## 1. Architecture overview

The backend follows Clean Architecture with four projects:

```
ClaudyGod.API            → Controllers, middleware, Program.cs (composition root)
ClaudyGod.Application     → CQRS (MediatR commands/queries), validators, DTOs, interfaces
ClaudyGod.Infrastructure  → EF Core (Postgres), JWT, email, encryption, external services
ClaudyGod.Domain          → Entities, domain exceptions, enums
```

Every write and most reads go through MediatR:

```
HTTP request
  → Controller (thin — deserializes, calls _mediator.Send, wraps in ApiResponse)
    → ValidationBehaviour (runs FluentValidation; throws on failure)
      → LoggingBehaviour
        → Handler (talks to IApplicationDbContext / domain entities / external services)
  → Controller returns ApiResponse<T>.Ok(result)

Any thrown exception (NotFoundException, DomainException, ServiceUnavailableException, etc.)
  → caught globally by ExceptionMiddleware → RFC7807 ProblemDetails response (see §3)
```

16 of 18 controllers follow this pattern exactly (thin controller, MediatR handler does the work). `AIController` and `YoutubeController` also follow it as of the latest hardening pass.

---

## 2. Auth model

### JWT + refresh cookie

- **Access token**: JWT bearer, HMAC-SHA256, ~60 minute expiry (`Jwt:AccessTokenExpiryMinutes`). Sent as `Authorization: Bearer <token>`.
- **Refresh token**: opaque random string, delivered as an **HttpOnly, Secure, SameSite=Strict cookie** (never in the response body) with a ~7-14 day expiry. Rotated on every use; reuse of a revoked token is detected and logged.
- Login/register/refresh all return the same shape:

```json
{
  "success": true,
  "message": "...",
  "data": {
    "accessToken": "eyJhbGciOi...",
    "role": "User",
    "accessTokenExpiresAt": "2026-07-22T15:30:00Z"
  },
  "errors": [],
  "fieldErrors": {}
}
```

### Rate limiting

Named policies (`Program.cs`), per-IP, fixed window:

| Policy | Limit | Applies to |
|---|---|---|
| `global` | 100 req / 60s | Every request (default) |
| `auth` | 10 req / 5min | `AuthController` |
| `ai` | 10 req / 1min | `AIController` |
| `comments` | 8 req / 10min | anonymous comments and reactions |
| `subscription` | 5 req / 1hour | subscribe and unsubscribe |
| `public-form` | 10 req / 10min | booking, contact, prayer, volunteer, ticket and YouTube helpers |
| `commerce` | 5 req / 5min | checkout and payment recording |

Exceeding a limit returns `429` with a `Retry-After` header.

### Authorization model — secure by default

Authorization uses two explicit endpoint classes:

- `[PublicEndpoint]` or `[AllowAnonymous]`: intentionally anonymous. Public mutations also require a named rate-limit policy.
- No anonymous marker: the global fallback policy requires an authenticated `Admin` or `SuperAdmin` JWT.

Audit identity is derived only from validated JWT claims. Caller-supplied `x-actor-id` and `x-actor-email` headers are never trusted.

---

## 3. Response contracts

### `ApiResponse<T>` — normal success/failure path

Used for successful application responses.

```json
{
  "success": true,
  "message": "Event created.",
  "data": { "...": "..." },
  "errors": [],
  "fieldErrors": {}
}
```

### RFC7807 `ProblemDetails` — every error path

All middleware, authentication, authorization, model-binding, rate-limit, validation, domain, and unexpected failures use `application/problem+json`. `ExceptionMiddleware` maps application exceptions consistently (`NotFoundException` → 404, `DuplicateResourceException` → 409, validation → 422, `ServiceUnavailableException` → 503, unexpected → 500):

```json
{
  "type": "https://httpstatuses.io/422",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more validation errors occurred. See 'errors' for details.",
  "instance": "/api/v1.0/payments/paystack/record",
  "code": "VALIDATION_FAILED",
  "traceId": "...",
  "correlationId": "...",
  "errors": { "email": ["A valid email address is required."] }
}
```

Clients should branch on `code`, use `detail` for readable guidance, map the optional field-name-to-messages `errors` extension to forms, and provide `correlationId` to support.

---

## 4. Endpoint reference

All routes are prefixed `/api/v{version}` (currently `v1.0`).

### AuthController — `/auth` · public · rate-limited (`auth`)

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | `/register` | none | `{username, email, password}` | `AuthResponseDto` |
| POST | `/login` | none | `{email, password}` | `AuthResponseDto` |
| POST | `/refresh` | refresh cookie | — | `AuthResponseDto` |
| POST | `/logout` | refresh cookie | — | — |
| GET | `/me` | Bearer JWT | — | current user profile |

Password policy (register): 8+ chars, upper/lower/digit/special character required.

### AIController — `/ai` · public · rate-limited (`ai`)

| Method | Path | Request | Response |
|---|---|---|---|
| POST | `/chat` | `{message, history?}` | `{reply}` |
| POST | `/prayer` | `{message}` | `{reply}` |
| POST | `/booking-help` | `{message}` | `{reply}` |

Returns `503` (ProblemDetails, `ServiceUnavailableException`) if `AIProvider:ApiKey` isn't configured.

### EventController — `/events` · public reads, admin mutation

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | `` | none | `?page&pageSize&status` | `PaginatedResult<EventDto>` |
| GET | `/{id}` | none | — | `EventDetailDto` |
| POST | `` | Admin/SuperAdmin | `CreateEventCommand` fields | `{id}` |
| PATCH | `/{id}/status` | Admin/SuperAdmin | `{status}` | — |

`EventDto`: `{id, title, description?, venue?, startDate, endDate?, totalCapacity, reservedCount, availableSeats, isFree, ticketPrice?, status, flyerImagePath?, createdAt}`.

### TicketController — `/tickets` · public reads exempt, writes public

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | `` | none | `ReserveTicketRequest` (below) | `{id}` |
| GET | `` | Admin/SuperAdmin | — | ticket list |

`ReserveTicketRequest`: `{eventId, firstName, lastName, email, phone, quantity=1}`. Returns a `confirmationCode` in the persisted `TicketDto`.

### BookingController — `/bookings` · public

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | `` | none | `CreateBookingRequest` | `{id}` |
| GET | `` | Admin/SuperAdmin | — | booking list |
| PATCH | `/{id}/status` | Admin/SuperAdmin | `{status, adminNotes?}` | — |

`CreateBookingRequest`: `{firstName, lastName, email, phone, countryCode, organization, orgType, eventType, eventDetails, eventDate, addressLine1, addressLine2?, city, state, zipCode, country, agreeTerms}`.

### ContactController — `/contacts` · public

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | `` | none | `{name, email, message}` | `{id}` |
| GET | `` | Admin/SuperAdmin | — | message list |

### VolunteerController — `/volunteers` · public

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | `` | none | `RegisterVolunteerRequest` | `{id}` |
| GET | `` | Admin/SuperAdmin | — | volunteer list |

`RegisterVolunteerRequest`: `{firstName, lastName, email, role, reason}`. `role` ∈ `BackupSinger, Protocol, Media, Security, Vocalist, Others`.

### PrayerRequestController — `/prayer-requests` · POST public, GET admin

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | `` | none | `{name, email, subject, requestText, isConfidential?}` | `{id}` |
| GET | `` | Admin/SuperAdmin | `?page&pageSize&status&includeConfidential` | `PaginatedResult<PrayerRequestDto>` |

Sends a confirmation email ("prayer-received" template) on success — a template send failure is logged, not thrown (best-effort, doesn't fail the request).

### FAQController — `/faqs` · public

| Method | Path | Request | Response |
|---|---|---|---|
| GET | `` | `?category` | flat `FAQDto[]` — **not paginated** |
| GET | `/categories/{category}` | — | flat `FAQDto[]` |

### BlogController — `/blog` · public reads, admin writes

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | `` | none | `?page&pageSize&tag` | `PaginatedResult<BlogPostDto>` (list DTO has no `content`) |
| GET | `/{slug}` | none | — | `BlogPostDetailDto` (has `content`) |
| POST | `` | Admin/SuperAdmin | title/slug/content/... | `{id}` |
| PUT | `/{id}` | Admin/SuperAdmin | same shape | — |
| DELETE | `/{id}` | Admin/SuperAdmin | — | — |

Slug must match `^[a-z0-9-]+$`.

### AlbumController — `/albums` · public reads

| Method | Path | Response |
|---|---|---|
| GET | `` | flat `AlbumDto[]` — **not paginated** |

`AlbumDto`: `{id, title, imageUrl?, spotifyUrl?, appleUrl?, youtubeUrl?, deezerUrl?, amazonUrl?, sortOrder, releasedAt?}`. Note: no `artist`, `description`, or `tracks` field — this is a curated links/artwork record, not a full album model.

### ReelController — `/reels` · public reads

| Method | Path | Response |
|---|---|---|
| GET | `` | `?category&page&pageSize` → flat `ReelDto[]` — **not paginated despite page/pageSize params** |

`ReelDto`: `{id, title, description?, videoUrl, thumbnailUrl?, category, isPublished, publishedAt?, sortOrder}`. Category vocabulary: `featured, sermon, teaching, music_video, live, christmas`. Distinct from `MediaController` — curated highlight reels, not general uploads.

### MediaController — `/media` · public reads, admin writes

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | `` | none | `?page&pageSize&type&isPublished` | `PaginatedResult<MediaItemDto>` |
| POST | `` | Admin/SuperAdmin | file upload | `{id}` |

`MediaItemDto`: `{id, title, description?, type, fileName, contentType, fileSizeBytes, publicUrl, thumbnailPath?, artistName?, albumName?, durationSeconds?, isPublished, viewCount, downloadCount, createdAt}`. Filters by `type` (image/video/audio) — **there is no `category` field or filter**, despite that being a common assumption.

### YoutubeController — `/media/youtube` · public

| Method | Path | Request | Response |
|---|---|---|---|
| GET / POST | `/{videoId}` | optional `autoplay, controls, modestBranding` | `{videoId, embedUrl, provider, expiresIn, generatedAt}` |

Validates `videoId` against `^[a-zA-Z0-9_-]{11}$`; builds a `youtube-nocookie.com` embed URL server-side so raw video IDs aren't exposed unnecessarily.

### PaymentController — `/payments` · public payment callbacks with verification and rate limits

| Method | Path | Request | Response |
|---|---|---|---|
| GET | `/status` | — | `{paystack: bool, zelle: bool, ngnTransfer: bool}` — which methods are currently active |
| POST | `/zelle/validate` | `{transactionId, amount, senderEmail?, senderPhone?, purpose?, orderId?}` | `{id}` — recorded pending, manually reviewed |
| POST | `/ngn-transfer/validate` | multipart form: `{reference, senderName, amount, currency, slipFile}` | `{id}` — recorded pending, manually reviewed |
| POST | `/paystack/record` | `{donorName, donorEmail, amount, currency, reference, message?}` | `{id}` — **server-verified** against Paystack's API before being marked verified |

Zelle and NGN bank transfer have no verification API and are always recorded as pending for manual admin review — this is by design, not a gap. Paystack is verified server-side (amount/currency/status cross-checked against Paystack's `/transaction/verify` endpoint) and returns `503` if the gateway secret key isn't configured yet.

### StoreController — `/store` · public catalog/checkout, admin catalog mutation

| Method | Path | Request | Response |
|---|---|---|---|
| POST | `/checkout` | `CreateOrderRequest` (below) | `OrderDto` |

`CreateOrderRequest`: `{items: LineItem[], shipping: ShippingInfo, shippingMethod, paymentMethod, subtotal, shippingCost, total, currency, paystackRef?}`. **There is no `GET` products endpoint on this controller** — see §6, Known Gaps.

### SubscriberController — `/subscribers` · public

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| POST | `` | none | `{name, email}` | `{id}` |
| DELETE | `/unsubscribe` | none | `{email, token}` | — |
| GET | `` | Admin/SuperAdmin | — | subscriber list |

### AdminController — `/admin` · Admin/SuperAdmin only

| Method | Path | Response |
|---|---|---|
| GET | `/dashboard` | `DashboardStatsDto` — counts across subscribers, bookings, volunteers, events, tickets, prayer requests, contacts, media, blog posts |

---

## 5. Health check

`GET /healthz` (no version prefix, no authentication) returns:

```json
{ "status": "healthy", "timestamp": "...", "checks": [{"name": "database", "status": "healthy", "duration": 2.1}, {"name": "redis", "status": "healthy", "duration": 0.4}] }
```

Operational probes are separated by purpose:

- `GET /health/live` checks only whether the process can serve HTTP.
- `GET /health/ready` checks PostgreSQL and Redis. PostgreSQL failure is unhealthy and returns `503`; Redis failure is degraded because it is an optional cache.
- `GET /healthz` is a compatibility alias for readiness and has the same failure semantics.

---

## 6. Enforced integrity contracts

- Checkout treats the product catalog as the only source of truth for names, prices, images, descriptions, and availability. Client-supplied display fields are ignored. Totals are recomputed server-side, Paystack payments are verified against reference/amount/currency, and a payment reference can fund only one order.
- Product inventory and event ticket capacity use PostgreSQL `xmin` optimistic concurrency tokens. Database constraints independently prevent negative inventory, oversold events, invalid ratings, and inconsistent order totals.
- Refresh tokens are random bearer credentials delivered only through a Secure, HttpOnly, SameSite=Strict cookie. Only SHA-256 hashes are persisted. Tokens rotate on refresh and reuse revokes the user's active token family.
- All error responses use `application/problem+json` with RFC 7807 fields. Validation failures include an `errors` field-name-to-messages extension and every request carries a validated correlation ID.
- Paginated endpoints enforce `page >= 1` and `1 <= pageSize <= 100`. Reels return the same `PaginatedResult<T>` envelope as other paginated resources.
