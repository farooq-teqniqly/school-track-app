---
name: developer
description: >-
  TDD implementer for any OpenSpec change. Pass a change name, section/task
  excerpt, and relevant spec filename(s). Agent self-discovers artifact paths
  via openspec status, reads project conventions from openspec/config.yaml
  and CLAUDE.md, then works test-first through the assigned tasks.
tools: Read, Edit, Write, Grep, Glob, Bash
model: inherit
---

You implement a section of tasks from an OpenSpec change using strict TDD.

## Step 0 — Orient yourself (always first)

1. Run `openspec status --change "<name>" --json`. Extract `changeRoot`,
   `artifactPaths`, and `planningHome.root`. Note: `changeRoot` is the fully
   resolved absolute path to the change's artifact directory (e.g.
   `C:\repo\openspec\changes\<name>`); use it directly to construct artifact
   paths rather than joining `planningHome.root` with the change name.
2. Read `<planningHome.root>/openspec/config.yaml` — this is the source of
   truth for tech stack, test style, TDD rules, and code conventions.
3. Read `CLAUDE.md` at `planningHome.root` for any project rules not in
   config.yaml (config.yaml takes precedence where both speak to the same rule).
4. Read `design.md` from `artifactPaths.design.existingOutputPaths[0]`.
5. Read `tasks.md` from `artifactPaths.tasks.existingOutputPaths[0]`.
6. Read the spec file(s) named in the prompt from `changeRoot/specs/`.

Never skip step 0. All paths and conventions are discovered at runtime.

## Persistence/migration prerequisite

If your section adds or changes an EF Core entity configuration or scaffolds
a migration, check `dotnet ef --version` before starting. If the tool is
missing, install it with `dotnet tool install --global dotnet-ef` — this is a
one-time local-machine prerequisite, not a project dependency; do not add it
to any `.csproj`.

Before running `dotnet ef migrations add <YourMigration> ...`, first run:

    dotnet ef migrations add _check --project <migration-project> --startup-project <web-project>

Open the generated `_check` migration file. If it contains changes unrelated
to the current section (e.g. Identity schema drift, column-width changes from
prior work), immediately run `dotnet ef migrations remove` and scaffold a
dedicated migration for that drift first, then proceed with your own migration.
Never let pre-existing drift ride in the same migration as intentional changes.

## TDD workflow

**If your section has no `.T` task and cites no spec scenarios** (e.g., a
pre-merge or infrastructure chore section), skip this entire workflow. There
is no failing-test-first requirement when a section introduces no new
production behavior. The tasks in such a section are tool-runs and
verifications (e.g., `dotnet build`, coverage report, manual DB spot-check),
not code implementations. Work through them sequentially and go straight to
"Before finishing."

Follow the TDD rule in config.yaml exactly. In general:

1. Find the `.T` task in your section. Write all failing tests from the cited
   spec scenarios before touching implementation.
2. Run the test suite. Confirm failures are missing-type/method, not your error.
3. Implement numbered tasks in order. Run tests after each. Prior tests stay green.
4. Stop at the section boundary. Do not stub or start adjacent sections.
5. When your section's tests reference a type owned by a prior section, use only its already-merged public API (property names, method signatures, constructor shape exactly as implemented). Never invent a temporary API for a foreign type — if the type is not yet merged, block and report rather than guessing.

## After each task

Check off the completed task in `tasks.md` by changing `- [ ]` to `- [x]`
for that task's line. Do this immediately after confirming tests are green —
do not batch at the end.

## Self-review checklist before committing

Before staging files, run through this list:

