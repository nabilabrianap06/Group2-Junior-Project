# OxyGenAI – To-do List

_Created 2026-11-26. Final result: a Windows application written in C# (Blazor inside a WinForms + WebView2 window)._

---

## Rules

1. **Check your dependencies first.** Make sure every task yours depends on is done **and works well**. If it does not, contact the owner, or create a new issue to fix it and assign it to the corresponding team member.
2. **Verify, then announce.** Before moving an issue or marking it as done, make sure it really is done and **tell everyone** that it is done.
3. **Done means the Definition of Done.** Every point of it is met, the solution builds with 0 errors, `dotnet test AIEnvironmentalTracker.slnx` passes, and you ran the app and saw the result yourself.
4. **One reviewer.** Another member reviews the pull request before it is merged. Nobody pushes directly to `main`.
5. **Pull before you start**, and work on your own branch (for example your NIM branch or `feature/<task-id>-short-name`).
6. **Keep the status current.** Set *In progress* when you start; have at most two tasks in progress at once.
7. **Report blockers early**, on the same day you find them: say which task is blocked, by what, and who you need.
8. **Announce shared changes first.** Tell the owner before changing their area, and tell everyone before changing a route, a service method, a setting name or the database schema.
9. **Never commit secrets** (API keys, connection strings, `.env.local`, `oxygenai.settings.json`). A key that was shared anywhere is revoked and replaced.
10. **Tests and documents move with the code.** New backend logic gets tests; changed behaviour is updated in `docs/` and the README in the same pull request.
11. **Write useful issues:** steps to reproduce, expected result, actual result, screenshot if it is visual, and the assignee.
12. **Reopen, do not hide.** If a finished task breaks, set it back to *To do* or open a linked issue, and tell the team.
13. **Freeze before the demo.** In the last day before submission, only bug fixes are merged.

**Status values:** `To do` · `In progress` · `Blocked` · `Review` · `Done`

---

## Team and responsibility

| Member | Area | Sections |
| :--- | :--- | :--- |
| Member 1 | Frontend: public pages, look and navigation, sign-in and account pages | 1, 2, 3 |
| Member 2 | Frontend: application pages and the Windows window | 4, 5, 6, 7, 8 |
| Member 3 | Testing, release, documentation and demo | 10, 11 |
| Member 4 | **Backend** | 9 |

The assignment of Members 1–3 is a proposal; adjust it together and keep this table current.

---

## Already done (for reference)

| Area | What works |
| :--- | :--- |
| Accounts and admin | Register, login, account pages; Admin role; admin panel; built-in admin account (see 9.1) |
| Tracking | Manual usage records, totals, recommendations, daily/weekly/monthly trend data |
| Gemini | Ask Gemini with a model choice, Compare models, per-user limits |
| Impact | Google's energy → carbon → water method with sources in the code |
| Platform | PostgreSQL, Windows window (WinForms + WebView2), package script, 104 automated tests |

The pages exist and render; most of them still need design work and a check by hand.

---

## 1. UI – Landing page (Member 1)

Today a guest who opens the app is sent straight to the login page.

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 1.1 | **Public route** – guests see a landing page at `/`; signed-in users reach the usage tracker | `/` opens without login for guests; a signed-in user gets the tracker (same page or `/usage`); every menu link and redirect still works; Member 4 informed of route changes | – | To do |
| 1.2 | **Hero section** – app name, one-sentence purpose, main button | Name, purpose and button visible without scrolling in a 900×600 window; text agreed by the team | 1.1 | To do |
| 1.3 | **Feature section** – track, ask Gemini, compare, dashboard | One card per existing feature; no feature is promised that does not exist | 1.1 | To do |
| 1.4 | **How the estimate works** – method and limits in plain language | States that numbers are estimates, names the Google source, mentions the 500-token assumption and the same factor for every Gemini model | 1.1 | To do |
| 1.5 | **Calls to action** – Register and Log in | Both buttons work from the landing page; signed-in users do not see them | 1.1 | To do |

## 2. UI – Look, layout and navigation (Member 1)

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 2.1 | **Theme** – colours, fonts, buttons, cards | One written colour and font choice applied in `app.css`; all pages use it; text is readable (good contrast) | – | To do |
| 2.2 | **Navigation menu** – icons, order, active page | Each item has its own icon (three share one today); the current page is highlighted; guest, user and admin see the right items | 2.1 | To do |
| 2.3 | **Window sizes** – small and large | No overlapping or cut-off content at 900×600 and full screen; wide tables scroll inside their card | 2.1 | To do |
| 2.4 | **Error, not-found and access-denied pages** | Same style as the app, a clear message and a way back | 2.1 | To do |

## 3. UI – Sign-in and account pages (Member 1)

These pages come from the ASP.NET Core template.

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 3.1 | **Login page** – remove what does not work | "Use another service" block, "Forgot your password?" and "Resend email confirmation" are removed (no external login and no email sending); passkey button kept or removed by team decision; login still works | 2.1 | To do |
| 3.2 | **Register page** | External-service block removed; password rules shown before the user submits; registration still works | 2.1 | To do |
| 3.3 | **Account pages** (change password, two-factor, personal data) | Every visible menu item works; items that need email or an external service are hidden | 3.1 | To do |

## 4. UI – Usage tracker (Member 2)

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 4.1 | **Input form** – clear labels, units, help text | Each field shows its unit and an example; invalid input shows a readable message; saving cannot be clicked twice | 2.1 | To do |
| 4.2 | **Totals, recommendations and recent records** | Numbers have units and are easy to read; a new account sees a friendly empty state; a saved record appears without reload | 4.1 | To do |

## 5. UI – Dashboard (Member 2)

