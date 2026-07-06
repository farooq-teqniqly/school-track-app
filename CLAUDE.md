# CLAUDE.md — dotnet-otel

## Language and spelling

Use U.S. English in all prose, comments, commit messages, and docs.

## Project structure and naming

- One project per layer/concern; name with the `Trakmark.<Area>` prefix (e.g. `Trakmark.Domain`). The web app project is `Trakmark`.
- Each project's unit tests live in a sibling project named `<Project>.Tests` (e.g. `Trakmark.Domain.Tests`).
- Test projects use **xUnit** and reference the project under test.
- Use **NSubstitute** for mocking in tests.
- One test class per production type, one file per test class — never group multiple unrelated types in a single test file.
- Name test methods using the Roy Osherove convention: `Subject_Scenario_ExpectedResult` with all three segments in PascalCase (e.g. `AddAppAuthentication_GoogleCredentialsMissing_ThrowsInvalidOperationException`). Never use lowercase words separated by underscores within a segment. The Subject segment must name the observable public entry point (the public method, endpoint, or property under test), not a private or internal helper — even if the test reaches the helper indirectly through that entry point.
- Each test has `// Arrange`, `// Act`, `// Assert` comments. When all phases collapse to a single expression or lambda (e.g. `Assert.Throws<>(...)` or a one-liner `Assert.False(SomeCall(value))`), combine them as `// Arrange / Act / Assert` or `// Act / Assert` on a single line — do not artificially split. Never annotate AAA markers with inline notes (e.g. `// Assert — some explanation`); use the plain marker only. This extends to the line immediately following the marker — do not place an explanatory comment beneath it (e.g. `// Arrange` followed by `// (setup in constructor)` on the next line). If the constructor provides full setup, write `// Arrange` alone.
- Do not add section-divider comments (e.g. `// ── Some heading ───`) inside test classes to group related tests. If a divider seems necessary, the test method names are not descriptive enough — rename them instead.
- Prefer data-driven tests (`[Theory]`/`[InlineData]`/`[MemberData]`) over many near-duplicate `[Fact]`s. The moment you need a second `[Fact]` that tests the same method with different inputs or expected outputs, stop and write a `[Theory]` instead — do not accumulate `[Fact]`s first and consolidate later. Once a `[Theory]` covers a case, do not also write a `[Fact]` for the same scenario — it is redundant and will diverge.
- All type arguments to `TheoryData<>` must be serializable primitives (`string`, `int`, `bool`, `enum`, etc.) — never delegates, lambdas, or complex objects (xUnit1044). When test variation requires non-serializable setup (e.g. `Action<T>`, interface mocks), encode the scenario as a `string` key and map it to the setup logic in a private helper method inside the test class.
- When writing tests for a type that returns a discriminated union or named result subtypes (e.g. `Success`, `NotFound`, `Conflict`, `Duplicate`), enumerate all subtypes before writing the first test and ensure each subtype has at least one dedicated test case.
- Every `catch` block that suppresses or transforms an exception (i.e., does not re-throw it) must have at least one dedicated test case that exercises that code path — including idempotent-success paths that catch a `DbUpdateException` and return a success result.
- Test behavior, not implementation — assert observable outcomes, not internal calls, so tests aren't brittle.
- For integration and end-to-end tests, prefer the real database via **Testcontainers** over in-memory fakes.
- Do not assert a property value that a query predicate already guarantees — e.g. `Assert.Equal(id, entity.AccountId)` immediately after `SingleAsync(e => e.AccountId == id)` is logically redundant. Assert only values that were not part of the retrieval predicate.
- Do not write trivially-true assertions: an assertion is circular when the outcome is guaranteed by construction or type invariants rather than by the behavior under test — e.g. `Assert.True(MyId.TryParse(myId.Value, out _))` always passes because `myId.Value` came from a valid `MyId`. Assert only claims the test actually exercises.
- For every `ArgumentNullException.ThrowIfNull` or `ArgumentException.ThrowIfNullOrEmpty` guard in a public or internal constructor or method, write at least one test case that passes `null` (or empty string) and asserts the expected exception.
- Do not test `internal` helpers directly. Cover them through their public API callers (e.g. test `DomainId.IsValid` by calling `TryParse`, not by invoking `IsValid` directly). If the helper is not reachable through any public surface, that is a design signal, not a reason to add a direct test.
- Prefer `Assert.Null`/`Assert.NotNull` over `Assert.False(x == null)` / `Assert.True(x != null)` (xUnit2024). Exception: when a test must explicitly invoke a custom `==`/`!=` operator to cover its null branch (e.g. `left?.Equals(right) ?? right is null`), pass a typed null variable (`TypeName? nullFoo = null;`) instead of the literal `null` — this exercises the operator without triggering xUnit2024.
- Practice **TDD**: when a spec defines behavior (e.g. OpenSpec `#### Scenario:` blocks), write the failing tests from those scenarios first, then implement to green. Each scenario maps to a test case. For test-only changes (adding tests against already-complete production code), satisfy the failing-first requirement by writing the test body with `Assert.Fail("not implemented")` as a placeholder, confirming the test fails, then replacing the placeholder with real assertions. Pre-merge and pure-chore sections that introduce no new production behavior and cite no spec scenarios are exempt from the failing-test-first requirement.
- Register every new project in `Trakmark.slnx`.

