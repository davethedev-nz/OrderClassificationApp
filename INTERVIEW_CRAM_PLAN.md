# .NET 10 Interview Cram Plan (8 Days)

Use this as the single source of truth between chat sessions.

- Deep reference guide: `DOTNET_10_INTERVIEW_FIELD_GUIDE.md`

## How to Use This File

- Work on one day at a time.
- Check off tasks as you complete them.
- Create one commit per milestone (or small coherent set of changes).
- At session end, fill in the `Session Handoff` section.

## Repository Scope

- Solution: `OrderClassification.sln`
- Current API project to evolve: `OrderClassification.Api/`
- Secondary sample project (optional/reference): `WebApplication1/`

## Branch + Commit Workflow

- [ ] Create feature branch: `feature/dotnet10-cram`
- [ ] Keep `main` always buildable.
- [ ] Commit after each milestone below.
- [ ] Push at end of each session.

Commit message pattern:

- `dayX: <milestone>`
- Examples:
  - `day1: scaffold layered projects and wire DI`
  - `day3: add EF Core migrations and integration tests`
  - `day5: publish and consume service bus integration event`

## Day-by-Day Checklist

### Day 1 - Foundation and .NET conventions

- [ ] Decide architecture style for this cram (Clean/Layered with vertical slices in API).
- [ ] Create projects/folders for `Domain`, `Application`, `Infrastructure`, and tests.
- [ ] Wire project references and dependency injection in `Program.cs`.
- [ ] Add Swagger/OpenAPI and health checks.
- [ ] Confirm local run and endpoint smoke test.
- [ ] Commit: `day1: foundation structure and startup pipeline`

### Day 2 - API shape, validation, and error handling

- [ ] Add first real endpoint pair (`GET` + `POST`) with DTOs.
- [ ] Add request validation and consistent `ProblemDetails` responses.
- [ ] Add global exception handling middleware.
- [ ] Add structured logging and request correlation.
- [ ] Commit: `day2: endpoint patterns validation and errors`

### Day 3 - Persistence + tests

- [ ] Add EF Core with SQL provider.
- [ ] Model one aggregate/entity and configure `DbContext`.
- [ ] Create and apply first migration.
- [ ] Add integration tests (`WebApplicationFactory`) for API behavior.
- [ ] Add core unit tests for domain/application logic.
- [ ] Commit: `day3: persistence and test baseline`

### Day 4 - Security + resilience

- [ ] Add JWT bearer authentication.
- [ ] Add policy-based authorization on one endpoint.
- [ ] Add secret/config handling for local and non-local environments.
- [ ] Add resilience policies (retry/timeout) where external calls exist.
- [ ] Commit: `day4: authz authn and resilience basics`

### Day 5 - Event-driven baseline

- [ ] Define integration event contract(s) for one business action.
- [ ] Add producer flow from API command -> event publication.
- [ ] Add consumer handler path and idempotency guard.
- [ ] Add retry and dead-letter handling notes/config.
- [ ] Commit: `day5: event producer consumer baseline`

### Day 6 - Reliability and operations

- [x] Add outbox pattern (or minimum viable outbox simulation) for reliability.
- [x] Add background worker (`IHostedService`) for event dispatch if needed.
- [x] Add health/readiness checks and basic telemetry wiring.
- [x] Document failure-mode behavior (duplicate messages, transient errors).
- [x] Commit: `day6: reliability and operational hardening`

### Day 7 - Azure IaC + CI/CD

- [ ] Add Bicep or Terraform baseline for Azure resources.
- [ ] Define per-environment config (`dev`/`test`/`prod`) inputs.
- [ ] Add CI pipeline (build + tests).
- [ ] Add CD pipeline (infrastructure + app deployment strategy).
- [ ] Commit: `day7: azure infrastructure and pipelines`

### Day 8 - Interview packaging and rehearsal

- [ ] Create architecture diagram and event sequence diagram.
- [ ] Add concise project `README.md` with run/test/deploy steps.
- [ ] Write a 5-minute demo script (API -> DB -> event -> consumer).
- [ ] Rehearse common tradeoff answers (Minimal API vs Controllers, outbox, etc.).
- [ ] Commit: `day8: interview demo package and docs`

## Session Handoff (fill each session)

- Date: 2026-06-16
- Completed today: Day 6 reliability hardening, Service Bus transport, outbox dispatcher, health/readiness notes, and the Day 6/Day 7 explainer docs.
- Last commit hash: `2af65a1`
- Next task (single immediate step): Day 7 Azure IaC baseline.
- Blockers/questions: None.

## Quick Recovery Commands

```powershell
git status
git --no-pager log --oneline -n 10
git checkout feature/dotnet10-cram
dotnet build .\OrderClassification.sln
```
