# FlyzoneMockup — Finish & Portfolio Gameplan

Status: **Release prepared for publication (2026-11-27).** Setup/planning in §0–§6; P1–P4 in §6b/§6c; P5-A in §8; P5-B in §9; P5-C in §10; roles/demo data/publication in §11. MIT license and `origin/main` are configured; use `git status`/`git log` for the current push state.
Owner: orchestrator. Last updated: 2026-11-27.
Source of truth: `https://github.com/ibraramin/FlyzoneMockup` @ `f7b7649` ("UI fixes and README update with screenshots", 2026-05-06).
Local clone: `/home/shafin/Opencode/Flyzone/FlyzoneMockup` (clean working tree at `HEAD`; the original source commit is `f7b7649`).

## 0. North-star alignment (founder approval needed)

**Restatement (2 sentences):** The goal is to turn an old client-built ASP.NET Core MVC demo into a genuinely finished, verifiable project that can be cited on a resume; we are in the planning/setup phase, with research on improvements and features explicitly deferred to session 2. This plan serves that goal by making every resume claim checkable (green build, tests, working Docker/DB, honest README) **before** adding features.

**Proposed north-star draft** (not written to `docs/NORTH-STAR.md` because that file is founder-owned): *A portfolio-grade, self-contained ASP.NET Core MVC application whose advertised features all work, whose build/tests/deployment pass from a clean clone with one command, and whose code tells a clear engineering story.* Non-goals: becoming a production SaaS, multi-tenancy, real payment/government integrations, hiding known gaps behind UI polish.

**Drift flag:** "Feature complete" is currently undefined. Proposed working definition for approval: (a) every visible link/button works, (b) README claims match code, (c) zero known high-severity dependency advisories, (d) automated tests + CI green, (e) one-command local and container startup with a fresh, seeded DB. If you disagree with any of (a)–(e), say so before session 2 — it changes the whole plan.

## 1. Setup completed this session (claim → evidence)

| Claim | Command run | Observed result |
|---|---|---|
| Source repo cloned locally | `git clone --depth 1 ...` | clone at path above, HEAD `f7b7649`, clean tree |
| .NET SDK available (was missing) | `curl` official `dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"` | **10.0.404** installed, isolated to `~/.dotnet` (no system change) |
| Dependencies restore | `dotnet restore` | success; warning **NU1903** — `SQLitePCLRaw.lib.e_sqlite3 2.1.11`, high-severity advisory GHSA-2m69-gcr7-jv3q |
| Release build | `dotnet build --no-restore` | **Build succeeded**, 0 errors, 3 warnings (NU1903 above; `Dashboard.cshtml:139` CS8602; `ServicesController.cs:176` CA2022 inexact stream read) |
| App boots | `dotnet run --no-build --urls http://127.0.0.1:5199` + `curl` | `GET /` → 200, `GET /Account/Login` → 200; migrations applied; dev-env HTTPS-redirect warning (no https port) is benign locally |
| Cached DB is stale, not populated with PII | inspected `flyzone.db` + app log | only 2 seed `TypingService` rows, 0 users; file is tracked by git despite `.gitignore` |

Local dev URLs: `http://localhost:5112` (HTTP profile), `https://localhost:7035` (HTTPS profile). Docker compose maps host `5000` → container `80`. README currently says `localhost:5000` for `dotnet run` — wrong (see §3).

## 2. Guiding principle: proof-of-work, not feature count

Two independent reviews agreed: the repo already *claims* a rich feature set (README lists auth, lockout, rate limiting, tracking, dashboard, Docker) but has **zero tests, no CI, and several claims contradicted by code**. Adding features first would make the portfolio story weaker, not stronger: a reviewer who clones and runs it finds the gaps in minutes. Sequence is therefore **trust → evidence → correctness → features → polish**.

## 3. Findings to drive the work (verified, ranked)

