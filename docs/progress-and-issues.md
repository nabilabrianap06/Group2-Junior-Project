# OxyGenAI – Progress and Issues

_Snapshot: 2026-11-25. Project: Junior Project DTETI UGM, Group 2._

OxyGenAI estimates the environmental impact (energy, CO₂, water) of generative-AI use.
Requirement for the course: **the codebase is C# and the output is a Windows application** (Blazor is allowed).

---

## 1. Summary

| Area | Status |
| :--- | :--- |
| Core web app (accounts, usage tracking, Ask Gemini) | Working |
| Gemini model choice and compare | Working: two models offered (`gemini-3.5-flash-lite`, `gemini-3.6-flash`); both answered a test call on 2026-11-25 |
| Usage limits | Working: prompt length, daily allowance per user, pause between requests |
| Trend dashboard | Working, not yet checked in a browser by a second person |
| Admin panel | Working (service and access rules tested); button clicks not yet tested in a browser |
| Impact formula | Implemented from Google's published method; same factor for every model |
| Windows-app output | **Partly met**: self-contained `OxyGenAI.exe` that opens the browser; no native window yet |
| Automated tests | 59 xUnit tests, all passing (`dotnet test AIEnvironmentalTracker.slnx`) |
| Cloud deployment | Not done (not required for a Windows app; see decisions) |

---

## 2. Feature status

### Done
| Feature | Notes |
| :--- | :--- |
| Register, login, manage account | ASP.NET Core Identity. No email confirmation. |
| Usage tracker (`/`) | Manual input of per-query water, CO₂, energy; totals, recommendations, last 20 records. |
| Ask Gemini (`/ask-gemini`) | Calls the Gemini REST API, reads token counts, estimates impact, saves the record. Needs an API key. |
| Impact estimation | `TokenImpactEstimator`: energy → carbon → water chain (Google, arXiv:2508.15734). Sources and limitations are in the file header. |
| Model choice | Dropdown on Ask Gemini; the allowed ids are in `Gemini:AllowedModels`, so only those can be sent to Google. |
| Compare models (`/compare`) | Same prompt to two or more models; prompt, answer and thinking tokens, estimated impact, ranking by estimated CO₂. Each answer is saved as usage. |
| Usage limits | `AskLimiter` (daily allowance per user, minimum pause) and a server-side prompt length cap. Counters are in memory. |
| Dashboard (`/dashboard`) | Daily (14 days), weekly (12 weeks) and monthly (12 months) totals in the browser's time zone, plus per-model totals. |
| Admin panel (`/admin`) | List, add, edit email, suspend or reactivate, grant or revoke the Admin role, platform-wide totals. |
| Database | PostgreSQL via EF Core; migrations applied at startup. Hosted on Neon for development. |
| Packaging | `publish.ps1` builds a self-contained zip; `Setup`/`Start` `.bat` scripts; user guide in `docs/USER_GUIDE.md`. |

### Not done
| Feature | Notes |
| :--- | :--- |
| Native Windows window | Plan: WinForms shell hosting the Blazor app in WebView2 (see decisions, ADR-007). |
| Per-model energy figures | Not available: Google publishes one figure for Gemini, not one per model (ADR-005). Needs real measurements or a published source before any difference between models can be claimed. |
| Other LLM providers | Future work (ADR-010). |
| Automatic detection of AI apps | `AIUsageLog.ScanDeviceActivity` scans OS processes; it only makes sense inside a desktop app, not on a web server. |

Not planned (they were never part of the README or the class diagram): sending email, CSV export, an audit log and persisting `CarbonFootprintReport` as a table.

---

## 3. What has been verified

| Check | Result |
| :--- | :--- |
| `dotnet build` of the solution | 0 errors, 0 warnings |
| Migrations against PostgreSQL (Neon) | Applied at startup; 10 tables created |
| Register → protected pages (HTTP) | Works; anonymous users are redirected to login |
| Dashboard aggregation (service, test data in Neon) | Daily, weekly, monthly buckets, empty buckets, time-zone boundary: passed |
| Admin service (18 checks) and access rules over HTTP | Passed: anonymous → login, normal user → access denied, admin → panel; suspended user → lockout page |
| Impact formula (12 checks) | Reproduces Google's figures: 0.24 Wh → 0.0326 g CO₂e and 0.253 mL water |

**Not verified:** clicking the dashboard and admin pages in a browser, the Ask Gemini page with a real API key, the packaged installer on a clean PC.

---

## 4. Known issues and risks

