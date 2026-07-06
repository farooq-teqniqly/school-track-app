## 1. Read model

- [x] 1.1 Add `Trakmark/Services/CityListItem.cs`: `public sealed record CityListItem` carrying the city name, state abbreviation, state display name, `CreatedAt` (`DateTimeOffset`), and `CreatedBy` (the resolved creator display identity — the creator's email, or their `RegisteredUserId` when no matching Identity user is found; see `design.md`). Pure data carrier — no validation, structural equality is correct. Add `<summary>` docs.

## 2. Read model page result

- [x] 2.1 Add `Trakmark/Services/CityPage.cs`: `public sealed record CityPage(IReadOnlyList<CityListItem> Items, int TotalCount)`. Add `<summary>` docs.

## 3. Query service

- [x] 3.1 Add `Trakmark/Services/ICityQueryService.cs` with a single method returning a `CityPage`, taking a search term (nullable), a state abbreviation filter (nullable), a page number, and a page size. Document that this interface is the intended home of a future `(name, state)` existence check (per `design.md`).
- [x] 3.2 Write failing integration tests (Testcontainers, real SQL Server) in `Trakmark.Data.Tests` (or the existing integration-test project) for the scenarios in `specs/manage-cities/spec.md`: list ordered by state then name (case-insensitive); empty catalog returns empty page with total 0; name search filters case-insensitively; no-match search returns empty; state filter narrows results; cleared state filter returns all; a page returns at most the page size and reports the correct total count; `CreatedBy` resolves to the creator's email when a matching Identity user exists, and falls back to the raw `RegisteredUserId` when no Identity user matches. Seed cities, a `RegisteredUser`, and an `AspNetUsers` row directly.
- [x] 3.3 Implement `Trakmark/Services/CityQueryService.cs`: project `CityEntity` to `CityListItem` (never return the entity); resolve `CreatedBy` via a read-time left join `Cities.CreatedByUserId` -> `RegisteredUsers.RegisteredUserId` -> `AspNetUsers.Email`, falling back to the raw `RegisteredUserId` when no Identity user matches (query-time join only — no `HasForeignKey`/schema FK to Identity tables, per `design.md`); apply case-insensitive name `Contains` and exact state-abbreviation filters as EF query predicates (use `ToUpper()`, not `ToUpperInvariant()`, per the note in `SaveCitiesBatchService`); order by state then name; page with `Skip`/`Take`; return items plus total count. `ArgumentNullException.ThrowIfNull` is not needed for value-type parameters, but guard any reference-type parameter that is not DI-injected.
- [x] 3.4 Register `ICityQueryService` -> `CityQueryService` in the web project's DI container alongside the existing city services.
- [x] 3.5 Run tests to green.

## 4. UI: View Cities page

- [x] 4.1 Write failing bUnit tests in `Trakmark.Tests` for the nav bar: a "View Cities" item renders in the Admin dropdown when the authenticated user is in the Admin role; does not render otherwise. Use `TestAuthorizationContext`.
- [x] 4.2 Add the "View Cities" item to the Admin dropdown in `TopNavMenu.razor` (href `admin/cities`), restricted to the Admin role. Run tests to green.
- [x] 4.3 Write failing bUnit tests for the page, mocking `ICityQueryService` via NSubstitute: rows render for the returned page items (name, state, and created-by shown); empty result shows the no-cities message and no rows; entering a search term calls the service with that term and resets to page 1; selecting a state calls the service with that abbreviation and resets to page 1; pager advances the page number and calls the service. Simulate the Admin role via `TestAuthorizationContext`.
- [x] 4.4 Build `Trakmark/Components/Pages/ViewCities.razor` (`@page "/admin/cities"`, `[Authorize(Roles = "Admin")]`, `@rendermode InteractiveServer`): a search box, a state `<select>` (reuse `StateHelper.AllStates`), a results table (name, state, created-by), and pager controls. Add `if (!RendererInfo.IsInteractive) return;` as the first line of `OnInitializedAsync`; wrap the service call in a broad `catch (Exception ex)` that emits a `[LoggerMessage]` at `Warning` and sets a user-visible error field. Reset to page 1 when the search term or state filter changes. Run tests to green.

## 5. Pre-merge

- [x] 5.1 Confirm the diff does not touch `Trakmark.Domain` (read-side/UI only) — if confirmed, the domain 100% coverage gate does not apply. If it does, run the coverage-report skill and close any gap.
- [x] 5.2 Run the SonarQube clean-build warning check per `CLAUDE.md` and resolve or triage findings.
