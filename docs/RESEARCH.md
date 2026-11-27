# FlyzoneMockup — Session 2 research: improvements & features

Status: **research only — no code changes**. Owner: orchestrator. Date: 2026-11-27.
Feeds `docs/GAMEPLAN.md` phase **P0**; implements nothing.

## 0. Method & limitations (read before trusting conclusions)

- Four independent lanes were run: (1) official .NET 10 practice research, (2) portfolio/hiring-signal research, (3) local dead-affordance inventory, (4) architecture tradeoff memo. This document is the reconciliation of all four, not any single lane's output.
- **Source limitation:** the portfolio lane's `websearch` returned zero results for all six queries; direct blog/Reddit fetches 404/403. Therefore **no 2024–2026 hiring-manager article could be verified**. Claims about what recruiters check are labeled **[inference]**; official Microsoft/GitHub sources were fetched successfully and are cited in §6.
- **Conflict resolved:** the earlier GAMEPLAN note implied no POST action emits an antiforgery token. The local inventory shows tag helpers on all 5 existing forms do emit tokens; the real gap is the **upload endpoint** (`Controllers/ServicesController.cs:148` has `[ValidateAntiForgeryToken]` but no form/token exists anywhere). GAMEPLAN §3 has been corrected to this narrower claim.
- **Effort disagreement resolved:** role-gated admin was estimated M by the tradeoff lane and L by the inventory lane. Treat as **M–L** (role seeding + views + tests decide).
- Environment facts re-verified this session: SDK 10.0.404 at `~/.dotnet`, build green (3 warnings), Docker daemon v29.9.1 available, working tree clean apart from untracked `docs/`.

## 1. Decisions for P0 (architecture)

| Decision | Recommendation | Why / tradeoff | Would change my mind |
|---|---|---|---|
| **Storage** | **Keep SQLite**; patch NU1903 via package upgrade/direct fixed reference. Document Postgres as an optional profile only. | Clean-clone startup, zero extra containers, SQLite-typed migrations already exist. Cost: a reviewer who values "real DB ops" may see SQLite as toy-grade **[inference]**. Postgres adds a second container, readiness, credentials, migration rewrite (but clears the advisory). | Public multi-user deployment; target role needing multi-service Docker/K8s; NU1903 unpatchable on SQLite. |
| **Admin surface** | **Minimal role-gated admin** — list applications, change status (append `ApplicationHistory`), edit `InternalNotes`. One controller, one view, one seed path. No full backoffice. | Status history is written (`ServicesController.cs:74-83,132-141`) but has no transition path, so the README claim can never become true without an admin actor. Roles are greenfield (`Program.cs:52-76` has no `AddRoles`). **Conditional:** if publishing stays private/case-study only, cut admin and delete the claim instead. | A private case-study-only plan, or an audience that doesn't care about authz depth. |
| **Rendering** | **Stay server-rendered MVC + Bootstrap**; vanilla `fetch` to an antiforgery-protected endpoint if interactivity is needed. | Repo is Razor + CDN Bootstrap/jQuery with no JS build or tests. HTMX/SPA adds a second toolchain, partial endpoints, and new CSRF surface — exactly where gaps already exist. | A truly live timeline requirement or a front-end-heavy target role. |
| **Workflow** | **Synchronous status transitions** in one transactional method (status + history together, replacing two non-transactional `SaveChanges` at `ServicesController.cs:60,83`). Defer jobs/email until after P4. | Background work sounds impressive **[inference]** but introduces secrets, external dependencies, and SQLite writer contention for little verifiable payoff in a demo. | Explicit notification/SLA requirement or a queue-focused audience. |

## 2. Ranked feature shortlist — what to actually add

Ranked by *verifiable backend skill per unit effort*, reconciling all lanes. Effort: S < 1h, M ≈ half-day, L = days.

