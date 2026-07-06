## Why

Admins can add cities but cannot see what already exists. The only way to
discover that a city is already persisted is to submit a batch and have the
save rejected with a `CrossBatchDuplicate` error. There is no read side over
`Cities` at all — no query service, no list page. Admins add cities blind and
hit avoidable duplicate errors.

This change adds the missing read side so an Admin can browse the city catalog
before adding. It is the foundation for a later, separate change that surfaces
duplicates inline in the Add Cities form as the user types.

## What Changes

- Add a read-only city query service that returns a projected list of persisted
  cities, supporting name search, state filtering, and paging.
- Add an admin-only "View Cities" page at `/admin/cities` that lists persisted
  cities (name + state), with a search box, a state filter, and paging.
- Add a "View Cities" item to the Admin nav dropdown alongside "Add Cities".

## Capabilities

### Modified Capabilities
- `manage-cities`: Adds requirements for browsing/listing persisted cities
  (query service + admin View Cities page). The existing add/validation/
  persistence requirements are unchanged.

## Impact

- New application-layer read service (`ICityQueryService` / `CityQueryService`)
  and a read-model projection type in the `Trakmark` web project's `Services`
  folder, mirrored by tests.
- New Blazor admin page `Trakmark/Components/Pages/ViewCities.razor` and a nav
  entry in `TopNavMenu.razor`, restricted to the Admin role.
- No change to `Trakmark.Domain` (read-side only — exempt from the domain
  100% coverage gate; confirm via diff before merge).
- No new EF migration (reads the existing `Cities` table).