1. **Security/correctness:** `Controllers/ServicesController.cs:222` binds a caller-supplied `applicationId` with no ownership check (IDOR on document uploads); `userDocument` static files under `/uploads/<guid>` have no auth gate; `documentType` is unvalidated.
2. **Security/correctness:** `Program.cs:92-95` swallows migration failures — app boots against a broken schema. `Program.cs:119-126` CSP allows `unsafe-inline`/`unsafe-eval` + 3 CDNs, undermining the XSS-protection claim.
3. **README claims vs code:** lockout configured (`Program.cs:63-64`) but `Controllers/AcoountController.cs:60` passes `lockoutOnFailure: false`, so no counter increments; "CSRF on all POSTs" *is* true for the 5 tag-helper forms, but the upload action (`Controllers/ServicesController.cs:148-229`) has `[ValidateAntiForgeryToken]` with no form/token emitted anywhere; "real-time status history" is written to DB but never displayed.
4. **Broken UI affordances:** `Views/Home/Index.cshtml:80,254` link to non-existent `Document/Track` and `Appointments/Schedule`; `Views/Account/Dashboard.cshtml:45-54,95-97,120,123` has dead `#` links and a GET logout against a POST-only action; `RegisterViewModel.cs:28` requires 6 chars while Identity requires 8; `_ValidationScriptsPartial` is never rendered.
5. **Structural debt:** `Controllers/HomeController.cs:29` uses a mismatched namespace and has a dead `Dashboard` action with no view; file/class mismatch `AcoountController.cs`; `Layout = null` on 5 functional pages; hard-coded `ServiceId` 1/2 and two non-transactional `SaveChanges` calls.
6. **Repo hygiene:** 205 tracked files / 14 MB in `Release/` (published binaries + PDBs), tracked `flyzone.db`, no `LICENSE` despite README claiming MIT, no upload directory, no tests, no CI.
7. **Deployment:** `Dockerfile:24` `EXPOSE 80`, compose `:19-20` healthcheck uses `curl http://localhost/`; .NET 10 aspnet images default to port **8080** and are not guaranteed to ship `curl` → container healthcheck/port likely broken. (To confirm with `docker compose up --build` in the deployment phase.)
8. **Dependency risk:** NU1903 high-severity advisory above; `AllowMissingPrunePackageData=true` in `.csproj:7` hints prior restore workarounds.

## 4. Phased roadmap (execution starts only after approval & session 2 research)

| Phase | Goal | Candidate tasks (evidence refs above) | Gate (runnable) |
|---|---|---|---|
| **P0 — Research (session 2, explicitly)** | Decide what "better" and "feature complete" mean; **no code changes** | Research current ASP.NET Core 10 practices; benchmark comparable portfolio apps; survey features: status-history UI, document upload UI, admin/role workflows, appointments, notifications, search/filter, export; produce ROI/effort/risk matrix + shortlist | `docs/RESEARCH.md` exists with cited sources and a ranked feature shortlist |
| **P1 — Trust & hygiene** | Make repo publishable | Resolve ownership/licensing (written permission or keep private), add `LICENSE` or reframe README; untrack `flyzone.db` + `Release/`; secret scan; reconcile README claims; fix NU1903 by upgrading SQLite packages | `git status` clean; `dotnet restore` with **0 NU1903** |
| **P2 — Evidence** | Make progress falsifiable | Add xUnit test project (+ WebApplicationFactory integration tests): login/register, form submit, upload validation, **IDOR regression**; add CI workflow running restore/build/test | `dotnet test` green; CI badge/run link in README |
| **P3 — Correctness & security** | Fix the gaps a reviewer will find | Ownership check on `UploadDocument`; auth gate for uploads; enable lockout; fail migrations loudly in Production; password-policy consistency; transaction for application+history; cookie settings (HttpOnly/Secure/SameSite/expiry); tighten CSP; client-side validation | `dotnet test` green + `dotnet build` with **0 warnings** |
| **P4 — Deployable** | One-command clean start | Fix container port/healthcheck (`ASPNETCORE_HTTP_PORTS=80` or compose→8080, healthcheck via built-in tool/TCP), deterministic seed on fresh volume, verify `docker compose up --build` from clean state | containers healthy; `curl -f localhost:5000/` → 200 with empty DB volume |
| **P5 — Feature completion (shortlist from P0)** | Every visible affordance works | Ranked shortlist and cut list now fixed in `docs/RESEARCH.md` §2–§3: secure upload UI, details/status timeline, track-status, optional minimal admin, validation wiring, DB-driven catalog. Appointments/scheduling is **cut**, not built | `dotnet test` green + browser smoke of every nav link |
| **P6 — Portfolio packaging** | Turn work into resume evidence | Rewrite README (architecture, decisions, screenshots, honest limitations), `docs/adr/` for scope decisions, 3–5 screenshots, resume bullets each paired with a verifiable command | README + ADR + screenshots; `docs/GAMEPLAN.md` marked done |