| # | Feature | Evidence in current code | Effort | Skill demonstrated | Decision |
|---|---|---|---|---|---|
| 1 | **Secure document upload UI** (fix IDOR at `:222`, ownership check, `documentType` allow-list, antiforgery header, auth-gated download) | Action exists at `ServicesController.cs:149-229`; **zero** forms with `enctype="multipart/form-data"` or `<input type="file">`; `wwwroot/uploads/` absent | M | File streaming, magic-byte validation, authz, CSRF | **Build (P5-1)** — highest ROI: hidden backend becomes demo-able |
| 2 | **Application details + status-history timeline** with ownership gate | History written at `:74-83,132-141`, model `ApplicationHistory.cs:5-17`; dashboard never queries it (`AcoountController.cs:152-156`); recent-activity rows inert (`Dashboard.cshtml:276-297`) | M | EF Core `Include`, ownership authz, state modeling | **Build (P5-2)** — makes the README claim true |
| 3 | **Track Status page** reusing #2, plus search/filter by reference | Dead hero CTA `Index.cshtml:80` → `/Document/Track` 404; dead sidebar `Dashboard.cshtml:48` | S–M | EF Core LINQ, routed lookup, `[Authorize]` | **Build (P5-3)**; add paging/filter only if time remains |
| 4 | **Minimal admin review queue** (status transitions + `InternalNotes`) | No roles anywhere; `ServiceApplication.InternalNotes:20` never read | M–L | Identity roles, policy authz, migrations | **Build if public repo + technical audience** (§1); else cut |
| 5 | **Validation + form hygiene** — render `_ValidationScriptsPartial` in the 4 form views with `Layout = null`; align `RegisterViewModel.cs:28` min 6 to Identity min 8 (`Program.cs:59`) | `_ValidationScriptsPartial` rendered **0** times; `Dashboard.cshtml:4`, `login.cshtml:4`, `register.cshtml:4`, both Services forms `:4` | S | Identity policy, client/server validation parity | **Build (P5-4)** — cheap, removes false claims |
| 6 | **DB-driven service catalog** — replace 7 filler cards with `TypingService` rows; expose `BaseFee`/`Description`/`IsActive` | `Index.cshtml:96-174` "Service 1..5" + lorem ipsum; only 2 seeded rows (`ApplicationDbContext.cs:43-46`); `IsActive` unused | S–M | EF Core + optional caching | **Build (P5-5)** — kills the most visible "mockup" smell |
| 7 | **Real `Home/Error` + `Home/Privacy` actions**, honest copy | `Program.cs:101` points exception handler at `/Home/Error` with no action → prod 404; `Privacy.cshtml:6` is template placeholder | S | Error handling hygiene | **Build (P5-6)** — production path must work |

