# Retro — view-cities — 2026-07-05

The read-side "View Cities" admin feature (PR #53, branch `view-cities`) passed
local tests and a clean SonarQube build on first implementation, but five
subsequent Copilot/Qodo review rounds surfaced defects our conventions did not
prevent. The generalizable ones (culture-sensitive casing, non-sargable
predicates, untrimmed optional filters, Blazor stale-state-on-failure, JS
interop resilience) are now encoded as rules.

## Findings applied

| Evidence | Improvement | File(s) changed |
|----------|-------------|-----------------|
| `searchTerm.ToUpper()` used current culture (Turkish-'i' bug); fixed in 0ef229e | Rule: normalize C#-side EF predicate inputs with `ToUpperInvariant()`; column side uses `ToUpper()` (→ SQL UPPER()), never `ToUpperInvariant()` on a mapped column | CLAUDE.md (Read-side query conventions), openspec/config.yaml (tasks), .claude/agents/developer.md (self-review) |
| Redundant `ToUpper()` on a canonically-cased `State` column made the predicate non-sargable; fixed in 6c6d166 | Rule: match a canonical-cased column directly (`c.State == upperInput`), never wrap it in per-row `ToUpper()`/`ToLower()` | CLAUDE.md, openspec/config.yaml, .claude/agents/developer.md |
| Whitespace-only / untrimmed filter inputs became real predicates; fixed in 6af74d7 | Rule: gate optional filters with `IsNullOrWhiteSpace` + `Trim()` before use | CLAUDE.md, openspec/config.yaml, .claude/agents/developer.md |
| Query failure left stale `_page` rows visible under the error banner; fixed in 6c6d166 | Rule: clear the backing field in the `catch` before setting the error field | CLAUDE.md (Blazor conventions), openspec/config.yaml (ui_testing), .claude/agents/developer.md |
| `OnAfterRenderAsync` caught only `JSDisconnectedException`; a missing/renamed script throws `JSException` and can tear down the circuit; fixed in 0ef229e/6c6d166 | Rule: for non-critical interop catch both `JSDisconnectedException` and `JSException`, and skip interop when there is nothing to format | CLAUDE.md, openspec/config.yaml, .claude/agents/developer.md |

## Carry-forward (not yet actionable)

- **SonarQube/coverage config friction (theme 6).** Reassigning `.sql` to the
  T-SQL analyzer made `.sql`/`.js` count as uncovered and dropped new-code
  coverage below the gate; the fix required `sonar.coverage.exclusions` scoped
  to specific paths (`Scripts/*.sql`, `wwwroot/js/*.js`), not blanket globs,
  and the .NET scanner ignores `sonar-project.properties` — exclusions must be
  passed as `/d:` params in CI. This is CI/infra config, not a code convention;
  it touches `.github/workflows/ci.yaml`, which is outside the rule-file scope.
  Capture as a CI/Sonar runbook note if it recurs.
- **Bot-review nit tail (theme 7).** Five review rounds each pushed a commit
  that re-triggered the bots on the changed line, risking an endless nit tail.
  Worth a workflow guideline on batching/stopping bot-review fixes, but this is
  a process/orchestration decision rather than a coding convention — defer
  until the stop criterion is concrete enough to encode.
