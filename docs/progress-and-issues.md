# OxyGenAI – Progress and Issues

_Snapshot: 2026-11-25. Project: Junior Project DTETI UGM, Group 2._

OxyGenAI estimates the environmental impact (energy, CO₂, water) of generative-AI use.
Requirement for the course: **the codebase is C# and the output is a Windows application** (Blazor is allowed).

---

## 1. Summary

| Area | Status |
| :--- | :--- |
| Core web app (accounts, usage tracking, Ask Gemini) | Working |
| Trend dashboard | Working, not yet checked in a browser by a second person |
| Admin panel | Working (service and access rules tested); button clicks not yet tested in a browser |
| Impact formula | Implemented from Google's published method; same factor for every model |
| Windows-app output | **Partly met**: self-contained `OxyGenAI.exe` that opens the browser; no native window yet |
| Automated tests | None |
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
| Dashboard (`/dashboard`) | Daily (14 days), weekly (12 weeks) and monthly (12 months) totals in the browser's time zone, plus per-model totals. |
| Admin panel (`/admin`) | List, add, edit email, suspend or reactivate, grant or revoke the Admin role, platform-wide totals. |
| Database | PostgreSQL via EF Core; migrations applied at startup. Hosted on Neon for development. |
| Packaging | `publish.ps1` builds a self-contained zip; `Setup`/`Start` `.bat` scripts; user guide in `docs/USER_GUIDE.md`. |

### Not done
| Feature | Notes |
| :--- | :--- |
| Native Windows window | Plan: WinForms shell hosting the Blazor app in WebView2 (see decisions, ADR-007). |
| Per-model environmental factors | Planned `EnvironmentalFactor` table with a source column. Needed before model comparison. |
| Model comparison UI | `AIComparison` exists in the Domain project only; it depends on the factor table. |
| Model picker for Gemini | Only one model is configured (`Gemini:Model`). |
| Automatic detection of AI apps | `AIUsageLog.ScanDeviceActivity` scans OS processes; it only makes sense inside a desktop app, not on a web server. |
| CSV export | Exists in the WinForms lab project only. |
| Email sending | `IdentityNoOpEmailSender`: forgot-password and confirmation emails are not sent. |
| Automated tests | No test project. |

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
| 2 | **Gemini free quota is small for the default model.** `gemini-2.5-flash` allows 5 requests/min and 20 requests/day on the free tier shown in AI Studio. | A few testers can use it up in one day (HTTP 429). | Switch the default to a Flash-Lite model (15/min, 500/day); verify the model id with the models endpoint. |
| 3 | **`gemini-2.5-flash` is deprecated.** From 2026-12-01 requests are routed to `gemini-3.5-flash-lite`. | Behaviour and quota change silently. | Set the model explicitly. |
| 4 | **Free-tier prompts may be used by Google to improve its products.** | Privacy for testers. | Tell testers not to enter sensitive text. |
| 5 | **Every Gemini model gets the same impact factor.** Google publishes one median figure, not one per model. | Cannot compare models honestly. | Per-model table with clearly labelled assumptions (ADR-005). |
| 6 | **Token scaling is an assumption.** The 500-token reference prompt is ours, not Google's. | Absolute numbers are rough. | Keep the limitation visible in the UI and report. |
| 7 | **Two user models.** `ApplicationUser` (Identity, used) and Domain `User/Admin/RegisteredUser` (not stored; plain-text password field). | Confusing; the Domain classes must never be persisted as they are. | Keep Identity for authentication (ADR-003); mark Domain user classes as design-only or remove them. |
| 8 | **Admin bootstrap.** The account named in `Admin:Email` becomes admin when it registers; there is no email confirmation. | If the admin does not register first, someone else could register that address. | Register the admin account first; keep `Admin:Email` out of public config until then. |
| 9 | **Suspended users keep their session for up to about 30 minutes.** Identity revalidates the security stamp every 30 minutes. | A suspended user may still browse briefly. New logins are refused immediately. | Shorten the revalidation interval if needed. |
| 10 | **Role changes need a new sign-in.** Roles are stored in the cookie. | A newly promoted admin must log in again. | Document it. |
| 11 | **No automated tests.** | Regressions are caught only by manual testing. | Add a test project for the Domain and services. |
| 12 | **Repository hygiene.** A SQLite file `app.db` from the project template is still committed; README mentions ASP.NET Core 9 and SQL Server. | Misleading. | Remove `app.db`; correct README (net10.0, PostgreSQL). |
| 13 | **Neon free database sleeps when idle.** | The first request after a pause can take seconds. | Warn testers; retry once. |
| 14 | **Secrets.** Connection strings and API keys live in user-secrets, environment variables or `oxygenai.settings.json` (git-ignored). | A key pasted into chat or committed is compromised. | Never paste or commit keys; rotate any key that was exposed. |

---

## 5. Suggested next steps (priority order)

1. Decide the Windows-app question (issue 1) with the lecturer.
2. Choose and set the Gemini model(s) (issues 2–3).
3. Black-box test pass on the packaged build (checklist below).
4. Per-model factor table and model comparison (issue 5).
5. Repository cleanup (issue 12) and a first test project (issue 11).

## 6. Black-box test checklist

- Register with a valid and an invalid email/password; log in; log out; log in with a wrong password.
- Open `/`, `/dashboard`, `/ask-gemini` while logged out (expect redirect to login) and logged in.
- Usage tracker: valid record; zero or negative query count; very large numbers; empty model name.
- Dashboard: switch Daily / Weekly / Monthly; empty state for a new account; record appears after saving.
- Ask Gemini: with and without an API key; empty prompt; very long prompt; rate-limit message.
- Admin (admin account): add user, edit email, suspend (then try to log in as that user), reactivate, grant and revoke admin; try to suspend yourself and the last admin.
- Admin page as a normal user (expect "Access denied").
- Packaged build: unzip, run `Setup OxyGenAI.bat`, `Start OxyGenAI.bat` on a PC without the .NET SDK.