## Code conventions

- One type per file — never define multiple top-level types in the same `.cs` file. The file name must match the type name.
- Namespace matches folder structure (IDE0130): a type in `Trakmark/Data/Entities/Foo.cs` is `namespace Trakmark.Data.Entities`. When moving a file into a subfolder, update its namespace and the `using`s of every dependent file to match.
- Target **net10.0**; use latest C# language features where they improve clarity.
- Types are **`sealed` by default**; unseal only when inheritance is intended and designed for.
- Use `sealed record` (or `readonly record struct`) only when the type is a pure data carrier with no validation logic and no custom equality semantics. Domain value objects that enforce invariants in their constructor must be `sealed class`, not `record`, and must implement `IEquatable<T>` with a matching `Equals`/`GetHashCode` override **and** `==`/`!=` operator overloads. Exception: a `readonly record struct` is acceptable when (a) it wraps a single primitive value, (b) the only invariant is a range/null check on that value, and (c) the auto-generated structural equality is semantically correct for the domain. **Every type that implements `IEquatable<T>` must expose `==`/`!=` operator overloads — no exceptions.**
- When writing null-left operator assertions (`Assert.False(null == x)`), skip `readonly record struct` types — the compiler does not allow a typed null operand for a value type, so the null-guard branch is unreachable. Only reference-type value objects (`sealed class`) need null-left coverage.
- `Nullable` and `ImplicitUsings` enabled on all projects.
- No inline XML comments on code that is self-explanatory. Add `<summary>` XML docs on all `internal` and `public` types and members (constructors, methods, properties, non-trivial fields, and `const`s). Exceptions: `[LoggerMessage]` methods in logging classes (`*.Logging.cs`) — the message template is self-documenting; EF Core migration files (`Migrations/`) — auto-generated, do not edit; test methods (`[Fact]`, `[Theory]`) — the method name is the specification, an XML summary would restate it. For overrides of base-class or interface members, `/// <inheritdoc/>` alone satisfies this requirement — do not place an explicit `<summary>` block alongside `<inheritdoc/>` on the same member.
- Use **source-generated logging** (`[LoggerMessage]`) for all `ILogger` calls — never `LogInformation(...)` directly (CA1873).
- Every `catch` block that suppresses or re-routes an exception (rather than re-throwing it) must emit at least one log entry via a `[LoggerMessage]`-generated method at `Warning` level or above. A silent catch hides failure paths from observability tooling.
- When test code must call the synchronous EF Core save path intentionally (e.g. `context.SaveChanges()` to exercise a `SavingChanges` interceptor override), add `#pragma warning disable S6966` / `#pragma warning restore S6966` around that call before staging — S6966 fires on any non-awaited EF save call, and the suppression must be in the commit that introduces the test, not applied separately after merge. Do not add a trailing inline comment to a `#pragma warning disable` line — the suppression reason is already captured in the nearby code; an inline restatement violates the "no comments that restate what the code says" rule.
- When an EF Core interceptor overrides both `SavingChangesAsync` and `SavingChanges`, every test scenario written for the async path must have a corresponding test on the sync path — and vice versa. A scenario tested only on one side of the async/sync pair leaves the other branch's code path untested.
- Split large partial classes by concern: e.g. `Foo.cs` for logic, `Foo.Logging.cs` for `[LoggerMessage]` declarations.
- Keep cyclomatic complexity of any method at **15 or below**; extract helpers when a method would exceed this.
- Keep constructor and method parameter counts at **7 or below** (SonarQube S107). When a signature would exceed this, introduce a parameter object — a `sealed record` (or `internal sealed record`) that groups the related parameters — rather than reordering or splitting arbitrarily. The grouping must reflect a genuine domain concept, not just a bag of parameters.
- Validate all public constructor and method parameters that accept reference types: use `ArgumentNullException.ThrowIfNull(param)` as the first line. Exception: DI-injected dependencies (trust the container). **After writing every new public/internal type, scan each public constructor and method — confirm every reference-type parameter has the guard before moving on.**
- When `Equals` uses a specific `StringComparison`, `GetHashCode` must use the same comparer (e.g. `Value.GetHashCode(StringComparison.Ordinal)`). Mismatched comparers silently break dictionary lookups.
- No defensive null-checks on DI-injected dependencies — trust the container.
- Remove any DI-injected dependency that is not used in the file it is injected into.
- Use `null!` (not `default!`) to suppress nullable warnings on uninitialized required properties.
- Never use `!` (null-forgiving) on reflection results (e.g. `Assembly.GetType(string)!`, `typeof(T).GetMethod(name)!`). Use `?? throw new InvalidOperationException(...)` instead — reflection lookups return `null` at runtime when the target is absent, and `!` silences a real safety gap.
- Always use braces for control statements (`if`, `else`, `for`, `foreach`, `while`, `do`) — even single-line bodies.
- Prefer C# pattern matching over boolean expressions: use `x is val1 or val2` (or relational patterns like `x is 0 or > 100`) instead of `x == val1 || x == val2` for constant/relational checks on a single variable; use `obj is T { Prop: val1 or val2 }` property patterns instead of `obj is T t && (t.Prop == val1 || t.Prop == val2)`.
- Never negate the condition of an `if` that has an `else` branch (SonarQube S1940 / S7735). Invert the condition and swap the branches so the positive case comes first: `if (x) { ... } else { ... }` not `if (!x) { ... } else { ... }`.
- No comments that restate what the code already says.
- Factory methods that generate a new identity (e.g. `Entity.Create(...)`) must be called **exactly once** per entity being constructed. Calling the same factory in separate passes (e.g., a validation pass and a build pass) produces a different identity on each call. Validate inputs first, then call the factory once and use its result throughout.
- Any service method that saves an entity to a table protected by a unique index must catch `DbUpdateException` and inspect the inner `SqlException` for SQL error numbers **2601** and **2627** (unique-constraint violations). Translate those into a domain-level duplicate result (e.g., a `Conflict` or `DuplicateEntry` discriminated-union case) rather than letting the exception propagate to the caller.