**Explicitly deferred (Tier 2, do not start before P4 is green):** search/filter + paging beyond #3, notifications bell (`Dashboard.cshtml:107-112`), `Account/Profile` (`:120-121`), password-reset flow, i18n/language switches, background jobs/email, Azure Blob storage abstraction.
**Cut (delete the affordance, don't build):** appointments/scheduling (`Index.cshtml:254`, `Dashboard.cshtml:51`), fake Instagram link (`Index.cshtml:243`), full backoffice, HTMX/partial SPA, "Instant Scheduling" marketing copy (`Index.cshtml:191-222`).

## 3. Delete/hide list — dead links to remove in the same burst as the features

| Location | Current | Action |
|---|---|---|
| `Views/Home/Index.cshtml:254` | `Url.Action("Schedule","Appointments")` → 404 | delete link + copy |
| `Views/Home/Index.cshtml:80` | `Url.Action("Track","Document")` → 404 | repoint to #3 |
| `Views/Account/Dashboard.cshtml:36,45,48,51,54` | five `href="#"` sidebar items | repoint Dashboard/Track/My Documents; hide Schedule/Support until built |
| `Views/Account/Dashboard.cshtml:123` | "Log Out" GET to `Home/Index` (doesn't sign out); real POST form at `:59-63` | remove duplicate or convert to POST form |
| `Views/Account/login.cshtml:56` | "Forgot password?" `href="#"` | delete until reset flow is built |
| `Views/Account/Dashboard.cshtml:95-97,120-121`, `Views/Home/Index.cshtml:48-50` | language/profile `#` links | static "EN" label / hide |
| `Views/Account/Dashboard.cshtml:107-112` | hardcoded "New alerts" dot | hide badge |
| `Controllers/HomeController.cs:37-40` | `Dashboard()` action with no view; commented legacy block `:1-24`; mismatched namespace `:29` | delete action + dead code, fix namespace |
| `Controllers/AcoountController.cs` | filename typo vs class `AccountController` | rename file |

## 4. Technical modernization checklist (for P1–P4)

Each item has an official source in §6; this is the "how to make it better" half of the research.

1. **Identity:** keep strict policy; set `ConfigureApplicationCookie` *after* `AddDefaultIdentity` — `HttpOnly`, `SecurePolicy=Always` (account for local HTTP dev), `SameSite=Lax`, `ExpireTimeSpan`, sliding expiry, `LoginPath`/`AccessDeniedPath`. Lockout only works with `lockoutOnFailure: true` (`AcoountController.cs:60` currently `false`).
2. **Authorization:** prefer a fallback policy requiring authenticated users plus `[AllowAnonymous]` on public endpoints; `UseAuthentication()` before `UseAuthorization()`. Needed for the admin decision in §1.
3. **Data Protection:** currently logs "No XML encryptor configured" on startup. Persist keys to a mounted volume/provider with `ProtectKeysWith...`, otherwise cookies/antiforgery break on container restart.
4. **Antiforgery:** `AutoValidateAntiforgeryToken` globally for MVC; AJAX upload uses `IAntiforgery.GetAndStoreTokens(...).RequestToken` in `X-XSRF-TOKEN` header (`AddAntiforgery(o => o.HeaderName = ...)`), `credentials: 'same-origin'`. If token is in both header and form, only the header is validated — useful for streaming uploads.
5. **Migrations:** startup `Database.Migrate()` is acceptable only for simple deployments (EF 9+ has a DB-wide lock); preferred pattern is a migration bundle/SQL script run as a one-shot deploy job. For this portfolio: keep startup migration in Development but **fail loudly in Production** (currently swallowed at `Program.cs:92-95`), and add `dotnet ef migrations has-pending-model-changes` to CI.
6. **Docker:** .NET 8+ images default to **8080**, not 80 (`Dockerfile:24` exposes 80; compose `:20` healthchecks `curl` on `/`). Set `ASPNETCORE_HTTP_PORTS=80` or move mapping to 8080; add `MapHealthChecks("/healthz")` and a healthcheck that does not assume `curl` exists.
7. **CSP:** replace `'unsafe-inline' 'unsafe-eval'` + 3 CDNs (`Program.cs:119-126`) with nonce/SRI-based policy; maintainable approach is vendoring Bootstrap/FontAwesome locally and dropping CDNs.
8. **Uploads:** dedicated storage outside app tree, execute permissions off, random filenames (already done), magic bytes (already done), size limits, quarantine/virus scan as future work.
9. **Tests/CI:** xUnit + `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory<Program>`; integration tests for login/register, form submit, upload validation, and the IDOR regression. CI: `checkout` → `setup-dotnet('10.0.x')` → `restore` → `build -c Release --no-restore` → `test --no-build -c Release`.
10. **Dependency audit:** NuGetAudit already flags NU1903; remediate with `dotnet list package --vulnerable --include-transitive` + `dotnet package update --vulnerable` (or pin a fixed version / add a direct reference for the transitive `SQLitePCLRaw.lib.e_sqlite3`). Promote NU1901–NU1905 to errors in CI.

## 5. Credibility signals & anti-patterns

Supported by official sources (§6) where cited; **[inference]** where not.

- Reviewers check a README that says what/why/how-to-run, a runnable clean-clone path with seeded demo account, tests/CI evidence, screenshots/diagram, and **honest scope/limitations** — Microsoft reference apps (eShopOnWeb, Relecloud) all do this. **[inference]** on "reviewers check" framing.
- Anti-patterns: feature stuffing/tutorial soup; claims with no runnable evidence; dead links and stale demo URLs; real PII/hardcoded secrets. Microsoft samples explicitly scope themselves *down* and document omissions.
- Highest credibility per unit effort remains **CI + tests including the IDOR regression** (agreed by tradeoff and portfolio lanes) — a reviewer runs `dotnet test` and sees both quality and security awareness.
- One coherent portal workflow (apply → track → review → status history) beats ten disconnected demo features.

## 6. Sources

Official Microsoft Learn (all `?view=aspnetcore-10.0` / efcore-10.0 unless noted):
- Identity configuration: `https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0`
- Auth cookies / SameSite: `.../security/authentication/cookie?view=aspnetcore-10.0`, `.../security/samesite?view=aspnetcore-10.0`
- Authorization policies: `.../security/authorization/policies?view=aspnetcore-10.0`
- Data Protection defaults/providers/web farm: `.../security/data-protection/configuration/default-settings?view=aspnetcore-10.0`, `.../implementation/key-storage-providers?view=aspnetcore-10.0`, `.../host-and-deploy/web-farm?view=aspnetcore-10.0`
- Rate limiting: `.../performance/rate-limit?view=aspnetcore-10.0`
- Antiforgery: `.../security/anti-request-forgery?view=aspnetcore-10.0`
- File uploads: `.../mvc/models/file-uploads?view=aspnetcore-10.0`
- CSP (Blazor-oriented; MVC application is inference): `.../blazor/security/content-security-policy?view=aspnetcore-10.0`
- Integration tests: `.../test/integration-tests?view=aspnetcore-10.0&pivots=xunit`
- Health checks: `.../host-and-deploy/health-checks?view=aspnetcore-10.0`
- Docker intro (8080 default): `https://learn.microsoft.com/en-us/dotnet/core/docker/introduction`
- NuGet audit / update / list: `https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages`, `https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-update`, `.../dotnet-package-list`
- CI for .NET: `https://learn.microsoft.com/en-us/dotnet/devops/dotnet-test-github-action`
- Modern web app guide: `https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/`

GitHub/reference:
- About READMEs: `https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes`
- eShopOnWeb (seeded demo account, testing, scope statement): `https://github.com/dotnet-architecture/eShopOnWeb`
- Relecloud modern web app pattern (now **archived** — itself a caution about link rot): `https://github.com/Azure/modern-web-app-pattern-dotnet`

## 7. Founder decisions before implementation

1. **Permission to publish** the client's code (highest risk; no LICENSE file exists). If denied → private repo + public case-study, and **cut the admin feature** per §1.
2. **Audience**: technical reviewers vs visual/non-technical. Technical → this ranking stands; visual → front-load P6 screenshots/UI.
3. **Admin yes/no** (§1) and **SQLite vs Postgres** (§1).
4. **Demo-data policy**: deterministic seed with fake users/applications vs current 2-row seed — needed for reviewer walkthrough and screenshots.

Next session signpost: *"FlyzoneMockup — implement P1/P2; research at `docs/RESEARCH.md`, plan at `docs/GAMEPLAN.md`; clone at `/home/shafin/Opencode/Flyzone/FlyzoneMockup`; SDK at `~/.dotnet`."*