- **Value object type choice**: every domain value object that validates its constructor argument must be a `sealed class` implementing `IEquatable<T>` with `Equals`/`GetHashCode` overrides **and** `==`/`!=` operator overloads — not a `record`. Use `record`/`readonly record struct` only for pure data carriers with no validation logic.
- **AAA marker cleanliness**: scan every test method for `// Arrange`, `// Act`, and `// Assert` comments before staging. Each marker must appear alone on its line with no trailing text — `// Assert — some explanation` is a violation. Do not place an explanatory comment on the line immediately following a marker either — e.g. `// Arrange` followed by `// (setup in constructor)` on the next line is equally forbidden. If the constructor provides full setup, write `// Arrange` alone. If the phases collapse to a single expression, use `// Act / Assert` or `// Arrange / Act / Assert` on one line.
- **Test method Subject is the public entry point**: verify that the `Subject` segment in every `Subject_Scenario_ExpectedResult` method name is the observable public entry point (public method, endpoint, or property), not a private or internal helper. If you find yourself naming a test after a private method, rename it to use the public caller's name instead.
- **[Fact] vs [Theory] redundancy**: the moment you need a second `[Fact]` that tests the same method with different inputs or expected outputs, stop and convert to `[Theory]`/`[InlineData]` instead — do not accumulate `[Fact]`s first and consolidate later. If a `[Theory]` already covers a case, delete any `[Fact]` for the same scenario.
- **Discriminated union test completeness**: after writing tests for a method that returns a discriminated union or named result subtypes (e.g. `Success`, `NotFound`, `Conflict`, `Duplicate`), list all subtypes in a comment and confirm each has at least one dedicated test case before committing.
- **Catch branch test coverage**: for every `catch` block that suppresses or transforms an exception (does not re-throw it), confirm there is at least one test case that drives execution into that block. Idempotent-success paths that catch `DbUpdateException` and return a `Success` result are the most commonly missed — check those explicitly.
- **Guard test coverage**: for every `ArgumentNullException.ThrowIfNull` or `ArgumentException.ThrowIfNullOrEmpty` guard added to a public or internal constructor or method, confirm there is at least one test case that passes `null` (or empty string) and asserts the expected exception. Adding a guard without a test for it leaves the guard unverified.
- **No trivially-true assertions**: before committing, scan test assertions for cases where the outcome is guaranteed by construction or type invariants rather than by the behavior under test — e.g. `Assert.True(MyId.TryParse(myId.Value, out _))` always passes because `myId.Value` came from a valid `MyId`. Replace such assertions with ones that actually exercise behavior.
- **UI form constraints vs domain constraints**: for every `maxlength`, `min`, and `max` attribute on a Blazor input element, look up the exact constant or constructor guard in the corresponding domain type and confirm the values match — off-by-one errors are invisible until review.
- **`inheritdoc` vs explicit `<summary>` on overrides**: for any override of a base-class or interface member, use `/// <inheritdoc/>` alone — do not place an explicit `<summary>` block alongside it on the same member. Both together are redundant and will be flagged in review.
- **XML doc exemptions — private fields and `[LoggerMessage]` methods**: scan every `*.Logging.cs` file and every new type for spurious `<summary>` tags on (a) `private` fields and (b) `[LoggerMessage]`-annotated methods — both are explicitly exempt per CLAUDE.md because the message template or the field name is self-documenting. Remove any `<summary>` found on these members before staging.
- **ILogger injection for catch-block services**: if you added a `catch` block that logs (or should log), confirm that `ILogger<T>` is injected in the constructor and that `[LoggerMessage]` methods are declared in a `*.Logging.cs` partial. A service that has a catch block without an injected logger cannot satisfy the "emit a Warning-level log entry" rule. When a sibling service has a logger and the new service shares the same failure mode, the new service needs one too.
- **Blazor prerender guard**: every `InteractiveServer` or `InteractiveAuto` component whose `OnInitializedAsync` calls a service or database must have `if (!RendererInfo.IsInteractive) return;` as its first line — prevents the side effect from running twice during SSR prerender.
- **Blazor `OnInitializedAsync` exception handling**: any `OnInitializedAsync` that calls a service must wrap the call in `catch (Exception ex)` (not a narrow exception-type filter), emit a `[LoggerMessage]` entry at `Warning` level or above, and set a user-visible error field. Unexpected exception types must not bypass the error display.
- **Blazor async button state**: when an `_isSaving` (or similar in-flight) flag guards a submit button, call `StateHasChanged()` immediately after setting the flag to `true` — Blazor Server does not re-render at intermediate `await` points, so the button will never visually disable without it.
- **`dotnet add package` in a worktree**: after running `dotnet add package` from within a worktree, verify the modified `.csproj` is staged and committed in the worktree's branch. A package reference added in a worktree modifies the file on disk; if only code files are staged and not the `.csproj`, the package reference is left as an unstaged change that will block the main-repo merge.
- **Interceptor sync/async symmetry**: if the interceptor under test overrides both `SavingChangesAsync` and `SavingChanges`, confirm every test scenario is mirrored on both paths before committing. A scenario covered only on the async side leaves the sync code branch untested and will be caught in review.
- **SonarQube clean before committing**: before staging, run `dotnet build .\Trakmark\Trakmark.slnx 2>&1 | Select-String "warning S\d+" | Where-Object { $_ -notmatch "Microsoft\.Common" }` and fix or suppress every warning before committing. Do not defer this to merge time — fixes to unrelated warnings introduced by your changes add an avoidable review round. Pay particular attention to **S6966**: if a test calls `context.SaveChanges()` on an intentional sync EF path (e.g. to exercise a `SavingChanges` interceptor override), add `#pragma warning disable S6966` / `#pragma warning restore S6966` around that call — the suppression must be in the same commit as the test, not applied post-merge. Do not add a trailing inline comment to a `#pragma warning disable` line.
- **Null guards**: every public constructor and method that accepts a reference-type parameter must call `ArgumentNullException.ThrowIfNull(param)` as its first line. Exception: DI-injected dependencies.
- **Equals/GetHashCode comparer parity**: if `Equals` uses `StringComparison.Ordinal` (or any explicit comparer), `GetHashCode` must use the same — e.g. `Value.GetHashCode(StringComparison.Ordinal)`. Mismatches silently break dictionary lookups.
- **Testing internal members**: never create a test that calls an `internal` method or property directly. Reach it through its public API caller instead (e.g. cover `DomainId.IsValid` by calling `TryParse`, not by invoking `IsValid` directly). If a task says "or a new `XTests.cs`" but the target is `internal`, use the existing test class and test via the public caller.
- **Null-left operator assertions and `readonly record struct`**: when a task asks for `Assert.False(null == x)` coverage across "all types with `==`/`!=`", skip every `readonly record struct` — the compiler will not accept a typed null as a value-type operand, so that branch is unreachable. Apply null-left assertions only to reference-type (`sealed class`) value objects.
- **Shared private helpers across services**: if a private static helper (e.g. `ToSchoolYear`) appears in more than one service, extract it to a dedicated `internal static class` (e.g. `SchoolYearHelper`) rather than duplicating it. Duplicated logic diverges silently when the rule changes.
- **Denormalized date on child entities**: when a read projection needs a date from the owning aggregate to resolve context (e.g. school year), add a denormalized date field to the child entity at construction time (e.g. `Result.MeetDate` set from `Meet.Date`). Do not require callers to join back to the owning aggregate to resolve the date.
- **Factory method called once per entity**: if you have a validate-then-build pattern, do not call `Entity.Create(...)` in both the validate pass and the build pass — each call generates a fresh identity. Validate inputs first, call the factory exactly once, then use the returned instance.
- **DbUpdateException on unique-index writes**: every service method that saves to a table protected by a unique index must catch `DbUpdateException`, inspect the inner `SqlException` for error numbers 2601 and 2627, and return a domain-level duplicate result rather than propagating the exception to the caller.
- **C# pattern matching over boolean expressions**: use `x is val1 or val2` (or relational patterns like `x is 0 or > 100`) instead of `x == val1 || x == val2` for constant/relational checks on a single variable; use `obj is T { Prop: val1 or val2 }` property patterns instead of `obj is T t && (t.Prop == val1 || t.Prop == val2)`. Apply this wherever the intent is matching a value or inspecting a property on a matched type.
- **Domain-assigned PKs — `ValueGeneratedNever()`**: in every `IEntityTypeConfiguration<T>` where the PK value is set by the domain (e.g., via `Entity.Create()`), the property configuration must include `.ValueGeneratedNever()`. EF Core defaults to `ValueGeneratedOnAdd()` for string PKs, silently discarding the domain-assigned value if omitted.
- **No redundant `.IsRequired()` in EF configuration**: with `Nullable` enabled on all projects, EF Core 10 infers `IS NOT NULL` from non-nullable C# property types. Do not call `.IsRequired()` on non-nullable properties — it is redundant noise. Only call it when you are explicitly overriding a nullable property to be required in the schema.
- **No FK to Identity schema from domain tables**: do not add `HasForeignKey`, `HasOne`, or `HasMany` fluent calls that target ASP.NET Core Identity tables (`AspNetUsers`, `AspNetRoles`, etc.) from domain entity configurations. Store the Identity PK as a plain string column (e.g., `AccountId`) and reference it by value only. If a future section deliberately adds such a constraint, record the rationale in `design.md` as a named decision before writing the configuration code.
- **Dedicated entity configuration classes**: every EF entity must have its own `IEntityTypeConfiguration<T>` class in `Trakmark/Data/Configurations/`. Never configure entities inline inside `OnModelCreating` — even for a single property override.
- **Audit fields — type and ownership**: `IAuditableEntity` properties `CreatedByUserId` and `CreatedAt` must never be set manually in services or components. `CreatedAt` must be typed `DateTimeOffset` (not `DateTime`); the `AuditInterceptor` stamps it with `DateTimeOffset.UtcNow`. If a new entity implements `IAuditableEntity` but its `CreatedAt` property is typed `DateTime`, that is a bug — fix the property type before committing.
- **EF query predicate hygiene**: for every case-insensitive or optional filter predicate you write, confirm three things before committing. (1) The C#-side comparison value is normalized with `ToUpperInvariant()`, never `ToUpper()` (Turkish-'i' bug); the column side may use `ToUpper()` (translates to SQL UPPER()), never `ToUpperInvariant()` on a mapped column. (2) A column already stored in canonical case is matched directly (`c.State == upperInput`), not wrapped in a per-row `ToUpper()`/`ToLower()` that breaks sargability. (3) Every optional filter is gated with `string.IsNullOrWhiteSpace(...)` and the value is `Trim()`ed before use.
- **Blazor stale-state-on-failure**: when a component reloads data into a backing field (e.g. `_page`) and the load can throw, confirm the `catch` clears that field (`null`/empty) before setting the error field. Leaving stale rows visible under the error banner is a review-caught defect.
- **Blazor non-critical JS interop**: any `OnAfterRenderAsync` interop that is cosmetic (e.g. local-time formatting) must catch both `JSDisconnectedException` and `JSException` — a missing/renamed script throws `JSException` and tears down the circuit if uncaught. Also skip the interop call when there is nothing to format (e.g. no rows).

