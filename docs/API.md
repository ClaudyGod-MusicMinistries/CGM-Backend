# ClaudyGod Backend API Reference

This is the developer-facing reference for the ClaudyGod Music Ministries backend API — a .NET 8 Clean Architecture REST API. It covers the architecture, the auth model, the two response shapes you'll encounter, and every endpoint across all 18 controllers.

This document is hand-authored rather than generated from Swagger, because the two most operationally important facts about this API — which endpoints require an API key, and the dual error-response shape — are middleware-level behavior that reflection-based tools like Swashbuckle cannot see and that no controller currently annotates.

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

Three policies (`Program.cs`), per-IP, fixed window:

| Policy | Limit | Applies to |
|---|---|---|
| `global` | 100 req / 60s | Every request (default) |
| `auth` | 10 req / 5min | `AuthController` |
| `ai` | 10 req / 1min | `AIController` |

Exceeding a limit returns `429` with a `Retry-After` header.

### API key gate — the single most important thing to know

Every request also passes through `ApiKeyMiddleware`, which requires an `x-api-key` header **unless** the target controller is marked `[PublicEndpoint]`. This is a coarse, non-cryptographic bot/abuse gate — it is **not** the same as `[Authorize]`; a controller can be `[PublicEndpoint]` (no API key needed) while still requiring a JWT bearer token on specific actions (e.g. `AuthController`'s `GET /me`).

| Controller | Requires `x-api-key`? |
|---|---|
| Auth, AI, Booking, Contact, FAQ, Ticket, Volunteer | **No** — `[PublicEndpoint]` |
| Album, Blog, Event, Media, PrayerRequest, Reel, Payment, Store, Subscriber, Youtube, Admin | **Yes** |
| `/health`, `/healthz` | No (framework-level, exempted directly in the middleware) |

If you're integrating a new client against this API and reads are coming back `401 Missing or invalid API key`, this table is why — check whether the controller you're calling is in the public list, and if not, send a valid key from `Security:ApiKeys` config as `x-api-key`.

---

## 3. Response shapes

You will see **two different JSON shapes** depending on how the request failed. A client must handle both.

### `ApiResponse<T>` — normal success/failure path

Used for ordinary success responses and for validation failures raised inside a MediatR pipeline via `ValidationBehaviour`.

```json
{
  "success": true,
  "message": "Event created.",
  "data": { "...": "..." },
  "errors": [],
  "fieldErrors": {}
}
```

On failure, `success: false`, `data: null`, and either `errors: string[]` (general errors) or `fieldErrors: { "fieldName": ["message"] }` (per-field validation errors) is populated.

### RFC7807 `ProblemDetails` — thrown-exception path

Raised by `ExceptionMiddleware` for exceptions thrown outside the normal validation pipeline (`NotFoundException` → 404, `DuplicateResourceException` → 409, `ServiceUnavailableException` → 503, unhandled → 500, and — importantly — the *domain* `ValidationException` also produces this shape, not `ApiResponse`):

```json
{
  "type": "https://httpstatuses.io/422",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more validation errors occurred. See 'errors' for details.",
  "instance": "/api/v1.0/payments/paystack/record",
  "correlationId": "...",
  "errors": { "email": ["A valid email address is required."] }
}
```

**Known contract subtlety**: this shape has no `success` key, and its `errors` field is a field-name → messages map (like `ApiResponse`'s `fieldErrors`), not the flat string array `ApiResponse.errors` uses. A client that only understands `ApiResponse` will silently lose field-level validation messages on this path unless it explicitly detects and normalizes both shapes (check for the absence of a `success` key, or the presence of `title`/`detail`).

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

### EventController — `/events` · requires API key for reads/writes, admin for mutation

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | `` | API key | `?page&pageSize&status` | `PaginatedResult<EventDto>` |
| GET | `/{id}` | API key | — | `EventDetailDto` |
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
| POST | `` | API key | `{name, email, subject, requestText, isConfidential?}` | `{id}` |
| GET | `` | Admin/SuperAdmin | `?page&pageSize&status&includeConfidential` | `PaginatedResult<PrayerRequestDto>` |

Sends a confirmation email ("prayer-received" template) on success — a template send failure is logged, not thrown (best-effort, doesn't fail the request).

### FAQController — `/faqs` · public

| Method | Path | Request | Response |
|---|---|---|---|
| GET | `` | `?category` | flat `FAQDto[]` — **not paginated** |
| GET | `/categories/{category}` | — | flat `FAQDto[]` |

### BlogController — `/blog` · reads require API key, writes admin

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | `` | API key | `?page&pageSize&tag` | `PaginatedResult<BlogPostDto>` (list DTO has no `content`) |
| GET | `/{slug}` | API key | — | `BlogPostDetailDto` (has `content`) |
| POST | `` | Admin/SuperAdmin | title/slug/content/... | `{id}` |
| PUT | `/{id}` | Admin/SuperAdmin | same shape | — |
| DELETE | `/{id}` | Admin/SuperAdmin | — | — |

Slug must match `^[a-z0-9-]+$`.

### AlbumController — `/albums` · requires API key

| Method | Path | Response |
|---|---|---|
| GET | `` | flat `AlbumDto[]` — **not paginated** |

`AlbumDto`: `{id, title, imageUrl?, spotifyUrl?, appleUrl?, youtubeUrl?, deezerUrl?, amazonUrl?, sortOrder, releasedAt?}`. Note: no `artist`, `description`, or `tracks` field — this is a curated links/artwork record, not a full album model.

### ReelController — `/reels` · requires API key

| Method | Path | Response |
|---|---|---|
| GET | `` | `?category&page&pageSize` → flat `ReelDto[]` — **not paginated despite page/pageSize params** |

`ReelDto`: `{id, title, description?, videoUrl, thumbnailUrl?, category, isPublished, publishedAt?, sortOrder}`. Category vocabulary: `featured, sermon, teaching, music_video, live, christmas`. Distinct from `MediaController` — curated highlight reels, not general uploads.

### MediaController — `/media` · reads require API key, writes admin

| Method | Path | Auth | Request | Response |
|---|---|---|---|---|
| GET | `` | API key | `?page&pageSize&type&isPublished` | `PaginatedResult<MediaItemDto>` |
| POST | `` | Admin/SuperAdmin | file upload | `{id}` |

`MediaItemDto`: `{id, title, description?, type, fileName, contentType, fileSizeBytes, publicUrl, thumbnailPath?, artistName?, albumName?, durationSeconds?, isPublished, viewCount, downloadCount, createdAt}`. Filters by `type` (image/video/audio) — **there is no `category` field or filter**, despite that being a common assumption.

### YoutubeController — `/media/youtube` · public

| Method | Path | Request | Response |
|---|---|---|---|
| GET / POST | `/{videoId}` | optional `autoplay, controls, modestBranding` | `{videoId, embedUrl, provider, expiresIn, generatedAt}` |

Validates `videoId` against `^[a-zA-Z0-9_-]{11}$`; builds a `youtube-nocookie.com` embed URL server-side so raw video IDs aren't exposed unnecessarily.

### PaymentController — `/payments` · requires API key

| Method | Path | Request | Response |
|---|---|---|---|
| GET | `/status` | — | `{paystack: bool, zelle: bool, ngnTransfer: bool}` — which methods are currently active |
| POST | `/zelle/validate` | `{transactionId, amount, senderEmail?, senderPhone?, purpose?, orderId?}` | `{id}` — recorded pending, manually reviewed |
| POST | `/ngn-transfer/validate` | multipart form: `{reference, senderName, amount, currency, slipFile}` | `{id}` — recorded pending, manually reviewed |
| POST | `/paystack/record` | `{donorName, donorEmail, amount, currency, reference, message?}` | `{id}` — **server-verified** against Paystack's API before being marked verified |

Zelle and NGN bank transfer have no verification API and are always recorded as pending for manual admin review — this is by design, not a gap. Paystack is verified server-side (amount/currency/status cross-checked against Paystack's `/transaction/verify` endpoint) and returns `503` if the gateway secret key isn't configured yet.

### StoreController — `/store` · requires API key

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

`GET /healthz` (no version prefix, no API key) returns:

```json
{ "status": "healthy", "timestamp": "...", "checks": [{"name": "database", "status": "healthy", "duration": 2.1}, {"name": "redis", "status": "healthy", "duration": 0.4}] }
```

Database/Redis failures report `status: "degraded"` rather than failing the whole check — this is intentional so a load balancer's health probe doesn't take the whole API out of rotation over a transient cache blip. See `Program.cs`'s health check registration for the reasoning.

---

## 6. Known gaps and inconsistencies

Recorded here deliberately, so they're documented rather than rediscovered the hard way:

1. **No Store product catalog.** `StoreController` only exposes `POST /checkout`; there is no `Product` entity, DTO, or `GET` endpoint anywhere in the backend. A frontend product-listing page has nothing to call until this is built. `CreateOrderRequest.items` accepts arbitrary client-supplied line items (name/price/quantity) — the checkout handler cross-checks the submitted `total` against what was actually paid via Paystack, but does **not** validate individual line-item prices against a source of truth, since none exists yet. Building a real catalog (entity + migration + `GET` endpoint, `[PublicEndpoint]`) would close both gaps at once.
2. **Inconsistent pagination.** `Event`, `Media`, `Blog`, and `PrayerRequest` controllers return `PaginatedResult<T>`; `Album`, `Reel`, and `FAQ` return a flat list despite `Reel` accepting `page`/`pageSize` query params that are silently ignored for shaping the response (they still limit the query, just not wrapped in a pagination envelope). Worth standardizing on one approach.
3. **Dual error-response shape.** See §3 — a client must handle both `ApiResponse` and RFC7807 `ProblemDetails` to reliably surface field-level validation errors. Consider having `ExceptionMiddleware` emit `ApiResponse`-shaped bodies for the validation-exception case specifically, since that's the one thrown-exception path a typical form submission actually hits.
4. **`EventDto.flyerImagePath`** is stored/returned as-is, unlike `MediaItemDto.publicUrl` which is resolved to a full public URL via `IFileStorageService` before being returned. A consumer expecting a ready-to-use image URL from `EventDto` may get a bare relative path instead — worth aligning with the Media pattern.
