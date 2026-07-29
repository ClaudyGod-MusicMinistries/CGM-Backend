# Production operations

## Required services

- PostgreSQL is the system of record. Apply migrations before starting a new API image.
- Redis backs cross-instance fixed-window rate limits. If Redis is unavailable, the API retains its local limiter and emits an error; `/health/ready` reports Redis as degraded.
- Configure `OpenTelemetry__OtlpEndpoint` with the OTLP HTTP/gRPC endpoint for Seq, Datadog, Grafana, or an OpenTelemetry Collector. Set authentication headers with the standard `OTEL_EXPORTER_OTLP_HEADERS` environment variable.

## Delivery guarantees

Email requests and domain events are inserted into `OutboxMessages` in the same PostgreSQL transaction as their business record. Workers claim rows using PostgreSQL optimistic concurrency, retry with exponential backoff, and release expired claims after two minutes. Delivery is at-least-once: consumers of domain events must be idempotent, and an SMTP message can be duplicated if a process stops after SMTP accepts it but before PostgreSQL records completion.

## Retention

`DataRetentionWorker` runs on startup and then at `Retention:IntervalHours`. Defaults retain audit logs for 365 days and expired refresh tokens, abandoned upload sessions, and processed outbox rows for 30 days. Set these values from environment-specific policy; do not shorten audit retention without legal approval.

## Minimum alerts

Create central alerts for:

- any `Outbox batch failed` or repeated `Outbox message ... delivery attempt ... failed` log;
- Redis health degraded for five minutes or `Distributed rate limiting unavailable` on more than one instance;
- `Scheduled data-retention pass failed`;
- PostgreSQL readiness unhealthy;
- HTTP 5xx rate above 1% for five minutes, or p95 latency above the service objective;
- no telemetry received from an expected production instance for five minutes.

## Integration tests

`ClaudyGod.PostgresIntegration.Tests` starts disposable `postgres:16-alpine`, applies the real migrations, and verifies PostgreSQL-only behavior. Docker must be available locally and in CI. Run with:

```bash
dotnet test tests/ClaudyGod.PostgresIntegration.Tests
```
