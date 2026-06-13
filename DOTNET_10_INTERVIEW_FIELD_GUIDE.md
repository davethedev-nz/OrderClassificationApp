# .NET 10 Interview Field Guide (8-Day Cram)

This guide is written for an experienced backend/platform engineer (Python, Docker, AWS, Kafka, CI/CD) transitioning into .NET quickly.

## 0) Current Baseline in This Repository (Observed)

- SDK pin: `global.json` -> .NET SDK `10.0.0`.
- Solution: `OrderClassification.sln` currently includes one project: `OrderClassification.Api`.
- API startup: `OrderClassification.Api/Program.cs` uses Minimal API template + Swagger.
- Packages in API project: `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`.
- Current endpoint: `GET /weatherforecast` (template endpoint).

Keep this as your known-good baseline before layering architecture.

## 1) Fast Mental Mapping (Your Background -> .NET)

| You know | .NET equivalent | Interview phrase |
|---|---|---|
| Flask/FastAPI app startup | `Program.cs` host builder + middleware pipeline | "ASP.NET Core composes request behavior through ordered middleware" |
| Python DI libs/manual wiring | Built-in dependency injection container | "DI is first-class in ASP.NET Core and shapes testability" |
| Pydantic/dataclasses | DTOs + validation + records/classes | "Contracts are explicit and versionable" |
| SQLAlchemy/Alembic | EF Core + Migrations | "Code-first migrations with tracked model snapshots" |
| Kafka producers/consumers | Azure Service Bus (or Kafka on Azure) + handlers | "Separate domain events from integration events" |
| AWS SSM/Secrets Manager | Azure App Configuration + Key Vault | "Externalized config and secret references per environment" |
| Terraform on AWS | Terraform or Bicep on Azure | "IaC parity, cloud-specific resource graph differs" |

## 2) Architecture We Will Build Toward

Target shape by Day 3-5:

- `OrderClassification.Api` - HTTP edge (routing, auth, transport concerns).
- `OrderClassification.Application` - use-cases, orchestration, DTO contracts.
- `OrderClassification.Domain` - entities, value objects, domain rules/events.
- `OrderClassification.Infrastructure` - EF Core, messaging clients, external integrations.
- `OrderClassification.Tests` - unit + integration tests.

Why interviewers like this: boundaries are explicit, test seams are obvious, and infra details stay replaceable.

## 3) Request Lifecycle You Should Be Able to Explain

In order:

1. Kestrel receives HTTP request.
2. Middleware executes in registration order (`Program.cs`).
3. Routing selects endpoint/controller action.
4. Model binding + validation hydrate DTOs.
5. Application use-case executes (domain rules + data access).
6. Response formatting and status code returned.
7. Logging/telemetry spans around the flow.

If asked "where do cross-cutting concerns go?": middleware, filters, or pipeline behaviors (depending on stack).

## 4) Day-by-Day Technical Depth (What to Learn + Why)

### Day 1: Solution structure + startup pipeline

- Learn solution/project boundaries (`.sln`, `.csproj`, project references).
- Add architecture folders/projects and wire DI extension methods.
- Keep API runnable with Swagger and health endpoint.

**What to say in interview:**
"I establish clear dependency direction early so domain/application stay independent from infrastructure."

### Day 2: API contracts + validation + errors

- Replace template endpoint with domain-relevant endpoints.
- Add DTOs, validation, and RFC7807 `ProblemDetails` responses.
- Add centralized exception handling.

**What to say:**
"Transport contracts are separate from domain models to avoid accidental coupling."

### Day 3: Persistence + tests

- Add EF Core provider and first migration.
- Keep async + cancellation token flow from endpoint to data layer.
- Add unit tests for domain logic and integration tests for endpoint behavior.

**What to say:**
"I prefer integration tests around API + persistence boundaries and unit tests for pure business logic."

### Day 4: Security + resilience

- Add JWT authn and policy-based authz.
- Add outbound resilience (`Polly`) where external dependencies exist.
- Ensure config/secret separation by environment.

**What to say:**
"Authentication proves identity, authorization enforces policy at endpoint/use-case boundaries."

### Day 5: Event-driven baseline

- Define integration event contracts.
- Publish event on successful state transition.
- Add consumer with idempotent processing behavior.

**What to say:**
"I publish integration events after persistence is guaranteed, and consumers are idempotent."

### Day 6: Reliability patterns

- Add outbox (or minimal outbox simulation) for durable dispatch.
- Add background worker (`IHostedService`) to flush outbox.
- Add readiness/liveness and observable failure signals.

**What to say:**
"Outbox bridges DB transaction boundaries and at-least-once messaging semantics."

### Day 7: Azure IaC + pipelines

- Add Bicep or Terraform for Azure resources.
- Add CI pipeline (`restore/build/test`) and CD skeleton.
- Use environment parameters and secret references.

**What to say:**
"I keep infra declarative and promote artifacts across environments with immutable builds."

### Day 8: Interview packaging