## Read-side query conventions

- **Culture-safe casing on the C# side.** When building a case-insensitive EF query predicate, normalize the C#-side comparison value with `ToUpperInvariant()` — never `ToUpper()`, whose current-culture behavior triggers the Turkish-`i` bug. On the *column* side use `ToUpper()`, which EF Core translates to SQL `UPPER()` (executed by the server, culture-independent). `ToUpperInvariant()` has no EF translation, so never call it on a mapped column inside a `Where`.
- **Keep predicates sargable.** Do not wrap a column already stored in a canonical case in `ToUpper()`/`ToLower()` inside a predicate — the per-row function call is non-sargable and defeats index use. Match the stored canonical form directly (e.g. `c.State == upperInput`, where `State` is always persisted uppercase) rather than `c.State.ToUpper() == upperInput`.
- **Treat blank optional filters as no filter.** For optional filter inputs (search terms, dropdown values), gate the predicate with `string.IsNullOrWhiteSpace(...)` and `Trim()` the value before use — never let a whitespace-only or untrimmed string become a real predicate.

## Configuration

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
- Secrets (API keys) and **connection strings** go in **user secrets**, never in `appsettings.json`.
- Docker Compose **passwords** go in a **`.env` file**, never hardcoded in `docker-compose.yml`.
- Endpoint overrides go in `appsettings.json` with sensible defaults in code.
- `Properties/launchSettings.json` sets `DOTNET_ENVIRONMENT=Development`.

