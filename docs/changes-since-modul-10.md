# Changes since Module 10 (`modul-10-mvp`)

_Snapshot: 2026-11-25. Comparison of the Module 10 deliverable with branch `533008`._

| | Commit | Description |
| :--- | :--- | :--- |
| **Baseline** | `fef4b7f` on `origin/modul-10-mvp` | Module 10 MVP: installable package, user guide, black-box test fixes (Worksheet Homework 10, 24 Nov 2026) |
| **Compared** | `0fc0d8a` on `533008` | Baseline plus the merge into `main` and five commits listed in section 6 |
| **Plus, not yet committed** | working tree | The Windows shell, packaging, documentation and test changes of section 6 (last row) |

`533008` is 0 commits behind and 6 commits ahead of `modul-10-mvp`, so nothing from Module 10 is missing. Against the baseline: 33 files changed, +2,668 / -42 lines (the npm lock file excluded).

---

## 1. Summary

- **Module 10 is intact.** Every file of the MVP that is not listed below is unchanged: `UsageRecord`, `ApplicationDbContext`, the migration, `Home.razor` (usage tracker), the Usage components, `publish.ps1`, `packaging/`, and the Domain classes other than `TokenImpactEstimator`.
- **Added (new files):** dashboard, admin and compare pages; admin, limiter and comparison services; the Windows shell project (WinForms + WebView2); an automated test project; project docs; Neon configuration.
- **Changed in place:** registration, navigation, routing, startup, usage service, `GeminiService` (model choice, server-side prompt cap), the Ask Gemini page, `appsettings.json`, README and user guide.
- **Rewritten (one file):** `TokenImpactEstimator`. Same public surface, new calculation.
- **Deleted:** the SQLite file `Data/app.db`, a leftover of the project template that no code used. Nothing else from Module 10; only a few lines of explanation text and 1 line in `Routes.razor` were replaced.

## 2. New (did not exist in Module 10)