**Ordering rule:** P1–P4 are non-negotiable before P5/P6. If time is short, stop after P4 with a smaller but fully credible project rather than shipping more half-working features.

## 5. Risks & mitigations

| Risk | Evidence / why it matters | Mitigation |
|---|---|---|
| **Publishing client-owned code (highest)** | "old project for a client"; single-author history; README claims MIT but no LICENSE file | Get written permission before making it public. Alternative: private repo + public case-study writeup with screenshots/diagrams |
| No verifiable progress signal | zero tests, no CI | P2 first; every phase ends with a runnable gate |
| Stale/misleading artifact | tracked `flyzone.db`, 14 MB `Release/`, false README claims | P1 cleanup + deterministic seed + honest README |
| Toolchain / deploy drift | SDK absent at session start; Docker port/healthcheck mismatch | SDK pinned locally (done); P4 container smoke test from clean volumes |
| Disk pressure | 24 GB free on `/home` before SDK install (~0.7 GB used) | Keep images/artifacts lean; no large DB fixtures in git |

**Weakest assumption:** that we are free to publish this client's code publicly. **Alternative if false:** keep the repository private and publish only a case-study (architecture diagram, screenshots, measured build/test evidence, written description of contributions) — the resume goal still holds. **Second assumption to challenge in P0:** that the audience values verifiable engineering evidence over a large feature list; if the audience is non-technical/visual under a deadline, invert the order and front-load P6 UI polish — but that tradeoff must be your explicit call.

## 6. Session 2 brief (explicitly research-only)

Deliver `docs/RESEARCH.md` and a feature shortlist. Questions to answer: (1) What does a current (.NET 10 / 2026) portfolio-grade ASP.NET MVC app need to demonstrate? (2) Which of the dead links/placeholder features are worth building vs deleting? (3) Which features best showcase backend skill (auth, identity, EF Core, background processing, caching, testing) vs generic UI? (4) SQLite vs a containerized Postgres/SQL Server for a resume demo — tradeoffs? (5) Where are the current security patterns outdated? (6) What would a hiring manager actually run to verify the claims? No code changes in session 2; decisions feed P0 → P1.

## 6b. P1/P2 completed (2026-11-27 — verified)