- Build short demo story: API command -> DB commit -> event publish -> consumer side effect.
- Prepare tradeoff answers and failure-mode reasoning.
- Final doc pass for setup/run/deploy commands.

**What to say:**
"I optimize for clarity of boundaries, reliability under failure, and operability in production."

## 5) Suggested Commands (PowerShell)

```powershell
# from repo root
cd C:\Users\dave-\RiderProjects\WebApplication1

dotnet --info
dotnet restore .\OrderClassification.sln
dotnet build .\OrderClassification.sln
dotnet run --project .\OrderClassification.Api\OrderClassification.Api.csproj
```

```powershell
# create additional projects (example names)
dotnet new classlib -n OrderClassification.Domain
dotnet new classlib -n OrderClassification.Application
dotnet new classlib -n OrderClassification.Infrastructure
dotnet new xunit -n OrderClassification.Tests

dotnet sln .\OrderClassification.sln add .\OrderClassification.Domain\OrderClassification.Domain.csproj
dotnet sln .\OrderClassification.sln add .\OrderClassification.Application\OrderClassification.Application.csproj
dotnet sln .\OrderClassification.sln add .\OrderClassification.Infrastructure\OrderClassification.Infrastructure.csproj
dotnet sln .\OrderClassification.sln add .\OrderClassification.Tests\OrderClassification.Tests.csproj
```

```powershell
# reference wiring (typical direction)
dotnet add .\OrderClassification.Application\OrderClassification.Application.csproj reference .\OrderClassification.Domain\OrderClassification.Domain.csproj
dotnet add .\OrderClassification.Infrastructure\OrderClassification.Infrastructure.csproj reference .\OrderClassification.Application\OrderClassification.Application.csproj
dotnet add .\OrderClassification.Infrastructure\OrderClassification.Infrastructure.csproj reference .\OrderClassification.Domain\OrderClassification.Domain.csproj
dotnet add .\OrderClassification.Api\OrderClassification.Api.csproj reference .\OrderClassification.Application\OrderClassification.Application.csproj
dotnet add .\OrderClassification.Api\OrderClassification.Api.csproj reference .\OrderClassification.Infrastructure\OrderClassification.Infrastructure.csproj
```

## 6) Project-Specific Practices to Follow in This Repo

- Keep `OrderClassification.Api/Program.cs` thin over time (service registration + endpoint wiring only).
- Remove template sample endpoint once real endpoints are in place.
- Use one API style consistently (Minimal APIs or Controllers) to avoid mixed interview narratives.
- Add one coherent feature end-to-end before adding broad abstractions.
- Document each milestone in `INTERVIEW_CRAM_PLAN.md` session handoff.

## 7) Event-Driven Design Notes (Kafka Mindset -> Service Bus Mindset)

- Treat events as immutable facts with explicit versioned contracts.
- Use message IDs and dedupe keys for idempotency.
- Design for at-least-once delivery: consumers must safely reprocess.
- Model dead-letter handling as a first-class operational path.
- Keep retries bounded and observable; avoid infinite poison loops.

## 8) Azure Deployment Notes (AWS Translation Layer)

- App runtime: Azure App Service or Container Apps.
- Messaging: Azure Service Bus (queues/topics).
- Secrets: Key Vault + managed identity.
- DB: Azure SQL or PostgreSQL managed service.
- Monitoring: Application Insights + Log Analytics.

Interview framing:
- "Same platform engineering principles as AWS; service primitives differ, not fundamentals."

## 9) Common Interview Questions You Should Prepare

- Why choose Minimal APIs vs Controllers here?
- How do you enforce dependency direction in the solution?
- When would you use repository pattern vs direct `DbContext` in handlers?
- How do you prevent duplicate event handling side effects?
- Where do you put retries and circuit breakers in this architecture?
- How do you evolve event contracts without breaking consumers?
- How do you run migrations safely in CI/CD?
- What telemetry indicates consumer lag or poison message issues?

## 10) How to Ask for Clarity (Use Your Existing Vocabulary)

Use prompts like:

- "Explain this .NET concept in Python/FastAPI terms."
- "Translate this Azure setup to AWS analogs."
- "Compare Service Bus behavior to Kafka for this exact flow."
- "Give me the interview answer and then the production answer."
- "Show me failure modes first, then happy path."
- "Explain this as if I am reviewing a PR from a junior engineer."

## 11) Session Template (Copy/Paste Each Study Session)

```markdown
## Session Log - YYYY-MM-DD

- Goal:
- Changes made:
- Commands run:
- Tests run + outcome:
- Commit(s):
- What I learned (interview phrasing):
- Next immediate step:
- Open questions for next chat:
```

## 12) Definition of Done for This Cram

You are done when you can:

- Build and run the API from clean checkout.
- Explain the request pipeline and DI lifecycle confidently.
- Demonstrate one persisted command flow with tests.
- Demonstrate one event publish/consume flow with idempotency.
- Explain Azure IaC and CI/CD flow end-to-end in 5 minutes.
- Answer "why this architecture" with explicit tradeoffs.