| Item | Purpose |
| :--- | :--- |
| `Components/Pages/Dashboard.razor` (`/dashboard`) | Daily, weekly and monthly totals and a per-model table |
| `Services/UsageTrackingService.cs` → `GetTrendAsync` (added to an existing class) | Aggregation behind the dashboard, in the user's time zone |
| `Components/Pages/Admin.razor` (`/admin`) | Admin panel: platform totals, add, edit, suspend accounts, grant or revoke the Admin role |
| `Services/AdminService.cs` | Admin operations on ASP.NET Core Identity, with safety guards |
| `Components/Pages/Compare.razor` (`/compare`) | Same prompt to two or more Gemini models, ranked by estimated impact |
| `Services/ModelComparisonService.cs` | Runs the comparison, saves each answer as usage, uses the Domain class `AIComparison` for the score |
| `Services/AskLimiter.cs` | Per-user daily allowance and pause between Gemini requests (in memory) |
| `tests/AIEnvironmentalTracker.Tests` | 98 xUnit tests on an in-memory database (and the shell's logic); added to `AIEnvironmentalTracker.slnx` |
| `src/AIEnvironmentalTracker.Desktop` | The Windows app: a WinForms window with WebView2 that starts the web app in the background (ADR-007). Files: `MainForm`, `ServerProcess`, `ServerLocator`, `UrlPolicy`, `OutputTail`, `KillOnCloseJob`, `LaunchOptions`, `CheckResult`, `Program` |
| `docs/progress-and-issues.md`, `docs/decisions-and-system-design.md`, this file | Project documentation |
| `neon.ts`, `package.json`, `package-lock.json`, `.gitignore` entries | Neon project configuration (not used by the .NET app at runtime) |

## 3. Changed in existing files

| File | Change |
| :--- | :--- |
| `Program.cs` | `AddRoles<IdentityRole>()`, register `AdminService`, create the Admin role and promote `Admin:Email` at startup (+15 lines, nothing removed) |
| `Components/Account/Pages/Register.razor` | After registration, promote the account if its email equals `Admin:Email` (+5) |
| `Components/Layout/NavMenu.razor` | Links to Dashboard and, for admins only, Admin (+12) |
| `Components/Routes.razor` | A signed-in user without access now sees "Access denied" instead of being sent to the login page |
| `Components/Pages/AskGemini.razor` | Model dropdown, limiter check, server-side prompt length, new explanation text |
| `Services/GeminiService.cs` | `GenerateAsync(prompt, model)`, `AllowedModels` allow-list (default first), prompt length cap, the reply carries the model that was called (+39 lines) |
| `appsettings.json` | Default model `gemini-3.5-flash-lite`, `Gemini:AllowedModels`, `Gemini:MaxPromptCharacters`, `AskLimits` |
| `Program.cs` (second step) | Registers `ModelComparisonService`, `AskLimiter`, `TimeProvider` and the `AskLimits` options |
| `README.md`, `docs/USER_GUIDE.md` | Formula, models, compare, limits, admin, tests and the Windows window described; outdated README rows removed (YARP, Google.GenAI, wrong folder tree) |
| `publish.ps1` | Also publishes the Windows shell into the same folder as the server; removes the NuGet documentation files |
| `AIEnvironmentalTracker.slnx` | Adds the Desktop and the test project |

## 4. Rewritten: `TokenImpactEstimator`

The Module 10 version scaled Google's three published figures independently:

```
water = 0.26 mL x (tokens / 500)   CO2 = 0.03 g x (tokens / 500)   energy = 0.24 Wh x (tokens / 500)
```

The new version follows Google's accounting chain (arXiv:2508.15734) and derives carbon and water from energy:

```
energy  = 0.24 Wh x (tokens / 500)
CO2e    = energy x 94 g/kWh + 0.010 g x (tokens / 500)
water   = (energy - overhead, PUE 1.09) x 1.15 L/kWh
```

- **Compatibility kept:** `ReferenceEnergyWh`, `ReferenceCo2Grams`, `ReferenceWaterMl`, `ReferenceTokenCount` and `CreateFactor(modelName, totalTokens)` still exist, so callers (`AskGemini.razor`) did not change. New: an optional `ImpactParameters` argument, `Estimate(...)` and the `ImpactEstimate` result.
- **Sources and limitations** are written at the top of the file.

Effect on numbers for a 500-token request:

| Metric | Module 10 | Now | Reason |
| :--- | :--- | :--- | :--- |
| Energy | 0.24 Wh | 0.24 Wh | unchanged |
| CO₂e | 0.03 g | 0.0326 g | operational 0.0226 g + embodied 0.010 g, instead of the rounded published figure |
| Water | 0.26 mL | 0.2532 mL | formula result; the paper's 0.26 mL is its rounded headline |

Values scale linearly with tokens in both versions. Manual records (usage tracker) are not affected.

## 5. Re-implemented concept (deliberate duplication)

The Domain project already had an `Admin` class (`AddUser`, `EditUser`, `SuspendUser`) from the UML. It keeps users in an in-memory list and is not stored in the database, and `User` keeps a plain-text password. The admin panel therefore does **not** use it: `AdminService` implements the same ideas (add, edit, suspend) on ASP.NET Core Identity. See ADR-003 in `docs/decisions-and-system-design.md`. The Domain classes are unchanged and still unused by the web app.

## 6. Commits after the baseline

| Commit | Content |
| :--- | :--- |
| `8f2f1c2` | Merge of `modul-10-mvp` into `main` |
| `094b66f` | Neon project configuration |
| `1e1d6fb` | Dashboard, admin panel, Identity roles |
| `eb90670` | Google energy-carbon-water formula and the two project documents |
| `0cd396a` | This comparison document |
| `0fc0d8a` | Model choice, Compare models, usage limits, test project, `app.db` removed, README corrected |
| _next commit_ | Windows shell (WinForms + WebView2) with its tests, `publish.ps1` for both programs, README, user guide and design documents updated |

## 7. Effect on the Module 10 worksheet

| Worksheet statement | Status |
| :--- | :--- |
| Black-box tests 1–22 (installation, accounts, usage tracker, Ask Gemini errors) | Not affected by the changes; not re-run |
| Test 23 (successful Gemini reply) | Still not run (needs a real API key; postponed). The displayed estimate would now be 0.0326 g and 0.2532 mL per 500 tokens |
| Section 2, "Impact formula" row | Outdated: describes the linear scaling of the three figures |
| Endpoint `gemini-2.5-flash` | Outdated: the default is now `gemini-3.5-flash-lite` and the user can pick `gemini-3.6-flash` (`gemini-2.5-flash` is deprecated and routed to `gemini-3.5-flash-lite` from 2026-12-01) |
| Dashboard, admin panel, model choice, Compare models, usage limits | Not covered by any worksheet test case; the services behind them are covered by the automated tests |

---

## 8. Three snapshots: old `main`, after the Module 10 merge, and now

| Snapshot | Commit | Date |
| :--- | :--- | :--- |
| **Old main** | `d5bf87f` | 2026-09-17 |
| **After the Module 10 merge** | `8f2f1c2` | 2026-11-25 |
| **Now** (branch `533008`) | `0fc0d8a` | 2026-11-25 |

| | Old main | After Module 10 merge | Now |
| :--- | :--- | :--- | :--- |
| **Domain project builds?** | No: 17 compile errors | Yes | Yes |
| **Web content** | Identity template: "Hello, world", Counter, Weather, Auth demo | Usage tracker and Ask Gemini | Plus dashboard, admin panel and model comparison |
| **App pages** | none | `/`, `/ask-gemini` | `/`, `/ask-gemini`, `/compare`, `/dashboard`, `/admin` |
| **Database** | SQLite (`app.db`), Identity tables only | PostgreSQL, Identity tables and `UsageRecords`, migrations at startup | same |
| **Services** | none | `GeminiService`, `UsageTrackingService`, `BrowserTimeService`, `UserMessages` | plus `AdminService`, `AskLimiter`, `ModelComparisonService` (7 service files) |
| **Authentication** | Identity, email confirmation required (no email sender, so nobody could sign in) | no email confirmation | plus the `Admin` role |
| **Gemini API** | none | yes, one model (`gemini-2.5-flash`) | model choice (2 models), prompt cap, per-user limits, compare |
| **Impact formula** | none | three published figures scaled by tokens | energy, then carbon, then water (Google chain) |
| **Dashboard (trend)** | no | no | yes |
| **Admin panel** | no (the Domain `Admin` class exists but is not stored) | no | yes (on Identity) |
| **Windows package** | no | yes: `OxyGenAI.exe` that opens the browser (`publish.ps1`, `Setup` and `Start` scripts) | plus `OxyGenAI-Desktop.exe`, a WinForms + WebView2 window around the same app |
| **User guide** | no | yes | updated |
| **README** | plans only; names ASP.NET Core 9 and SQL Server | install section added; some rows still outdated | corrected |
| **Black-box test** | no | 22 of 23 Match (Worksheet 10) | not re-run; new features have no cases yet |
| **Automated tests** | no | no | yes: 98 xUnit tests, all passing |
| **Project documentation** | no | no | yes |
| **App code, approximate lines** (`.cs` and `.razor`, without the Account template, migrations and tests) | about 1,012 | about 1,728 | about 3,019 (plus about 746 lines of tests) |

The line counts are a rough size indicator, not a measure of quality; part of the newest code is source comments.

### Old main
Domain classes derived from the UML that did not compile, and a Web project that was still the template:
- `GetFactorsByModel(string)` was called, but only the two-parameter version existed.
- `decimal` was multiplied with `double` in `AIComparison`.
- `RegisteredUser.Register` was referenced by `Admin` but did not exist.
- A placeholder project `AIEnvironmental/Class1.cs` was still present.

### After the Module 10 merge (42 files, +1,484 / -351)
The first usable application: the Domain compiles, the template pages and the placeholder project are removed, the database moved to PostgreSQL, and the usage tracker, Ask Gemini, the installer package, the user guide and the black-box test were added.

### Now (33 files, +2,668 / -42, compared with the merged `main`)
Almost purely additive: dashboard, admin panel, Identity roles, the full Google formula, Gemini model choice, model comparison, usage limits, the test project and project documentation. The only deletion of substance is the unused SQLite file `app.db`.

### Unchanged in all three snapshots
The Domain classes `User`, `Admin`, `RegisteredUser` and `AIUsageLog.ScanDeviceActivity` exist but are not used by the web application (they belong to the UML and the worksheets). `AIComparison` was unused in the first two snapshots; it is now used by `ModelComparisonService`.

### Backend and frontend split of the newest features

| Feature | Backend (logic and data) | Frontend (page) |
| :--- | :--- | :--- |
| Trend dashboard | `UsageTrackingService.GetTrendAsync`: query, buckets, time zone, per-model totals | `Components/Pages/Dashboard.razor` |
| Admin panel | `AdminService`: accounts, roles, guards, platform totals; startup promotion in `Program.cs` | `Components/Pages/Admin.razor`, menu link in `NavMenu.razor` |
| Impact formula | `TokenImpactEstimator` (Domain project) | explanation text in `AskGemini.razor` |
| Model choice | `GeminiService` allow-list and prompt cap, `appsettings.json` | model dropdown in `AskGemini.razor` |
| Windows window | `ServerProcess` (hidden server on a free port), `ServerLocator`, `KillOnCloseJob` | `MainForm` (WebView2), `UrlPolicy` keeps the window on our own server |
| Compare models | `ModelComparisonService` (+ `AIComparison` in the Domain project) | `Components/Pages/Compare.razor`, menu link in `NavMenu.razor` |
| Usage limits | `AskLimiter`, `AskLimits` settings | error message shown on the Ask Gemini and Compare pages |