| Claim | Command | Observed result |
|---|---|---|
| Vulnerable transitive SQLite package removed | `dotnet restore -p:NuGetAudit=true -p:TreatWarningsAsErrors=NU1901%3B...%3BNU1905` + `dotnet list package --vulnerable --include-transitive` | 0 warnings/errors; **no vulnerable packages** in either project (direct pin `SQLitePCLRaw.lib.e_sqlite3 3.53.3` in `FlyzoneMockup.csproj`) |
| Clean Release build | `dotnet build -c Release --no-restore -warnaserror` | **Build succeeded, 0 warnings, 0 errors** |
| Automated tests exist and pass | `dotnet test -c Release --no-build` | **9 passed, 0 failed** — public pages, auth gate, upload auth gate, registration, lockout, magic bytes, IDOR regression |
| Tests are isolated | inspected temp DB paths + app config during test runs | each test class uses a throwaway SQLite file; local `flyzone.db` untouched |
| App still boots | `dotnet run -c Release --no-build` on port 5200 with a temp DB + `curl` | `/`, `/Account/Login`, `/Account/Register` → 200 |
| Repo hygiene | `git rm --cached Release`, `git rm --cached flyzone.db`, `git mv AcoountController.cs AccountController.cs` | 205 `Release/` files and `flyzone.db` untracked but still on disk; 0 tracked DB/binaries; file typo fixed |
| Honest license/README | read `README.md` | MIT claim replaced with "no license granted"; ports, lockout, upload/CSRF, status-history, Docker, and limitations corrected |

**Pulled forward from P3** (to make the regression tests meaningful): upload ownership check + `documentType` allow-list (`Controllers/ServicesController.cs`), `lockoutOnFailure: true` (`Controllers/AccountController.cs`), lazy connection-string resolution in `Program.cs` (needed for test isolation), and 4 nullable warnings in `Views/Account/Dashboard.cshtml`. Remaining P3 work: cookie/Data Protection config, CSP hardening, fail-loud production migrations, transactions, client-side validation, dead-link cleanup.

**Not done:** no commits were made (user approval required), no public publishing, no license grant, no Docker rebuild (P4).

## 6c. P3/P4 completed (2026-11-27 — verified)

| Claim | Command | Observed result |
|---|---|---|
| Clean warnings-as-errors build | `dotnet build -c Release --no-restore -warnaserror` | Build succeeded, **0 warnings, 0 errors** |
| Tests green incl. new security coverage | `dotnet test -c Release --no-build` | **13 passed, 0 failed** (9 prior + CSP header, security headers, auth cookie, production migration failure) |
| CSP forbids inline/eval scripts | `curl -s -D - -o /dev/null http://localhost:5000/` | `script-src 'self' https://cdn...` — no `unsafe-inline`/`unsafe-eval`; page scripts moved to `wwwroot/js/home-map.js` + `wwwroot/js/dashboard.js` |
| Auth cookie hardened | test `AuthCookie_IsHttpOnlyAndSameSiteLax` + code inspection `Program.cs` | `.Flyzone.Auth` cookie: test asserts `httponly`, `samesite=lax`; `Secure` is configured for non-Development (not asserted by the test, which runs in Development); 14-day sliding expiry |
| Production migration fails loudly | test `MigrationFailure_InProduction_PreventsStartup` + container log | unusable DB path aborts startup; container log "Database migrations applied successfully." on a valid volume |
| Form submissions are atomic | code inspection `Controllers/ServicesController.cs` | GoldenVisa + DrivingLicense each wrap both saves in one transaction with a single `CommitAsync` |
| Client-side validation wired | code inspection of 4 `Layout = null` views | jQuery + `_ValidationScriptsPartial` added to login/register/GoldenVisa/DrivingLicense |
| Dead links removed | code inspection | `Document/Track` → `Account/Dashboard`; `Appointments/Schedule` → `#contact`; `#` sidebar items removed; profile logout converted to POST form |
| Clean container start | `rm -rf App_Data && docker compose up --build -d` | image built; health = **healthy**; `Now listening on: http://[::]:8080` |
| One-command access | `curl -f http://localhost:5000/` | **HTTP 200** |
| Deterministic seed on fresh volume | copy DB + WAL/SHM out of container, query with `python3 sqlite3` | **2** seeded `TypingServices` rows, **1** EF migration, **1** persisted Data Protection key |
| README claims reconciled | read `README.md` | P4 "being verified" caveats replaced with verified port/URL/healthcheck facts |

**Not done:** no commits (user approval still required), no public publishing, no license grant. P5 features remain out of scope. The container was stopped after the smoke test (`docker compose down`); the built image and bind-mount data remain.