## UI testing

- Use **bUnit + xUnit** in a `Trakmark.Tests` project for all Blazor component tests. Do not use Playwright or Cypress for component-level UI work — they require a running app, cannot mock services via NSubstitute, and make auth simulation difficult.
- Simulate authentication in bUnit via `TestAuthorizationContext` (from the `bunit.web` package). Do not spin up a real auth server for component tests.
- Mock the service boundary via NSubstitute in bUnit tests. Do not re-prove database behavior through UI tests — that is covered by integration tests (Testcontainers).
- Playwright may be introduced later as a separate smoke-test layer for true E2E coverage (login flow, full stack). Keep it in a separate project; do not mix with component tests.
- Before committing a Blazor form component, verify that every `maxlength`, `min`, and `max` attribute on input elements matches the corresponding domain constraint exactly — look up the domain type's constant or constructor guard; do not rely on memory.

## Blazor component conventions

- Every `InteractiveServer` or `InteractiveAuto` component with a side-effectful `OnInitializedAsync` must add `if (!RendererInfo.IsInteractive) return;` as the first line — prevents redundant DB calls and service invocations during SSR prerender.
- Any `OnInitializedAsync` that calls a service which may throw must wrap the call in a broad `catch (Exception ex)`, emit a `[LoggerMessage]`-generated entry at `Warning` level or above, and set a user-visible error field — never let it propagate uncaught. Prefer `catch (Exception ex)` over a narrow exception-type filter; unexpected types must not bypass the error display.
- When adding an async-guarded submit button (e.g. a `_isSaving` flag), call `StateHasChanged()` immediately after setting `_isSaving = true` — Blazor Server does not re-render at intermediate `await` points, so without the explicit call the button never visually disables while the request is in flight.
- When a component reloads data into a backing field (e.g. `_page`) and the load can fail, clear that field (set it to `null` or empty) inside the `catch` before setting the error field — otherwise stale rows stay visible beneath the error banner.
- For non-critical JS interop (e.g. cosmetic local-time formatting in `OnAfterRenderAsync`), catch **both** `JSDisconnectedException` **and** `JSException`: a missing or renamed script raises `JSException`, which if uncaught tears down the circuit. Skip the interop call entirely when there is nothing to format (e.g. the page has no rows) rather than invoking it unconditionally.

## Git commits

- Subject line: ≤ 50 characters, imperative mood, no period.
- Body (optional): wrap at 72 characters, explain _why_ not _what_.
- Total commit message: **under 50 words**.
- Always add `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>` when AI-assisted.

## Pull request descriptions

- **Under 200 words.**
- Include: what changed, why, and a short test/verification note.
- No filler phrases ("this PR...", "in this change...").

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