The data is ready (`UsageTrackingService.GetTrendAsync`); today it is shown as tables with simple bars.

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 5.1 | **Charts** – trend of CO₂, water, energy and queries | The user can see each metric over time with labelled axes and units; chart values equal the table values | 2.1 | To do |
| 5.2 | **Period switch and per-model view** | Daily, Weekly and Monthly update chart and tables; the selected period is visible | 5.1 | To do |
| 5.3 | **Empty, loading and error states** | Each state has its own clear message; no blank screen | 5.1 | To do |

## 6. UI – Ask Gemini and Compare models (Member 2)

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 6.1 | **Ask Gemini layout** – model list, prompt, reply, impact | Loading state while waiting; long replies are readable; limit and error messages are visible; character limit shown | 2.1 | To do |
| 6.2 | **Compare models layout** | Result table readable at 900×600; lowest impact is highlighted; failed models are clearly marked; the explanation of the ranking stays visible | 2.1 | To do |

## 7. UI – Admin panel (Member 2)

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 7.1 | **Account table and actions** | Actions are easy to find; suspend and role changes ask for confirmation; refusals (own account, last admin) show their message | 2.1 | To do |
| 7.2 | **Platform totals and add-account form** | Totals have units; the form shows password rules and clears after success | 7.1 | To do |

## 8. UI – Windows window (Member 2)

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 8.1 | **Application icon and title** | Own icon on the `.exe`, window and taskbar; the title is correct on every page | – | To do |
| 8.2 | **Start screen and error message** | A branded start screen while the server starts; the failure message is understandable for a non-developer | 8.1 | To do |

---

## 9. Backend (Member 4)

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 9.1 | **Built-in admin account** `admin@email.com` | Created at startup; login verified; tests and documents updated; **committed and pushed** | – | Review (implemented and tested, not yet committed) |
| 9.2 | **Class diagram: `RegisteredUser`** – use it for usage recording, footprint report, comparison and recommendations | The web app performs these four actions through `RegisteredUser`; Identity stays the only place for passwords; existing pages behave the same; tests pass | 9.1 | To do (plan needs approval) |
| 9.3 | **Class diagram: `Admin`** – use `ManageUserAccounts`, `AddUser`, `EditUser`, `SuspendUser` | Admin panel actions go through `Admin`; changes are saved with Identity; guards (own account, last admin) still hold; tests pass | 9.2 | To do |
| 9.4 | **Class diagram: `AIUsageLog.ScanDeviceActivity`** – detect a running AI app | A service returns the detected app and session duration through `RegisteredUser.RecordAIUsage`; only app names are read; "nothing detected" is handled; tests pass | 9.2 | To do |
| 9.5 | **"Detect AI app" button** on the usage tracker (with Member 2) | The button fills the model name from the detection; works in the Windows app on the same PC | 9.4, 4.1 | To do |
| 9.6 | **Real Gemini key** – verify the live path | New key stored in user-secrets (old shared keys revoked); a real reply with token counts is saved; both models checked | – | Blocked (needs a key) |
| 9.7 | **Re-test other Gemini models** that answered HTTP 503 | Each model tested again; stable ones added to `Gemini:AllowedModels`; result noted in `docs/` | 9.6 | Blocked (needs a key) |
| 9.8 | _Optional:_ shorter session check for suspended users (now about 30 minutes) | A suspended user who is signed in loses access within about a minute; tested | – | To do (needs decision) |
| 9.9 | _Optional:_ log file for the installed app | Server messages are written to `%LOCALAPPDATA%\OxyGenAI\logs`; no secrets in the log | – | To do (needs decision) |

---

## 10. Testing (Member 3)

Features added after Module 10 are checked by hand; there is no new black-box test round for them.

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 10.1 | **Hand check: Windows window** | Opens, title follows the page, external links open in the browser, closing it stops the server; findings filed as issues | 8.2 | To do |
| 10.2 | **Hand check: sign-in and landing** | Landing, register, login, logout and account pages work for guest, user and admin | 1.5, 3.3 | To do |
| 10.3 | **Hand check: tracker and dashboard** | Save a record, check totals and each period; wrong input is refused | 4.2, 5.3 | To do |
| 10.4 | **Hand check: admin panel** | Add, edit, suspend (then try to log in), reactivate, grant and revoke admin, both refusals | 7.2, 9.3 | To do |
| 10.5 | **Module 10 black-box test 23** and hand check of Ask Gemini and Compare | Successful reply with token counts and saved record; screenshot taken; result marked | 9.6, 6.2 | Blocked (needs a key) |
| 10.6 | **Package on a clean PC** (no .NET SDK) | Unzip, setup, start the window, register and log in successfully; steps match the user guide | 8.2 | To do |

## 11. Release, documents and demo (Member 3)

| ID | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- |
| 11.1 | **Confirm the Windows-app approach** with the lecturer or assistant | Answer received and written in `docs/progress-and-issues.md` | – | To do |
| 11.2 | **Merge into `main`** by pull request | Reviewed, builds and tests pass, `main` contains the final work | 9.1, sections 1–8 | To do |
| 11.3 | **Update the worksheet** (impact formula, model, new features) | The document matches the application | 11.2 | To do |
| 11.4 | **Final package** | `publish.ps1` run from `main`; zip tested (10.6) and shared with the team | 11.2 | To do |
| 11.5 | **Demo script and screenshots** | Written steps for the demo, rehearsed once from start to end without errors | 11.4 | To do |

---

## Not in scope

| Item | Reason |
| :--- | :--- |
| Web deployment | The result is a Windows application |
| Email sending, CSV export, audit log, report table | Never part of the README or the class diagram |
| Limit on failed login attempts | Decided against for this demo |
| Different energy figures per Gemini model | No published source |
| Other AI providers | Future work |
| New black-box test round for features added after Module 10 | Decided against |
