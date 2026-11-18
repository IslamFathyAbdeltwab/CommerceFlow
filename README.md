<div align="center">

# CommerceFlow

**A modular-monolith e-commerce backend — Clean Architecture, CQRS, and 13 business modules in one deployable.**

![.NET](https://img.shields.io/badge/.NET-11-512BD4?style=flat-square&logo=.NET)
![EF Core](https://img.shields.io/badge/EF%20Core-10-68217A?style=flat-square)
![MediatR](https://img.shields.io/badge/CQRS-MediatR-06B6D4?style=flat-square)
![Tests](https://img.shields.io/badge/tests-1658%20passing-brightgreen?style=flat-square)
![Endpoints](https://img.shields.io/badge/endpoints-151%20operations-blue?style=flat-square)
![Auth](https://img.shields.io/badge/auth-JWT-orange?style=flat-square)

[Overview](#overview) · [Architecture](#architecture) · [Modules](#the-modules) · [Highlights](#engineering-highlights) · [Getting Started](#getting-started) · [API](#api-usage) · [Testing](#testing)

</div>

---

## Overview

CommerceFlow is the backend of an online store: **what is sold** (Catalog), **how much is left** (Inventory), **what a customer picks** (Cart), **what they buy** (Orders), **how they pay** (Payments), **how it ships** (Shipping), and **what happens after** (Returns, Reviews, Notifications) — with Wishlists, Addresses, Promotions and JWT Auth supporting the whole journey.

**The business flow in one line:**

> Catalog → Cart → Order → Payment → Shipment → Delivery → (Return)

Built as a **modular monolith**: one deployable, 13 modules that each own their data and talk through explicit ports — the isolation of microservices without the network cost.

| | |
|---|---|
| **13** business modules | each a full vertical slice (command/query → handler → validator → DTO) |
| **118 routes / 151 operations** | authenticated by default (deny-by-default JWT) |
| **1658 unit tests** | 1022 Application + 636 Domain, 0 failing |
| **360 end-to-end assertions** | every operation exercised over real HTTP, 151/151 coverage |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│ CommerceFlow.API            controllers · JWT · error middleware · DI   │
└────────────┬────────────────────────────────────────────────────────────┘
             │  MediatR  (every request is a command or a query)
┌────────────▼────────────────────────────────────────────────────────────┐
│ CommerceFlow.Application    handlers · validators · DTOs · events      │
│                             (one folder per use-case — vertical slice)  │
└───────┬──────────────────────────────────────┬─────────────────────────┘
        │ depends on                          │ depends on ports only
┌───────▼──────────────────┐         ┌────────▼──────────────────────────┐
│ CommerceFlow.Domain      │         │ CommerceFlow.Abstraction          │
│ entities · business      │◄────────│ interfaces: IUserRepository,      │
│ rules · guards           │         │ ICartRepository, ITokenService…  │
└──────────────────────────┘         └────────▲──────────────────────────┘
                                              │ implemented by
                                     ┌────────┴──────────────────────────┐
                                     │ CommerceFlow.Infrastructure       │
                                     │ EF Core · repositories · PBKDF2 · │
                                     │ JWT signing · RabbitMQ + worker   │
                                     └───────────────────────────────────┘
```

**The dependency rule:** `Domain` knows nothing. `Application` knows `Domain` and the ports. `Infrastructure` implements the ports. `API` is a thin HTTP adapter — controllers bind a request to a MediatR message and return the result; no business logic lives there.

**How one request travels:**

```
POST /api/Orders/{id}/confirm
  → Controller            binds → ConfirmOrderCommand
  → ValidationBehavior    shape rules           → 400 {errors}      (if invalid)
  → Handler               pre-checks state      → 409, nothing saved (if already confirmed)
  → Domain entity         guards the transition → 400 via DomainException (safety net)
  → UnitOfWork            ONE SaveChanges       → atomic commit
  → (integration event)   OrderConfirmed → Notification subscriber in the same commit
  → DTO                   204 / 201 / 200
```

**Constraints come in three deliberate layers:**

| Layer | Responsibility | HTTP answer |
|---|---|---|
| **Validator** (FluentValidation) | *shape only* — lengths, formats, required fields | `400` + `errors` per field |
| **Handler pre-check** | *state & conflicts* — checked **before** any state moves | `409` / `404`, nothing saved |
| **Domain guard** | *safety net* — invariants on the entity itself | `DomainException` → `400` |

**Error contract** — one shape for the entire API:

```json
{ "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "statusCode": 409,
  "detail": "A product with slug 'aurora-phone' already exists." }
```
Validation failures add `"errors": { "Name": ["Product name is required."] }`.

---

## The Modules

Each module answers one business question and owns its tables (single DbContext, module-scoped repositories).

| Module | Business question | Operations |
|---|---|:---:|
| **Catalog** | *What do we sell?* — brands, category tree, products, variants, images, attributes | 43 |
| **Inventory** | *How many do we have?* — warehouses, stock levels, reservations, movements | 24 |
| **Cart** | *What has the customer picked?* — lines, quantities, checkout into an Order | 10 |
| **Order** | *What did they commit to?* — lines and the lifecycle confirm→pay→ship→deliver/cancel | 14 |
| **Payments** | *Did the money arrive?* — charge, refund, pluggable gateways (Fake/Stripe/PayPal) | 5 |
| **Shipping** | *How does it travel?* — shipments and tracking states | 9 |
| **Returns** | *They sent it back* — approve → receive → complete, or reject/cancel | 8 |
| **Reviews** | *What do buyers say?* — submit and moderate (approve/reject/reply) | 6 |
| **Wishlists** | *What did they bookmark?* | 7 |
| **Addresses** | *Where does it ship?* — address book with one default per customer | 7 |
| **Promotions** | *How do we discount?* — percentage/fixed codes, usage limits, windows | 8 |
| **Notifications** | *How do we tell them?* — In-App/Email/SMS/Push with a delivery pipeline | 7 |
| **Auth** | *Who is asking?* — register / login / who-am-I | 3 |

### Catalog — the core module, in detail

| Entity | What it is | Key rules |
|---|---|---|
| **Brand** | the label (*Nova Tech*) | slug unique (→409), lowercase, no spaces; on/off switch, no deletes |
| **Category** | navigation tree (*Electronics → Phones*) | self-parenting forbidden; cannot move under its own descendant; `displayOrder ≥ 0` |
| **Product** | the sellable thing (*Aurora Phone*) | lifecycle `Draft → Published → Archived`; publish needs ≥1 active variant; **archived = read-only forever**; slug unique |
| **ProductVariant** | one purchasable SKU (*Black / 128GB / $599*) | SKU unique per product (UPPERCASE); `price ≥ 0`; compare-at **must be >** price; **a published product must keep ≥1 active variant** |
| **ProductImage** | storage-key reference (not the binary) | only product-level images can be *primary*; first one becomes primary automatically; removing the primary promotes the next |
| **ProductAttribute** | the variant vocabulary (*Color*, *Storage*) | `code` unique; `isVariantDefining` = does it split products into variants? |
| **ProductVariantAttributeValue** | variant × attribute = value (*…× Color = Black*) | upsert semantics; value ≤ 200 |
| **ProductCategory** | product ↔ category link | duplicates refused by the domain |

> **DDD in practice:** children (`ProductVariant`, `ProductImage`, `ProductVariantAttributeValue`) have `internal` constructors — only the `Product` aggregate root creates them, and no child repository exists.

---

## Engineering Highlights

**1. Vertical slices, not horizontal layers per feature**
Each use-case is one folder containing everything it needs:

```
Application/Orders/Commands/ConfirmOrder/
├── ConfirmOrderCommand.cs          # the request (immutable record)
├── ConfirmOrderCommandHandler.cs   # the business logic
└── ConfirmOrderCommandValidator.cs # shape rules only
```

**2. CQRS through a MediatR pipeline**
Commands mutate, queries read. A `ValidationBehavior` runs every FluentValidation validator automatically before a handler sees the request — handlers never re-check shape.

**3. Transactional integrity across modules**
Checkout composes **Carts + Catalog + Orders** ports inside **one `SaveChanges`**: the order row and the cart conversion commit together or not at all. `UnitOfWork` *is* the single save — repositories never commit on their own.

**4. Integration events without a second database**
`ConfirmOrder` publishes `OrderConfirmedIntegrationEvent` through MediatR **before** the save; the `Notifications` subscriber writes the in-app notification in the **same unit of work**. Re-confirming is refused with 409 *before* the event can fire, and a subscriber failure aborts the confirm — no orphan notifications, no double-sends.

**5. RabbitMQ done carefully**
- Topic exchange `commerce.notifications` → queue `commerce.notifications.worker`
- **At-least-once delivery** + idempotent `Mark*` methods: a redelivered message can never move a notification backwards or double-count a failure
- **Ack only after the DB confirm commits**; transient failures `nack(requeue)`; poison payloads logged and dropped
- **Graceful degradation:** with no broker running, the API still works — rows stay `Queued` and drain when the broker appears (auto-reconnect loop, one warning, then silence)
- The worker drives the **same command** the `/sent` endpoint does — one confirm contract

**6. Security**
- **Deny-by-default JWT**: a `FallbackPolicy` protects every endpoint; only `register`, `login` and the OpenAPI document opt out with `[AllowAnonymous]` — a new controller is protected the day it is written
- Passwords: **PBKDF2-SHA256, 100k iterations, 16-byte per-user salt**, fixed-time comparison; stored format carries its parameters for future rotation
- **Anti-enumeration:** unknown email and wrong password return the *same* 401 and the *same* message
- Failed challenges answer in the house `problem+json` shape, not an empty 401

**7. Idempotent state machines**
Notifications treat repeated confirmations as no-ops (`Queued → Sent`, `Sent → Read`…); brands/categories activate/deactivate without error when already in that state. Forward-only transitions are tables in the domain, not scattered `if`s.

---

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET — API & tests on `net11.0`, libraries on `net10.0` |
| Web | ASP.NET Core MVC controllers · native OpenAPI (`/openapi/v1.json`) |
| CQRS / messaging in-process | MediatR 12 |
| Validation | FluentValidation 12 (pipeline behavior) |
| Persistence | EF Core 10 · SQL Server (LocalDB) · 14 migrations |
| Async messaging | RabbitMQ.Client 7 (topic exchange, background consumer) |
| Auth | JWT bearer (HS256) · PBKDF2 password hashing |
| Tests | xUnit 2.9 · Moq · 1658 tests |

---

## Project Structure

```
CommerceFlow/
├── CommerceFlow.slnx
├── src/
│   ├── CommerceFlow.Domain/           # entities, invariants, exceptions — zero dependencies
│   ├── CommerceFlow.Abstraction/      # ports (interfaces), Application.* namespaces
│   ├── CommerceFlow.Application/      # CQRS handlers, validators, DTOs, integration events
│   ├── CommerceFlow.Infrastructure/   # EF Core, repositories, JWT, PBKDF2, RabbitMQ worker
│   └── CommerceFlow.API/              # thin controllers, auth wiring, error middleware
└── tests/
    ├── CommerceFlow.Domain.Tests/       # 636 tests — entity invariants & guards
    └── CommerceFlow.Application.Tests/  # 1022 tests — handlers, validators, events
```

---

## Getting Started

**Prerequisites:** [.NET 11 SDK](https://dotnet.microsoft.com/download) · SQL Server LocalDB (ships with VS) · *(optional)* Docker for RabbitMQ

```bash
git clone https://github.com/IslamFathyAbdteltwab/CommerceFlow.git
cd CommerceFlow
dotnet restore

# create/upgrade the database (LocalDB connection string lives in appsettings.json)
dotnet ef database update --project src/CommerceFlow.Infrastructure --startup-project src/CommerceFlow.API

# run the API
dotnet run --project src/CommerceFlow.API --urls http://localhost:5056
```

* **OpenAPI document:** `http://localhost:5056/openapi/v1.json`
* **JWT settings:** `Jwt` section in `appsettings.json` — the dev signing key must be replaced (user-secrets/environment) before any real deployment.

**Optional — the notification broker:**

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Without it everything still works: notifications stay `Queued` and drain automatically once the broker is up.

---

## API Usage

**Every request after login carries a token** (deny-by-default):

```http
### 1. Register
POST /api/Auth/register
{ "email": "ada@example.com", "fullName": "Ada Lovelace", "password": "Passw0rd123" }
→ 201 { "id": "…" }

### 2. Login
POST /api/Auth/login
{ "email": "ada@example.com", "password": "Passw0rd123" }
→ 200 { "accessToken": "eyJhbGciOi…", "tokenType": "Bearer", "expiresAtUtc": "…" }

### 3. Everything else
GET /api/Products
Authorization: Bearer eyJhbGciOi…
→ 200 { "items": [...], "totalCount": 21 }

### No token → 401
GET /api/Products
→ 401 { "statusCode": 401, "detail": "A valid access token is required." }
```

**Catalog lifecycle in practice:**

```http
POST /api/Brands                {"name":"Nova Tech","slug":"nova-tech"}      → 201
POST /api/Products              {…,"brandId":"…"}                            → 201 (Draft)
POST /api/Products/{id}/variants {"name":"Black 128GB","sku":"AURORA-BLK-128",
                                  "price":599,"compareAtPrice":699}           → 201
POST /api/Products/{id}/publish                                            → 204
POST /api/Products/{id}/publish                                            → 409 "Only a draft product can be published."
POST /api/Products/{id}/variants/{vid}/deactivate   (last active one)       → 409 "A published product must keep at least one active variant."
```

**Conventions:** creates → `201` + `Location`; state transitions → `204`; enums serialize as numbers; `fail`/`reject`/`reply` take a plain JSON string body.

---

## Testing

```bash
dotnet test tests/CommerceFlow.Domain.Tests        # 636 pass
dotnet test tests/CommerceFlow.Application.Tests   # 1022 pass
```

**Unit style** — fast and behaviour-focused:
- Handlers assert **exception type + domain state + `SaveChangesAsync` called once/never** (a failed pre-check must not save)
- Validators are tested rule by rule
- Plain `Assert` + Moq — no fluent assertion libraries hiding intent

**End-to-end** — a full HTTP suite drives **all 151 operations**: auth handshake first, then every module's happy paths, guards (`409`), validations (`400`), not-found (`404`) and cross-module flows (checkout → order → confirm → notification). Result: **360/360 assertions, 151/151 endpoint coverage.**

---

## Design Conventions

* **Single DbContext, modular mind-set** — tables are module-owned; cross-module reads go through explicit ports (e.g. checkout asks Catalog for variant snapshots through `IProductRepository`)
* **No deletes where history matters** — brands, categories and accounts are deactivated, never removed; orders and notifications are append-only
* **Client-generated GUID ids** — no identity-table round-trips
* **One error mapper** — handlers and domain throw typed exceptions; `ExceptionHandlingMiddleware` alone knows HTTP status codes
* **README numbers are real** — counts come from the last full verification run

---

## Roadmap

- [ ] Refresh tokens + token revocation
- [ ] Role-based authorization (`Admin`) on management endpoints
- [ ] Outbox pattern for integration events (exactly-once across process restarts)
- [ ] Real payment provider sandbox integration (Stripe test mode)
- [ ] Full-text catalog search & filtering by `isFilterable` attributes
- [ ] CI pipeline (build → unit tests → E2E suite) + Docker Compose (API + SQL + RabbitMQ)

---

<div align="center">

**Built with Clean Architecture · CQRS · DDD tactical patterns**

*CommerceFlow — Islam Fathy · [github.com/IslamFathyAbdteltwab/CommerceFlow](https://github.com/IslamFathyAbdteltwab/CommerceFlow)*

</div>
