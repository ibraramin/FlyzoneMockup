# Resume evidence — FlyzoneMockup

Owner: [owner: orchestrator]. Verified 2026-11-27 against `/home/shafin/Opencode/Flyzone/FlyzoneMockup`.

Every bullet below is paired with a command that was actually run. Prefix all `dotnet` commands with `export PATH="$HOME/.dotnet:$PATH"`. Numbers are observed results, not estimates. Nothing here required publishing the client-derived code.

| Resume bullet | Command actually run | Observed result |
|---|---|---|
| Turned an old client mockup into a finished, verifiable ASP.NET Core 10 MVC app | `dotnet build -c Release --no-restore -warnaserror` | `Build succeeded`, **0 warnings, 0 errors** |
| Built an executable security/quality suite instead of claiming quality | `dotnet test -c Release --no-build` | **22 passed, 0 failed** (20 test declarations; one theory expands to 3 cases) |
| Removed a high-severity transitive NuGet advisory | `dotnet list package --vulnerable --include-transitive` | "no vulnerable packages" for **both** `FlyzoneMockup` and `FlyzoneMockup.Tests` |
| Fixed an IDOR on document uploads and added magic-byte validation | `dotnet test -c Release --no-build --filter FullyQualifiedName~Upload_RejectsAttachingDocumentToAnotherUsersApplication` | **1 passed**; ownership check at `Controllers/ServicesController.cs:211-222`, signature check at `:224-231`, owner-gated download at `:296-341` |
| Hardened CSP, auth cookies and production migration behavior; eliminated inline/eval scripts | `curl -s -D - -o /dev/null http://127.0.0.1:5211/ \| grep -i '^content-security-policy'` | `script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com`; no `unsafe-inline`/`unsafe-eval` in `script-src`; `connect-src 'self'`; `frame-ancestors 'none'` |
| Replaced mockup filler with a DB-driven EF Core catalog and a real migration | `curl -s http://127.0.0.1:5211/ \| grep -o 'Business License Setup\|From AED 1,450'`; `python3 -c "import sqlite3; c=sqlite3.connect('/var/tmp/flyzone-p5c3/shots.db'); print(c.execute('select MigrationId from __EFMigrationsHistory').fetchall(), c.execute('select count(*) from TypingServices').fetchone())"` | both strings present on the landing page; two applied migrations and **6** seeded catalog rows |
| Proved the catalog-bound service id is not a magic integer | `dotnet test -c Release --no-build --filter FullyQualifiedName~GoldenVisaSubmission_UsesCatalogServiceId_NotHardcodedInteger` | **1 passed**; the test re-keys the seeded row to id **101**, so a restored `ServiceId = 1` would fail (`tests/FlyzoneMockup.Tests/ServiceCatalogTests.cs:54-97`) |
| Removed every dead UI affordance | `grep -rn 'href="#"' Views --include=*.cshtml \|\| echo none` | `none` |
| Delivered a one-command container startup with healthcheck and persistent data | `docker compose config -q`; `docker compose -p flyzone-evidence up --build -d`; `docker inspect --format '{{.State.Health.Status}}' flyzone-evidence-web-1`; `curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5000/`; `docker compose -p flyzone-evidence down` | config OK; image built; **healthy**; **HTTP 200**; 6 seeded services in the mounted DB; container removed cleanly |
| Documented scope decisions and captured fresh, honest portfolio evidence | `ls docs/adr/*.md \| wc -l`; `ls imgs/*.png \| wc -l` | **5** ADRs and **10** screenshots, recaptured from a clean DB after a real registration + Golden Visa submission |

## Notes on honesty

- All work is in **one local commit** (`git log -1`); no push, publication or license grant was made — the code remains client-derived and private.
- No admin role and no demo-data seeding: a single real submission demonstrates the timeline. See `docs/adr/0004-no-admin-queue-this-burst.md`.
- Earlier phases (P1–P4) have their own claim→evidence tables in `docs/GAMEPLAN.md` §6b/§6c; this file records only what was re-run or first added in P5-B/P5-C.
- The hero uses a self-contained region graphic (no map API key). A first screenshot pass showed a failed third-party tile watermark; it was replaced and the screenshot recaptured and visually re-checked.
