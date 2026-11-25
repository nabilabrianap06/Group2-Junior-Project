# Changes since Module 10 (`modul-10-mvp`)

_Snapshot: 2026-11-25. Comparison of the Module 10 deliverable with branch `533008`._

| | Commit | Description |
| :--- | :--- | :--- |
| **Baseline** | `fef4b7f` on `origin/modul-10-mvp` | Module 10 MVP: installable package, user guide, black-box test fixes (Worksheet Homework 10, 24 Nov 2026) |
| **Compared** | `eb90670` on `533008` | Baseline plus the merge into `main` and three commits listed in section 6 |

`533008` is 0 commits behind and 4 commits ahead of `modul-10-mvp`, so nothing from Module 10 is missing.

---

## 1. Summary

- **Module 10 is intact.** Every file of the MVP that is not listed below is unchanged: `GeminiService`, `UsageRecord`, `ApplicationDbContext`, the migration, `Home.razor` (usage tracker), the Usage components, `publish.ps1`, `packaging/`, and the Domain classes other than `TokenImpactEstimator`.
- **Added (new files):** dashboard page, admin page, admin service, project docs, Neon configuration.
- **Changed in place (small):** registration, navigation, routing, startup, usage service, explanation texts.
- **Rewritten (one file):** `TokenImpactEstimator`. Same public surface, new calculation.
- **Deleted:** nothing from Module 10. Only 4 lines of explanation text and 1 line in `Routes.razor` were replaced.

## 2. New (did not exist in Module 10)

| Item | Purpose |
| :--- | :--- |
| `Components/Pages/Dashboard.razor` (`/dashboard`) | Daily, weekly and monthly totals and a per-model table |
| `Services/UsageTrackingService.cs` → `GetTrendAsync` (added to an existing class) | Aggregation behind the dashboard, in the user's time zone |
| `Components/Pages/Admin.razor` (`/admin`) | Admin panel: platform totals, add, edit, suspend accounts, grant or revoke the Admin role |
| `Services/AdminService.cs` | Admin operations on ASP.NET Core Identity, with safety guards |
| `docs/progress-and-issues.md`, `docs/decisions-and-system-design.md`, this file | Project documentation |
| `neon.ts`, `package.json`, `package-lock.json`, `.gitignore` entries | Neon project configuration (not used by the .NET app at runtime) |

## 3. Changed in existing files

| File | Change |
| :--- | :--- |
| `Program.cs` | `AddRoles<IdentityRole>()`, register `AdminService`, create the Admin role and promote `Admin:Email` at startup (+15 lines, nothing removed) |
| `Components/Account/Pages/Register.razor` | After registration, promote the account if its email equals `Admin:Email` (+5) |
| `Components/Layout/NavMenu.razor` | Links to Dashboard and, for admins only, Admin (+12) |
| `Components/Routes.razor` | A signed-in user without access now sees "Access denied" instead of being sent to the login page |
| `Components/Pages/AskGemini.razor` | The "How the estimate works" text describes the new formula |
| `README.md`, `docs/USER_GUIDE.md` | Formula description updated |

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

## 7. Effect on the Module 10 worksheet

| Worksheet statement | Status |
| :--- | :--- |
| Black-box tests 1–22 (installation, accounts, usage tracker, Ask Gemini errors) | Not affected by the changes; not re-run |
| Test 23 (successful Gemini reply) | Still not run; the displayed estimate would now be 0.0326 g and 0.2532 mL per 500 tokens |
| Section 2, "Impact formula" row | Outdated: describes the linear scaling of the three figures |
| Endpoint `gemini-2.5-flash` | Unchanged in code; the model is deprecated and routed to `gemini-3.5-flash-lite` from 2026-12-01 |
| Dashboard, admin panel | Not covered by any worksheet test case |

---

## 8. Three snapshots: old `main`, after the Module 10 merge, and now

| Snapshot | Commit | Date |
| :--- | :--- | :--- |
| **Old main** | `d5bf87f` | 2026-09-17 |
| **After the Module 10 merge** | `8f2f1c2` | 2026-11-25 |
| **Now** (branch `533008`) | `eb90670` | 2026-11-25 |

| | Old main | After Module 10 merge | Now |
| :--- | :--- | :--- | :--- |
| **Domain project builds?** | No: 17 compile errors | Yes | Yes |
| **Web content** | Identity template: "Hello, world", Counter, Weather, Auth demo | Usage tracker and Ask Gemini | Plus dashboard and admin panel |
| **App pages** | none | `/`, `/ask-gemini` | `/`, `/ask-gemini`, `/dashboard`, `/admin` |
| **Database** | SQLite (`app.db`), Identity tables only | PostgreSQL, Identity tables and `UsageRecords`, migrations at startup | same |
| **Services** | none | `GeminiService`, `UsageTrackingService`, `BrowserTimeService`, `UserMessages` | plus `AdminService` (5 service files) |
| **Authentication** | Identity, email confirmation required (no email sender, so nobody could sign in) | no email confirmation | plus the `Admin` role |
| **Gemini API** | none | yes | same |
| **Impact formula** | none | three published figures scaled by tokens | energy, then carbon, then water (Google chain) |
| **Dashboard (trend)** | no | no | yes |
| **Admin panel** | no (the Domain `Admin` class exists but is not stored) | no | yes (on Identity) |
| **Windows package** | no | yes (`publish.ps1`, `Setup` and `Start` scripts) | same |
| **User guide** | no | yes | updated |
| **README** | plans only; names ASP.NET Core 9 and SQL Server | install section added | formula updated |
| **Black-box test** | no | 22 of 23 Match (Worksheet 10) | not re-run; new features have no cases yet |
| **Project documentation** | no | no | yes |
| **App code, approximate lines** (`.cs` and `.razor`, without the Account template and migrations) | about 1,012 | about 1,728 | about 2,584 |

The line counts are a rough size indicator, not a measure of quality; part of the newest code is source comments.

### Old main
Domain classes derived from the UML that did not compile, and a Web project that was still the template:
- `GetFactorsByModel(string)` was called, but only the two-parameter version existed.
- `decimal` was multiplied with `double` in `AIComparison`.
- `RegisteredUser.Register` was referenced by `Admin` but did not exist.
- A placeholder project `AIEnvironmental/Class1.cs` was still present.

### After the Module 10 merge (42 files, +1,484 / -351)
The first usable application: the Domain compiles, the template pages and the placeholder project are removed, the database moved to PostgreSQL, and the usage tracker, Ask Gemini, the installer package, the user guide and the black-box test were added.

### Now (17 files, +1,213 / -19)
Almost purely additive: dashboard, admin panel, Identity roles, the full Google formula, and project documentation.

### Unchanged in all three snapshots
The Domain classes `User`, `Admin`, `RegisteredUser`, `AIComparison` and `AIUsageLog.ScanDeviceActivity` exist but are not used by the web application.

### Backend and frontend split of the newest features

| Feature | Backend (logic and data) | Frontend (page) |
| :--- | :--- | :--- |
| Trend dashboard | `UsageTrackingService.GetTrendAsync`: query, buckets, time zone, per-model totals | `Components/Pages/Dashboard.razor` |
| Admin panel | `AdminService`: accounts, roles, guards, platform totals; startup promotion in `Program.cs` | `Components/Pages/Admin.razor`, menu link in `NavMenu.razor` |
| Impact formula | `TokenImpactEstimator` (Domain project) | explanation text in `AskGemini.razor` |
