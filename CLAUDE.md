# CLAUDE.md — Trakmark

Shared .NET conventions are imported from the baseline below (source of truth:
[farooq-teqniqly/claude-templates](https://github.com/farooq-teqniqly/claude-templates)).
Rules in this file are project-specific and override the baseline where they conflict.

@CLAUDE-baseline.md

## Project layout

- The web app project is `Trakmark`; other projects use the `Trakmark.<Area>` prefix.
- Register every new project in `Trakmark.slnx`.

## Code conventions (beyond baseline)

- XML-doc exemptions specific to this repo: `[LoggerMessage]` methods in `*.Logging.cs`
  (the message template is self-documenting), EF Core migration files (`Migrations/`,
  auto-generated), and test methods (the name is the specification).
- When test code must call the synchronous EF Core save path intentionally (e.g. `context.SaveChanges()` to exercise a `SavingChanges` interceptor override), add `#pragma warning disable S6966` / `#pragma warning restore S6966` around that call before staging — S6966 fires on any non-awaited EF save call, and the suppression must be in the commit that introduces the test, not applied separately after merge. Do not add a trailing inline comment to a `#pragma warning disable` line — the suppression reason is already captured in the nearby code; an inline restatement violates the "no comments that restate what the code says" rule.
- When an EF Core interceptor overrides both `SavingChangesAsync` and `SavingChanges`, every test scenario written for the async path must have a corresponding test on the sync path — and vice versa. A scenario tested only on one side of the async/sync pair leaves the other branch's code path untested.
- Factory methods that generate a new identity (e.g. `Entity.Create(...)`) must be called **exactly once** per entity being constructed. Calling the same factory in separate passes (e.g., a validation pass and a build pass) produces a different identity on each call. Validate inputs first, then call the factory once and use its result throughout.
- Any service method that saves an entity to a table protected by a unique index must catch `DbUpdateException` and inspect the inner `SqlException` for SQL error numbers **2601** and **2627** (unique-constraint violations). Translate those into a domain-level duplicate result (e.g., a `Conflict` or `DuplicateEntry` discriminated-union case) rather than letting the exception propagate to the caller.

## Read-side query conventions

- **Culture-safe casing on the C# side.** When building a case-insensitive EF query predicate, normalize the C#-side comparison value with `ToUpperInvariant()` — never `ToUpper()`, whose current-culture behavior triggers the Turkish-`i` bug. On the *column* side use `ToUpper()`, which EF Core translates to SQL `UPPER()` (executed by the server, culture-independent). `ToUpperInvariant()` has no EF translation, so never call it on a mapped column inside a `Where`.
- **Keep predicates sargable.** Do not wrap a column already stored in a canonical case in `ToUpper()`/`ToLower()` inside a predicate — the per-row function call is non-sargable and defeats index use. Match the stored canonical form directly (e.g. `c.State == upperInput`, where `State` is always persisted uppercase) rather than `c.State.ToUpper() == upperInput`.
- **Treat blank optional filters as no filter.** For optional filter inputs (search terms, dropdown values), gate the predicate with `string.IsNullOrWhiteSpace(...)` and `Trim()` the value before use — never let a whitespace-only or untrimmed string become a real predicate.

## EF Core configuration

- Unit tests that exercise EF Core interceptors require `Microsoft.EntityFrameworkCore.InMemory` in the test project's `.csproj`. Add it with `dotnet add package Microsoft.EntityFrameworkCore.InMemory` before writing interceptor unit tests; do not reference the in-memory provider without adding the package explicitly.
- EF Core migration scaffolding requires the `dotnet-ef` global tool. Check with `dotnet ef --version` before starting persistence/migration work; if missing, install with `dotnet tool install --global dotnet-ef`. This is a local dev-machine prerequisite, not a project dependency — do not add it to any `.csproj`.
- Before adding a new EF Core migration, first run `dotnet ef migrations add _check --project <migration-project> --startup-project <web-project>` to detect pending model drift. Inspect the generated file: if it contains changes unrelated to your current work, immediately run `dotnet ef migrations remove` and scaffold a separate migration for that drift alone before continuing. Never let unrelated schema drift ride in the same migration as intentional changes.
- The EF Core design-time `IDesignTimeDbContextFactory<T>` must not hard-code a connection string. Read it from an environment variable — e.g. `Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "<localdb-fallback>"`. This is the only place in the codebase where a connection string may live outside user secrets; the env-var-with-fallback pattern is the approved exception.
- In `IEntityTypeConfiguration<T>`, always call `.ValueGeneratedNever()` on any PK that is domain-assigned (i.e., set by a factory method such as `Entity.Create()`). EF Core defaults to `ValueGeneratedOnAdd()` for string PKs, which conflicts with domain-controlled identity and silently discards the provided value.
- Every EF entity must use a DB-generated `int IDENTITY` column (`Id`) as the clustered primary key. Domain-assigned IDs (e.g., `CityId`, `RegisteredUserId`) are mapped as unique alternate keys via `HasAlternateKey`. Never use a domain-assigned string ID as the physical PK — random string PKs fragment the clustered index.
- Do not call `.IsRequired()` on non-nullable string properties in `IEntityTypeConfiguration<T>`. With `Nullable` enabled on all projects, EF Core 10 infers `IS NOT NULL` from the C# nullable annotation — the explicit call is redundant noise.
- Every EF entity must have a dedicated `IEntityTypeConfiguration<T>` class in `Trakmark/Data/Configurations/`. Never configure entities inline in `OnModelCreating`.
- Use `IAuditableEntity` + `AuditInterceptor` for `CreatedByUserId` and `CreatedAt` — never stamp these fields manually in services or components. The `CreatedAt` property must be typed `DateTimeOffset` (not `DateTime`); the interceptor stamps it with `DateTimeOffset.UtcNow`.
- Domain tables must not define FK constraints to ASP.NET Core Identity schema tables (`AspNetUsers`, `AspNetRoles`, etc.). Reference Identity PKs by value (e.g., store the GUID string in an `AccountId` column) but omit any `HasForeignKey`/`HasOne`/`HasMany` EF fluent call targeting an Identity table. Identity schema evolves independently; a FK would couple migrations and violate domain/infrastructure decoupling. If a future section intentionally adds such a constraint, record the rationale in `design.md` as a named decision.

## UI testing (beyond baseline)

- Before committing a Blazor form component, verify that every `maxlength`, `min`, and `max` attribute on input elements matches the corresponding domain constraint exactly — look up the domain type's constant or constructor guard; do not rely on memory.

## Blazor component conventions

- Every `InteractiveServer` or `InteractiveAuto` component with a side-effectful `OnInitializedAsync` must add `if (!RendererInfo.IsInteractive) return;` as the first line — prevents redundant DB calls and service invocations during SSR prerender.
- Any `OnInitializedAsync` that calls a service which may throw must wrap the call in a broad `catch (Exception ex)`, emit a `[LoggerMessage]`-generated entry at `Warning` level or above, and set a user-visible error field — never let it propagate uncaught. Prefer `catch (Exception ex)` over a narrow exception-type filter; unexpected types must not bypass the error display.
- When adding an async-guarded submit button (e.g. a `_isSaving` flag), call `StateHasChanged()` immediately after setting `_isSaving = true` — Blazor Server does not re-render at intermediate `await` points, so without the explicit call the button never visually disables while the request is in flight.
- When a component reloads data into a backing field (e.g. `_page`) and the load can fail, clear that field (set it to `null` or empty) inside the `catch` before setting the error field — otherwise stale rows stay visible beneath the error banner.
- For non-critical JS interop (e.g. cosmetic local-time formatting in `OnAfterRenderAsync`), catch **both** `JSDisconnectedException` **and** `JSException`: a missing or renamed script raises `JSException`, which if uncaught tears down the circuit. Skip the interop call entirely when there is nothing to format (e.g. the page has no rows) rather than invoking it unconditionally.

## Pre-merge checklist

Before merging any branch, resolve all SonarQube warnings using a clean build:

```powershell
dotnet clean .\Trakmark\Trakmark.slnx
dotnet build .\Trakmark\Trakmark.slnx 2>&1 |
    Select-String "warning S\d+" |
    Where-Object { $_.Line -notmatch "Microsoft\.Common" } |
    ForEach-Object { $_.Line.Trim() }
```

- Attempt to fix or suppress each warning. Repeat for up to **3 rounds**.
- If warnings remain after 3 rounds, **block the merge** and write the outstanding items to `docs/sonarqube-warnings-triage.md` (date-stamped entry, branch name, remaining warning list, reason each could not be resolved).

`Trakmark.Domain` line coverage must be **100%** before merging **any change whose diff touches `Trakmark.Domain`**. Run the `coverage-report` skill (or its `Run-Coverage.ps1`) and add tests to close any gap — domain types have no untestable infrastructure dependencies, so an uncovered line means a missing test, not an exemption. Sections/changes that do not modify `Trakmark.Domain` (e.g. persistence, application-layer, or UI-only work) are exempt from this gate — confirm exemption by checking the diff, not by assumption.

### External bot review rounds (Copilot / Qodo)

Automated reviewers re-run on every push, so each fix commit re-triggers a new pass on the changed lines — left unchecked this produces an endless tail of ever-smaller nits. Cap it:

- **Fix Major+ findings immediately**, each verified and tested, however many rounds it takes — real defects are never deferred.
- **Once a round returns only Nit / won't-fix findings (no Major or above), it is the last round.** Fix the accepted nits in **one batched commit**, disposition the rest (reply + resolve as won't-fix with rationale), and **stop** — do not treat the bot re-review triggered by that batched commit as a new round to service.
- Record each round's outcome in the PR review doc (`docs/pr-reviews/PR_REVIEW_<n>.md`) so the round count and stop decision are auditable.
