# FCMB Assessment – C#

A modular-monolith banking API built on **.NET 10** and orchestrated locally with **.NET Aspire**. It handles user sign-up (backed by Keycloak for identity) and fund transfers, with a customer rewards engine, and modules that talk to each other through the transactional outbox/inbox pattern over Azure Service Bus.

## Tech stack

| Concern | Technology |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core Minimal APIs |
| Orchestration | .NET Aspire 13 (AppHost) |
| Identity | Keycloak (JWT bearer auth, admin API via Refit) |
| Persistence | EF Core 10 + SQL Server (one schema per module) |
| Messaging | MassTransit on Azure Service Bus (emulator locally) |
| Background jobs | Quartz.NET (outbox/inbox processors) |
| Validation | FluentValidation (applied via a decorator) |
| Resilience | `Microsoft.Extensions.Http.Resilience` (retry, circuit breaker, timeouts) |
| Observability | Serilog, OpenTelemetry (OTLP traces and logs), health checks |
| API docs | OpenAPI + Scalar, URL-segment API versioning |
| Code quality | SonarAnalyzer, `AnalysisMode=All`, warnings treated as errors |

## Solution layout

```
Orchestration/
  Fcmb.Assessment.CSharp.Api.AppHost/   Aspire host: Keycloak, Service Bus emulator, API
src/
  API/
    Fcmb.Assessment.CSharp.Api/         Composition root: Program.cs, middleware, OpenAPI, migrations
  Common/
    *.Common.Domain                     Entity base, Result/Error, domain events
    *.Common.Application                CQRS abstractions, decorators, outbox/inbox models
    *.Common.Infrastructure             Auth, MassTransit, Quartz, OTel, audit log, resilience
    *.Common.Presentation               IEndpoint, ApiResponse envelope, endpoint discovery
  Modules/
    Users/                              Domain, Application, Infrastructure, Presentation, IntegrationEvents
    Transactions/                       Domain, Application, Infrastructure, Presentation
```

Each module follows Clean Architecture layering and owns its own `DbContext` and database schema (`users`, `transactions`). Modules don't reference each other's internals. They communicate only through **integration events** (for example `UserSignedUpIntegrationEvent`, published by Users and consumed by Transactions).

### How events flow

1. A domain entity raises a domain event (such as `User.Create` raising `UserSignedUpDomainEvent`).
2. `InsertOutboxMessagesInterceptor` saves the event to the module's outbox table in the same transaction.
3. A Quartz `ProcessOutboxJob` dispatches outbox messages to domain event handlers, which publish integration events through MassTransit to the `domain-events-topic`.
4. Consumers write incoming events to the receiving module's inbox, and `ProcessInboxJob` hands them to idempotent integration event handlers.

Retries and dead-lettering apply to both outbox and inbox. You configure them per module in `modules.<name>.json`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (for the Keycloak and Azure Service Bus emulator containers)
- A reachable SQL Server instance (the AppHost does **not** provision one)
- Optional: the Aspire CLI (`dotnet tool install -g aspire.cli`)

## Getting started

### 1. Configure the API

Set these values in `src/API/Fcmb.Assessment.CSharp.Api/appsettings.Development.json` (git-ignored) or as environment variables:

```jsonc
{
  "ConnectionStrings": {
    "Database": "Server=localhost,1433;Database=FcmbAssessment;User Id=sa;Password=<pwd>;TrustServerCertificate=True"
  },
  "Authentication": {
    "Audience": "account",
    "MetadataAddress": "http://localhost:8080/realms/fcmb-assessment-csharp/.well-known/openid-configuration",
    "TokenValidationParameters": {
      "ValidIssuers": [ "http://localhost:8080/realms/fcmb-assessment-csharp" ]
    },
    "RequireHttpsMetadata": false
  },
  "KeyCloak": {
    "BaseUrl": "http://localhost:8080",
    "ClientId": "<confidential-client-id>",
    "ClientSecret": "<client-secret>",
    "TokenUrl": "http://localhost:8080/realms/fcmb-assessment-csharp/protocol/openid-connect/token",
    "AuthorizationUrl": "http://localhost:8080/realms/fcmb-assessment-csharp/protocol/openid-connect/auth",
    "HealthUrl": "http://localhost:8080/health/"
  }
}
```

Aspire injects the Service Bus connection string (`ConnectionStrings:azureservicebus`) automatically.