## 7. Out of scope this session

No feature implementation, no security fixes, no repo cleanup, no commits, no Docker rebuild, no web research beyond what was needed to identify the two source repos. Start a fresh session with the signpost: *"FlyzoneMockup — session 2 research; plan at `docs/GAMEPLAN.md`; clone at `/home/shafin/Opencode/Flyzone/FlyzoneMockup`; SDK at `~/.dotnet`."*

## 8. Next session brief — P5-A: applicant workflow + hygiene (planning only)

**Objective:** make the portal's core workflow demonstrable end-to-end (apply → upload → track → status history) and close the remaining cheap hygiene gaps. P6 packaging, admin roles, and new infrastructure stay out of this burst.

**Tasks, in order** (shortlist numbers from `docs/RESEARCH.md` §2):
1. **#1 Secure upload UI + auth-gated download.** Add a multipart Razor form posting to the existing `UploadDocument` endpoint with an antiforgery field token, an `MyDocuments` list action, and an authorized `DownloadDocument` action. Move stored files out of `wwwroot/uploads` into the app-data volume so direct static access is impossible (respect the existing IDOR/magic-byte tests). Repoint the dashboard's "My Documents" affordance.
2. **#2 Application details + status-history timeline.** Add `ServicesController.Details/{id}` with an ownership check and `Include(ApplicationHistories)`, a Razor timeline view, and links from dashboard recent-activity rows. This makes the README's history claim true.
3. **#3 Track-status page.** Add a real `Services/Track` route (lookup by owned application reference, reusing #2's timeline) and repoint the landing-page CTA from the old dead `/Document/Track`.
4. **#5 + #7 Hygiene.** Align `RegisterViewModel.cs:28` min length 6 → 8; add `HomeController.Error` + `HomeController.Privacy` with honest copy; delete the dead `HomeController.Dashboard()` action and commented legacy block; update README limitations (upload UI and history now exist).

**Explicitly out of this burst:** #4 minimal admin (conditional on publication/audience), #6 DB-driven catalog, demo-data seeding, Postgres, P6 screenshots/ADRs/resume bullets, commits.

**Founder decisions (defaults let the work start without blocking):** publication permission (still unresolved — keeps repo private); audience (assume technical); admin yes/no (assume **no** this session); demo-data policy (assume **one real submission** demonstrates the timeline, no seeded fake users). Say so before the session if any default is wrong.

**DoD (runnable):** `dotnet build -c Release --no-restore -warnaserror` → 0 warnings; `dotnet test -c Release --no-build` → all green including new ownership/IDOR tests for Details, Track and Download; authenticated browser/curl smoke of every newly linked affordance; `grep -rn 'href="#"' Views --include=*.cshtml` → no matches.

