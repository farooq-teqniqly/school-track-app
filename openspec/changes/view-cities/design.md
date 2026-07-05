## Context

`SaveCitiesBatchService` already queries the `Cities` table internally for
cross-batch duplicate detection, but nothing exposes persisted cities to the
UI. The Add Cities page (`/admin/cities/add`) is write-only. This change adds
the first read path over `Cities`.

This is deliberately the first of two changes. The second (a later, separate
change) adds a live per-row duplicate guard to the Add Cities form. Both
consume the same read side, which is why the query service is introduced here
with that future consumer in mind.

## Goals / Non-Goals

**Goals:**
- A read-only query service returning a projected list of cities (never the EF
  entity), with name search, state filter, and paging.
- An admin-only View Cities page that lists persisted cities.
- Keep the read side shaped so the future duplicate-guard change extends it
  rather than reshaping it.

**Non-Goals:**
- The inline per-row duplicate guard on the Add Cities form (separate change).
- Editing or deleting cities (add-and-view only for now).
- Any change to `Trakmark.Domain`, the `City`/`State` value objects, the
  `Cities` schema, or a new EF migration.

## Decisions

### The query service returns a read-model projection, not `CityEntity`
`CityEntity` carries persistence-only fields (`Id` surrogate key,
`CreatedByUserId`) that the UI has no business seeing. The service projects to a
small `sealed record CityListItem` (city name, state abbreviation, state name
for display, `CreatedAt`). The EF entity never leaves the service boundary.

### "Created by" is resolved to the creator's email, with a raw-ID fallback
`CityEntity.CreatedByUserId` stores a `RegisteredUserId` (`USR-XXXXXX`), which
is not human-readable. To answer "who created this city," the query resolves it
in two hops: `Cities.CreatedByUserId` -> `RegisteredUsers.RegisteredUserId` ->
`AspNetUsers.Email`. When no matching Identity user is found (e.g. an account
was removed), the projection falls back to the raw `RegisteredUserId` so the
column is never blank. Showing the raw ID unconditionally was rejected — it
does not answer "who." This is a **read-time join only**: the query joins
across the Identity table but adds no `HasForeignKey`/`HasOne` schema
constraint, so it does not violate the "domain tables must not FK to Identity
schema" rule. Recorded here so a reviewer does not flag the Identity join.

### Sort order: state, then city name
Cities are ordered by state abbreviation, then by name (both ascending, case-
insensitive). This groups a state's cities together, which is the natural way
to scan reference data. Search and filter narrow the set; the sort is stable
across them.

### Search and state filter are server-side query predicates
Name search is a case-insensitive `Contains`; the state filter is an exact
abbreviation match. Both are applied in the EF query (translated to SQL), not
in memory, so paging counts stay correct as the catalog grows. `ToUpper()` is
used rather than `ToUpperInvariant()` for the same EF-translation reason
documented in `SaveCitiesBatchService`.

### Paging over load-all
Reference data (U.S. cities) can reach the thousands. The page requests a fixed
page size and a page number; the service returns the page plus a total count so
the UI can render pager controls. This avoids rendering thousands of rows at
once and keeps the Blazor Server render diff small.

### The query service is the intended home of the future existence check
The later duplicate-guard change needs a cheap "does (name, state) already
exist" check. That method belongs on `ICityQueryService` (the read side over
`Cities`), and the guard change will add it there — this change does not build
it (no consumer yet, per YAGNI), but the interface is named and scoped so the
guard change extends it rather than introducing a second read service. Recorded
here so the intent is not lost between the two changes.

### View Cities is a distinct page, not merged into Add Cities
Listing and batch-add have different layout needs (a scannable table vs. a wide
repeatable-row form). They stay separate pages under the same Admin dropdown.
The inline visibility the user ultimately wants on the Add form is delivered by
the separate duplicate-guard change, not by cramming the list into the add
page.

## Risks / Trade-offs

- **[Risk]** Server-side `Contains` search on `Name` cannot use the unique
  index and will scan as the table grows. → **Mitigation**: Acceptable at
  reference-data scale (thousands, not millions); revisit with a full-text or
  prefix index only if measured to be slow.
- **[Risk]** Paging + search + filter interaction can produce an empty page if
  the user is on page N and narrows the filter. → **Mitigation**: The page
  resets to page 1 whenever the search text or state filter changes.

## Open Questions

- Page size (e.g. 25 vs 50 rows) — a UI default, settled during implementation;
  not a spec-level decision.