| # | Issue | Impact | Suggested action |
| :-- | :--- | :--- | :--- |
| 1 | **Windows-app requirement only partly met.** `OxyGenAI.exe` runs a local web server and opens the browser. | May be judged as a web app. | Confirm with the lecturer; if needed build the WinForms + WebView2 shell (small). |
| 2 | **Gemini free quota is shared by all users.** The default is now `gemini-3.5-flash-lite` (15 requests/min, 500/day per project in AI Studio); `gemini-3.6-flash` allows only 20/day. Google also answers HTTP 503 when busy: `gemini-3.1-flash-lite`, `gemini-3.8-flash` and `gemini-4-hydrogen` returned 503 and `gemini-2.5-flash-lite` returned 404 in the test on 2026-11-25, so they are not offered. | Many testers can exhaust the project quota; a model can be unavailable at any time. | Per-user allowance is in place (`AskLimits`). Re-test the 503 models later and add them to `Gemini:AllowedModels`. |
| 3 | ~~`gemini-2.5-flash` is deprecated (routed to `gemini-3.5-flash-lite` from 2026-12-01).~~ | Resolved: the default model is set explicitly to `gemini-3.5-flash-lite`. | – |
| 4 | **Free-tier prompts may be used by Google to improve its products.** | Privacy for testers. | Tell testers not to enter sensitive text. |
| 5 | **Every Gemini model gets the same energy per token.** Google publishes one median figure, not one per model, and price tiers say nothing about energy. | The comparison ranks models by tokens used for a prompt, not by energy per token. | Say so in the UI (done). Add per-model figures only from a published or measured source (ADR-005). |
| 6 | **Token scaling is an assumption.** The 500-token reference prompt is ours, not Google's. | Absolute numbers are rough. | Keep the limitation visible in the UI and report. |
| 7 | **Two user models.** `ApplicationUser` (Identity, used) and Domain `User/Admin/RegisteredUser` (not stored; plain-text password field). | Confusing; the Domain classes must never be persisted as they are. | Keep Identity for authentication (ADR-003); mark Domain user classes as design-only or remove them. |
| 8 | **Admin bootstrap.** The account named in `Admin:Email` becomes admin when it registers; there is no email confirmation. | If the admin does not register first, someone else could register that address. | Register the admin account first; keep `Admin:Email` out of public config until then. |
| 9 | **Suspended users keep their session for up to about 30 minutes.** Identity revalidates the security stamp every 30 minutes. | A suspended user may still browse briefly. New logins are refused immediately. | Shorten the revalidation interval if needed. |
| 10 | **Role changes need a new sign-in.** Roles are stored in the cookie. | A newly promoted admin must log in again. | Document it. |
| 11 | ~~No automated tests.~~ | Resolved: 59 xUnit tests (estimator, trend, admin rules, limiter, Gemini client with a fake HTTP handler, comparison). | Keep adding tests with each feature. Black-box testing of the UI is still manual. |
| 12 | ~~Repository hygiene: `app.db` and an outdated README.~~ | Resolved: `app.db` removed, README corrected. | – |
| 13 | **Neon free database sleeps when idle.** | The first request after a pause can take seconds. | Warn testers; retry once. |
| 14 | **Secrets.** Connection strings and API keys live in user-secrets, environment variables or `oxygenai.settings.json` (git-ignored). | A key pasted into chat or committed is compromised. | Never paste or commit keys; rotate any key that was exposed. |

---

## 5. Suggested next steps (priority order)

1. Decide the Windows-app question (issue 1) with the lecturer.
2. Run Black-box test 23 (a successful Gemini reply) and the new pages in a browser (checklist below).
3. Re-test the Gemini models that answered 503 and add the stable ones to `Gemini:AllowedModels` (issue 2).
4. Build the WinForms + WebView2 shell if the lecturer requires a native window (ADR-007).
5. Future work: other LLM providers (ADR-010) and per-model energy figures from a real source (issue 5).

## 6. Black-box test checklist

- Register with a valid and an invalid email/password; log in; log out; log in with a wrong password.
- Open `/`, `/dashboard`, `/ask-gemini` while logged out (expect redirect to login) and logged in.
- Usage tracker: valid record; zero or negative query count; very large numbers; empty model name.
- Dashboard: switch Daily / Weekly / Monthly; empty state for a new account; record appears after saving.
- Ask Gemini: with and without an API key; empty prompt; a prompt over 4,000 characters; each model in the list; the daily-allowance and pause messages; rate-limit message.
- Compare models: two models; one model ticked only (button disabled); a model that returns an error; check that each answer appears in the usage history.
- Admin (admin account): add user, edit email, suspend (then try to log in as that user), reactivate, grant and revoke admin; try to suspend yourself and the last admin.
- Admin page as a normal user (expect "Access denied").
- Packaged build: unzip, run `Setup OxyGenAI.bat`, `Start OxyGenAI.bat` on a PC without the .NET SDK.