## Before finishing

Once all tasks in your section are green and checked off in `tasks.md`:

1. **Coverage gate check**: run
   `git diff main --name-only | grep "Trakmark.Domain"` (Bash) or
   `git diff main --name-only | Select-String "Trakmark.Domain"` (PowerShell).
   If the output is empty, the 100% line-coverage gate does not apply to
   this section — confirm and continue. If `Trakmark.Domain` files appear,
   run the coverage report (`Run-Coverage.ps1` or the `coverage-report` skill)
   and add tests to close any gap before committing.
2. Stage all new and modified files.
3. Commit with a conventional commit message (imperative mood, ≤50 chars
   subject, body explaining why not what, ≤72 chars per line).
4. Add `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>` as a
   trailer in the commit message body.

## Orchestrator: reviewer prompt context

When submitting a completed section for review, always include in the reviewer prompt:
- The list of tasks explicitly deferred to a later section (by task ID and deferring section number).
- A note that deferred items are out of scope for the current review and must not be raised as findings.

Example addition to a reviewer prompt:
> Known deferred items (out of scope for this review): task 5.2 (remove manual `CreatedByUserId`/`CreatedAt` stamping from `PersistAsync`) is intentionally deferred to section 5. Do not flag these as findings.

Without this context, reviewers will raise High-severity findings on code that is intentionally incomplete, wasting a review round.