**Weakest assumption + alternative:** that one work-burst can finish upload UI + details/timeline + track-status without the admin/demo-data decisions. If status transitions must be visible end-to-end, that requires minimal admin or seeded applications — both blocked on founder decisions. Alternative: build only the applicant-side slice (#1–#3 with a single real submission) and defer admin/seed to P5-B.

**Signpost:** *"FlyzoneMockup — P5-A session; brief at `docs/GAMEPLAN.md` §8, shortlist at `docs/RESEARCH.md` §2; clone at `/home/shafin/Opencode/Flyzone/FlyzoneMockup`; SDK at `~/.dotnet`. P3/P4 verified: 13 tests, clean container smoke. Scope: upload UI, details/timeline, track-status, registration/error hygiene."*

---

## 9. P5-B completed (2026-11-27 — verified)

**Objective:** finish the P5 shortlist's most visible gap (DB-driven catalog), close the remaining cut-list items, fold in P5-A review nits, and keep the README strictly honest. No P6 packaging, no admin, no commits.

### 9.1 Claim → evidence

| Claim | Command | Observed result |
|---|---|---|
| P5-A baseline reproduced before edits | `dotnet build -c Release --no-restore -warnaserror` + `dotnet test -c Release --no-build` | 0 warnings / 0 errors; **19 passed, 0 failed** |
| Catalog is data-driven, not markup | `HomeController.Index` injects `ApplicationDbContext`; `Index.cshtml` `@model IReadOnlyList<TypingService>` | active rows only (`IsActive`), ordered by `Id`; category/name/description/fee rendered from the entity |
| Fresh-clone seed covers the grid | `dotnet ef migrations add SeedServiceCatalog` + app boot against `/var/tmp/flyzone-p5b-smoke/smoke.db` | new migration inserts 4 rows and refreshes 2 descriptions; `__EFMigrationsHistory` shows 2 migrations; 6 seeded services |
| Landing page smoke | `dotnet run -c Release --no-build --no-launch-profile` + `curl -s -o ... -w '%{http_code}' http://127.0.0.1:5211/` | **200**; contains `Golden Visa Application`, `Business License Setup`, `From AED 2,800`; contains no `Service 1`, `quick brown fox` or `Instant Scheduling` |
| Auth pages still reachable | `curl` on `/Account/Login`, `/Account/Register` | **200 / 200** |
| Cut list closed | `grep -rn 'href="#"' Views --include=*.cshtml` + code inspection | no matches; fake alerts badge removed; "Instant Scheduling" replaced with honest "Secure Uploads"; ".NET Core MVC" → "ASP.NET Core MVC" |
| Review nit: magic ServiceId removed | code inspection `Controllers/ServicesController.cs` + new test | both forms call `ResolveServiceIdAsync("<catalog name>")`; form submission test asserts the persisted `ServiceId` equals the catalog row |
| Automated coverage | `dotnet build -c Release --no-restore -warnaserror` then `dotnet test -c Release --no-build` | **0 warnings / 0 errors**; **22 passed, 0 failed** (19 prior + 3 catalog tests: active rows, inactive hidden, catalog-bound ServiceId — the latter re-keys the seeded row to id **101**, so the removed literal `ServiceId = 1` would fail the test) |
| README honesty | read `README.md` | six seeded services, DB-driven catalog, upload/details/track workflow, removed-filler note all reflected; limitations unchanged where true |

### 9.2 What changed

- `Data/ApplicationDbContext.cs` + two migration files: catalog seed expanded to six rows; ids 1/2 kept stable and descriptions refreshed.
- `Controllers/HomeController.cs`: constructor injection, async catalog query with `AsNoTracking()`.
- `Views/Home/Index.cshtml`: model-bound service cards, cut-list copy fixes, invariant-culture fee formatting; `Views/_ViewImports.cshtml` imports `System.Globalization`.
- `Views/Account/Dashboard.cshtml`: dead notifications badge removed.
- `Controllers/ServicesController.cs`: `ResolveServiceIdAsync` replaces hard-coded `ServiceId` 1/2.
- `tests/FlyzoneMockup.Tests/ServiceCatalogTests.cs`: three new integration tests.
- `README.md`, this plan: claims reconciled with the running app.

### 9.3 Not done / carried forward

At the end of P5-B: no commits (user approval required) — all work was later recorded in a single local commit (§10). No publishing decision, no license grant. Minimal admin (#4) and demo-data seeding stay cut: the founder has not resolved publication/audience policy, and a single real submission already demonstrates the timeline. P6 screenshots/ADRs/resume bullets became the next burst.

**Next session brief — P5-C (portfolio packaging):** produce `docs/adr/` records for the scope decisions (SQLite, server-rendered MVC, no admin, outside-wwwroot uploads), capture fresh screenshots from a clean container, and write resume bullets each paired with a runnable verification command. Keep build/tests green; do not start admin or demo-data seeding without a founder decision.

**DoD (runnable):** `dotnet build -c Release --no-restore -warnaserror` → 0 warnings; `dotnet test -c Release --no-build` → all green; `curl -f http://localhost:5000/` → 200 from a clean container; every resume bullet cites a command that was actually run; `docs/GAMEPLAN.md` marked done.

**Weakest assumption + alternative:** that the repo stays private/case-study-only, so admin remains cut and the portfolio story rests on verifiable evidence rather than role depth. If publication is approved for a technical audience, build the minimal admin review queue (RESEARCH §1/#4) before packaging instead.

**Signpost:** *"FlyzoneMockup — P5-C session; brief at `docs/GAMEPLAN.md` §9; clone at `/home/shafin/Opencode/Flyzone/FlyzoneMockup`; SDK at `~/.dotnet`. P5-B verified: 22 tests, DB-driven catalog, clean landing-page smoke. Scope: ADRs, screenshots, resume bullets, final README."*

---

## 10. P5-C completed (2026-11-27 — verified)

**Objective:** turn the verified work into portfolio packaging: scope ADRs, fresh screenshots, resume bullets paired with runnable commands, and a final honest README. Explicitly approved follow-up scope included one local commit; admin, demo-data seeding, publishing and licensing remain held.

### 10.1 Claim → evidence

| Claim | Command | Observed result |
|---|---|---|
| Scope decisions recorded | `ls docs/adr/*.md \| wc -l` | **5** ADRs: SQLite, server-rendered MVC, outside-wwwroot uploads, no admin this burst, DB-driven catalog |
| Screenshots are fresh, not stale | browser capture from a clean DB after real registration + Golden Visa submission; `ls imgs/*.png \| wc -l` | **10** screenshots; vision re-check passed for landing page and details/timeline |
| Hero artifact fixed | visual re-check after replacing third-party map tiles with a self-contained CSS region graphic | "API KEY REQUIRED" watermark gone; six catalog cards still render; CSP no longer allowlists `unpkg.com` or CartoDB |
| Resume bullets are checkable | commands in `docs/RESUME-EVIDENCE.md`, each re-run this session | build 0 warnings; 22 tests; IDOR filter 1 passed; catalog filter 1 passed; no vulnerable packages; `href="#"` none |
| Container smoke from current tree | `docker compose config -q`; `docker compose -p flyzone-evidence up --build -d`; health/curl; `docker compose -p flyzone-evidence down` | config OK; image built; **healthy**; `GET :5000/` → **200**; 6 seeded services; container removed |
| Build context hygiene | `docker compose up --build` after `.dockerignore` fix | first attempt failed on root-owned `App_Data/keys` in the build context; added `App_Data/` to `.dockerignore`; rebuild succeeded |
| README honesty | read `README.md` | Leaflet claim removed; static graphic limitation added; new screenshots and ADR/resume-evidence links added |

### 10.2 What changed in P5-C

- `docs/adr/0001`–`0005`: immutable scope decisions with alternatives and verification commands.
- `docs/RESUME-EVIDENCE.md`: 10 resume bullets, each paired with a command and observed result.
- `imgs/`: 10 screenshots recaptured (landing, login, register, dashboard, both forms, upload, my documents, details/timeline, track).
- `Views/Home/Index.cshtml`, `wwwroot/js/home-map.js`, `wwwroot/css/flyzone-landing.css`, `wwwroot/css/site.css`: key-free hero graphic replacing failed map tiles.
- `Program.cs`: CSP no longer allowlists `unpkg.com` or CartoDB; `connect-src 'self'`.
- `.dockerignore`: `App_Data/` excluded from the image build context.
- `README.md`, this plan: final reconciliation.

### 10.3 Final state

P0–P6 packaging was complete at this point and every phase had a runnable evidence table. The holds listed above were **subsequently released** in §11 after the founder's publication/admin/demo-data decisions; `docs/adr/0006` supersedes `docs/adr/0004`.

**Session signpost for any later work:** *"FlyzoneMockup — complete through P5-C; plans at `docs/GAMEPLAN.md` §9–§10, ADRs at `docs/adr/`, evidence at `docs/RESUME-EVIDENCE.md`; clone at `/home/shafin/Opencode/Flyzone/FlyzoneMockup`; SDK at `~/.dotnet`."*

---

## 11. Publication, Admin roles and demo data (2026-11-27 — verified)

Founder decisions released the remaining holds: publish to GitHub with generic licensing and a viewer-friendly README; add Admin roles; demo-data policy delegated. `docs/adr/0006` supersedes ADR-0004.

### 11.1 Claim → evidence

| Claim | Command / artifact | Observed result |
|---|---|---|
| Identity roles work | code + `dotnet test -c Release --no-build --filter FullyQualifiedName~AdminFlowTests` | `.AddRoles<IdentityRole>()`; **5 passed** (admin list/details, non-admin denial/redirect, atomic status+history transition, antiforgery rejection, unknown-status rejection) |
| Admin transition is atomic and auditable | `AdminController.Update` code + admin test | transaction wraps status change, notes and `ApplicationHistory` append; one `SaveChangesAsync` + `CommitAsync` |
| Demo data is opt-in and safe by default | config inspection + test host overrides | `DemoData:Enabled=false` in `appsettings.json`, `true` in Development/Compose, `false` in `TestAppFactory`; public fixtures `demo@flyzone.example` / `admin@flyzone.example` with 3 sample applications and history |
| Full suite still green | `dotnet build -c Release --no-restore -warnaserror`; `dotnet test -c Release --no-build` | **0 warnings / 0 errors**; **27 passed, 0 failed** |
| Admin UI actually renders (screenshot gate) | `/var/tmp/opencode/shots/Flyzone-Admin-Queue.png`, `/var/tmp/opencode/shots/Flyzone-Admin-Review.png`; browser login as demo admin | images captured from the running app; visual inspection recorded in the verification note below |
| Container with demo data | `docker compose -p flyzone-final up --build -d`; health; `curl -f localhost:5000/`; DB query; `docker compose -p flyzone-final down` | **healthy**; **HTTP 200**; 3 demo applications, 1 `Admin` role, 6 catalog services; container removed |
| Viewer-friendly publication | `README.md`, `LICENSE`, `git remote -v`, `git status --porcelain` | MIT license, CI badge, demo-account table, admin screenshots; remote `origin` = `ibraramin/FlyzoneMockup`; clean tree before push |

**Screenshot verification note (hard gate):** `/var/tmp/opencode/shots/Flyzone-Admin-Queue.png` was visually inspected and shows a styled queue with the status filter and exactly three seeded applications (UnderReview, WaitingPayment, Completed) for `demo@flyzone.example`. `/var/tmp/opencode/shots/Flyzone-Admin-Review.png` shows the review form with status select, comment, internal notes, Save Transition button and a two-entry Application Timeline. No blank page, error page, watermark or broken layout. Minor non-blocking observation: seeded applications have zero attached documents, so the seed comment "Documents received" is decorative only.

### 11.2 What changed

- `Program.cs`: role support + opt-in `DemoDataSeeder` invocation; `Data/DemoDataSeeder.cs` new.
- `Controllers/AdminController.cs`, `Views/Admin/{Index,Details}.cshtml`: role-gated review queue and transactional transitions; dashboard admin link.
- Config: `appsettings*.json`, `docker-compose.yml`, `TestAppFactory.cs` (demo flag).
- `tests/FlyzoneMockup.Tests/AdminFlowTests.cs`: 5 new integration tests.
- `LICENSE` (MIT), README (badge, demo accounts, admin section, screenshots), `docs/adr/0006`, `docs/RESUME-EVIDENCE.md`, this plan.

**Final state:** release prepared for `origin/main` with MIT licensing and viewer-facing docs. No further work is queued; any future change should keep the build/test/screenshot gates green and add a new ADR for scope reversals.