### 2. Set up Keycloak

When the AppHost runs, it starts Keycloak on **http://localhost:8080** with a persistent data volume. Then:

1. Create a realm named **`fcmb-assessment-csharp`**. The admin API route in `IKeyCloakClient` expects this name.
2. Create a confidential client with **service accounts enabled**, and give its service account the `realm-management` → `manage-users` role. Put its ID and secret in `KeyCloak:ClientId` and `KeyCloak:ClientSecret`.

The Aspire dashboard shows the Keycloak admin credentials under the `keycloak` resource.

### 3. Run

```bash
# Through Aspire (recommended): starts Keycloak, Service Bus emulator and the API
dotnet run --project Orchestration/Fcmb.Assessment.CSharp.Api.AppHost
# or
aspire run
```

In the `Development` environment, the API applies EF Core migrations for both modules on startup.

| URL | Purpose |
| --- | --- |
| https://localhost:7025/scalar | Interactive API reference (Development only) |
| https://localhost:7025/openapi/v1.json | OpenAPI document |
| https://localhost:7025/health | Health check (UI client JSON format) |
| Aspire dashboard (URL printed on start) | Logs, traces and resources |

## API

All routes are versioned under `api/v{version}/`. Responses use a common `ApiResponse` envelope (`isSuccess`, `statusCode`, `message`, `error`, `data`), and errors come back as Problem Details.

### `POST /api/v1/users/sign-up`

Registers a user in Keycloak and in the Users module. Returns `201 Created`.

```json
{
  "firstName": "Ada",
  "lastName": "Obi",
  "phoneNumber": "+2348012345678",
  "dob": "1990-05-14",
  "userType": "Individual",
  "email": "ada.obi@example.com",
  "password": "Str0ng!Pass"
}
```

Validation rules: names up to 50 characters, an E.164 phone number, an age of at least 18, a unique email and phone number, and a password of 8+ characters with upper case, lower case, a digit and a symbol. `userType` is `Individual` or `Corporate`.

### `POST /api/v1/transactions/transfer` 🔒

Requires a Keycloak bearer token.

```json
{
  "transferType": "IntraBank",
  "sourceAccount": "0123456789",
  "destinationAccount": "9876543210",
  "amount": 50000
}
```

Both account numbers must be distinct 10-digit numbers, and `amount` must be greater than 0.

## Rewards rules

`Reward.ProcessTransfer` in the Transactions domain implements these rules. Counters reset at the start of each calendar month.

| | Individual | Corporate |
| --- | --- | --- |
| Qualifying transfer | amount > ₦25,000 | amount > ₦120,000 |
| Points per qualifying transfer | 2 | 3 |
| Reward | ₦1,500 airtime on the 8th qualifying transfer | ₦7,500 cashback at 90+ points (once per month) |
| Tenure bonus | Double points on the first 4 qualifying transfers for customers registered 4+ years | Same |

## Configuration reference

| File | Contents |
| --- | --- |
| `appsettings.json` | Serilog, connection strings, JWT authentication, Keycloak URLs |
| `modules.users.json` | Users outbox/inbox job settings, Keycloak client credentials |
| `modules.transactions.json` | Transactions outbox/inbox job settings |

`modules.<name>.Development.json` files load as optional overrides.

Outbox/inbox settings: `IntervalInSeconds`, `BatchSize`, `MaxRetries`, `RetryDelaySeconds`.

## Development

```bash
dotnet build          # warnings fail the build: keep analyzers happy
```

Add a migration (example for the Users module):

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/Users/Fcmb.Assessment.CSharp.Modules.Users.Infrastructure \
  --startup-project src/API/Fcmb.Assessment.CSharp.Api \
  --context UsersDbContext \
  --output-dir Database/Migrations
```

Package versions are managed centrally in `Directory.Packages.props`. Shared build settings live in `Directory.Build.props`.

## Status / known gaps

This is still in progress:

- `TransferUseCase.Handler` only records a `Transaction`. It doesn't yet debit or credit accounts or apply rewards.
- `UserSignedUpIntegrationEventHandler` in the Transactions module (which creates the customer, account and reward) throws `NotImplementedException`.
- Keycloak admin tokens aren't cached yet, so each admin call fetches a new token.
- The AppHost doesn't provision SQL Server, so you must supply it separately.