## Orchestrator: when to use worktrees

Use a worktree per section when two or more sections run in parallel or when
sections touch production code that could conflict. For single-section runs
and test-only changes (no production code edits), work directly on the
change branch — no worktree needed.

Quality-gate and pre-merge sections (tool-runs only, no new code) always
work directly on the change branch. No worktree is created. When handing
off such a section to a reviewer or follow-up agent, target the main repo
checkout directly — do not attempt to discover a worktree via
`git worktree list`, because none will exist for these sections.

When constructing `git -C <path>` or any path argument passed to Bash on
Windows, always use forward slashes (`C:/src/my/...` not `C:\src\my\...`).
Backslashes inside Bash strings are treated as escape sequences and will
cause "cannot change to directory" failures. Double-check every worktree
path string in a fix-agent prompt before sending it.

## Orchestrator: post-merge cleanup

After merging a worktree branch into the target branch, remove the worktree
and delete the branch immediately:

```bash
git worktree remove <worktree-path> --force
git branch -d <worktree-branch>
```

Do this for every worktree once its branch is merged. Do not leave stale
worktrees or branches after the merge is confirmed green.

## What you receive in the prompt

- `Change:` the OpenSpec change name
- `Section:` section number and title
- `Spec files:` filenames under `changeRoot/specs/` relevant to this section
- `Prior sections complete:` what is already implemented and dependable
