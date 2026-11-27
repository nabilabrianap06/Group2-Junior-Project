# OxyGenAI – To-do List

_Created 2026-11-26. Final result: a Windows application written in C# (Blazor inside a WinForms + WebView2 window)._

---

## Rules

1. **Check your dependencies first.** Make sure every task yours depends on is done **and works well**. If it does not, contact the owner, or create a new issue to fix it and assign it to the corresponding team member. This covers the whole chain, not only the tasks listed directly (see *Order of work*).
2. **Verify, then announce.** Before moving an issue or marking it as done, make sure it really is done and **tell everyone** that it is done.
3. **Done means the Definition of Done.** Every point of it is met. For code: the solution builds with 0 errors, `dotnet test AIEnvironmentalTracker.slnx` passes, and you ran the app and saw the result yourself.
4. **Design first.** Front-end work on a page starts after the UI design of that page is approved. If the design cannot be built as drawn, talk to the UI owner before changing it.
5. **One reviewer.** Another member reviews the pull request before it is merged. Nobody pushes directly to `main`.
6. **Pull before you start**, and work on your own branch (for example your NIM branch or `feature/<task-id>-short-name`).
7. **Keep the status current.** Set *In progress* when you start; have at most two tasks in progress at once.
8. **Report blockers early**, on the same day you find them: say which task is blocked, by what, and who you need.
9. **Announce shared changes first.** Tell the owner before changing their area, and tell everyone before changing a route, a service method, a setting name or the database schema.
10. **Never commit secrets** (API keys, connection strings, `.env.local`, `oxygenai.settings.json`). A key that was shared anywhere is revoked and replaced.
11. **Everyone documents and tests their own work.** The person who changes something updates `docs/` and the README in the same pull request; backend logic also gets tests. Nobody writes documentation on behalf of another member.
12. **Write useful issues:** steps to reproduce, expected result, actual result, screenshot if it is visual, and the assignee.
13. **Reopen, do not hide.** If a finished task breaks, set it back to *To do* or open a linked issue, and tell the team.
14. **Freeze before the demo.** In the last day before submission, only bug fixes are merged.

**Status values:** `To do` · `In progress` · `Blocked` · `Review` · `Done`

---

## Team and responsibility

Two members work on the backend and two on the front end.

| Member | Side | Role | Builds | Sections |
| :--- | :--- | :--- | :--- | :--- |
| Member 1 | **Backend** | UI design + backend | Mockups of every page and of the Windows window, visual review; detection of the AI app in use and the optional backend tasks | 1–8 (UI rows), 9C, 9E |
| Member 2 | **Front end** | Front end | Landing page, look and navigation, sign-in and account pages, admin panel | 1, 2, 3, 7 (front-end rows) |
| Member 3 | **Front end** | Front end | Usage tracker, dashboard, Ask Gemini, Compare models, Windows window | 4, 5, 6, 8 (front-end rows) |
| Member 4 | **Backend** | Backend only | Services, domain model and class diagram, Neon database, Gemini, integration | 9A, 9B, 9D |

Testing and release (sections 10 and 11) are shared by all members. Keep this table current.

**General Definition of Done**
- **UI task:** the mockup is in the shared design file (or `docs/design/`), shows the normal, empty, loading and error states that apply, works at 900×600 and full screen, and is approved by the team.
- **Front-end task:** the page matches the approved mockup, works at 900×600 and full screen, and rule 3 is met.

The table rows list what is needed **in addition** to these.

---

## Order of work

Work from step 1 downwards. A task may start only when **everything in its chain** is `Done`: the tasks in its *Depends on* column, the tasks those depend on, and so on. Tasks in the same step do not depend on each other, so they can run in parallel. A task number alone does not set the order; the dependencies do.

| Step | Member 1 (UI + backend) | Member 2 (front end) | Member 3 (front end) | Member 4 (backend) | All |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | 1.1 👀, 9.15, 11.1 | 2.2 | – | 9.8, 9.19 🔒, 9.23 👀 | – |
| 2 | 1.2 👀, 1.3, 2.1, 3.1 👀, 4.1 👀, 5.1, 6.1 ⏳, 7.1, 8.1, 9.16 | 1.4 👀 | – | 9.9, 9.14, 9.20 🔒 | – |
| 3 | 4.4 | 1.5 👀, 1.6, 2.3, 2.4, 3.2 ⏳, 3.3 👀, 7.2, 7.3 | 4.2 👀, 5.2, 6.2 👀, 6.3, 8.2, 8.3 | 9.10, 9.12, 9.13 | – |
| 4 | 9.17, 10.7 | 2.5, 3.4, 10.1 | 4.3 👀, 5.3, 5.4, 10.4 | 9.11, 10.5 🔒 | – |
| 5 | – | – | 4.5, 10.2 | 9.18 | – |
| 6 | 10.6 | 10.3 | – | 11.2 | – |
| 7 | – | – | – | 11.4 | 11.3 |
| 8 | – | – | – | – | 11.5 |

👀 = in review (check it first) · ⏳ = in progress · 🔒 = also waiting for something outside the team (a Gemini API key). Optional tasks (9.21, 9.22) are not scheduled; they need a decision first. Because the merge into `main` (11.2) depends on all of sections 1–9, it also waits for 9.19 and 9.20.

### Backend chain (Members 4 and 1)

Tasks that are not `Done` yet.

| Task | Owner | Step | Depends on directly | Whole chain that must be done first |
| :--- | :--- | :--- | :--- | :--- |
| 9.8 | Member 4 | 1 | – | nothing (can start now) |
| 9.15 | Member 1 | 1 | – | nothing (can start now) |
| 9.19 | Member 4 | 1 | – | nothing (can start now) |
| 9.23 | Member 4 | 1 | 9.7 | nothing (can start now) |
| 9.9 | Member 4 | 2 | 9.8 | 9.8 |
| 9.14 | Member 4 | 2 | 9.8 | 9.8 |
| 9.16 | Member 1 | 2 | 9.15, 9.8 | 9.8, 9.15 |
| 9.20 | Member 4 | 2 | 9.19 | 9.19 |
| 9.10 | Member 4 | 3 | 9.9 | 9.8, 9.9 |
| 9.12 | Member 4 | 3 | 9.9 | 9.8, 9.9 |
| 9.13 | Member 4 | 3 | 9.9 | 9.8, 9.9 |
| 9.11 | Member 4 | 4 | 9.10 | 9.8, 9.9, 9.10 |
| 9.17 | Member 1 | 4 | 9.15, 9.10 | 9.8, 9.9, 9.10, 9.15 |
| 9.18 | Member 4 | 5 | 9.17, 9.13 | 9.8, 9.9, 9.10, 9.13, 9.15, 9.17 |

---

## Already done (for reference)

| Area | What works |
| :--- | :--- |
| Accounts and admin | Register, login, account pages; Admin role; admin panel; built-in admin account (see 9.1) |
| Tracking | Manual usage records, totals, recommendations, daily/weekly/monthly trend data |
| Gemini | Ask Gemini with a model choice, Compare models, per-user limits |
| Impact | Google's energy → carbon → water method with sources in the code |
| Platform | Neon database, Windows window (WinForms + WebView2), package script, 104 automated tests |
| UI redesign | Theme, top navigation bar, usage tracker, Ask Gemini, log in and register (merged 2026-11-27) |

Tasks touched by the redesign are marked `Review` (or `In progress`): they still need to be checked against their Definition of Done before they become `Done`.

---

## 1. Look, layout and navigation

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 1.1 | UI | Member 1 | **Design system** – colours, fonts, buttons, cards, tables, forms | One page listing every style with its values; text contrast is readable | – | Review |
| 1.2 | UI | Member 1 | **Navigation menu design** – icons, order, active page | Own icon for every item (three share one today); guest, user and admin variants | 1.1 | Review |
| 1.3 | UI | Member 1 | **Error, not-found and access-denied design** | Clear message and a way back on each | 1.1 | To do |
| 1.4 | Front end | Member 2 | **Apply the design system** | Values from 1.1 are in `app.css` and used by all shared elements; Member 3 told how to use them | 1.1 | Review |
| 1.5 | Front end | Member 2 | **Navigation menu** | Current page is highlighted; each role sees only its items | 1.2, 1.4 | Review |
| 1.6 | Front end | Member 2 | **Error pages** | The three pages are reachable and styled | 1.3, 1.4 | To do |

## 2. Landing page

Today a guest who opens the app is sent straight to the login page.

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 2.1 | UI | Member 1 | **Landing page design** – hero, features, how the estimate works, buttons | Name, purpose and main button visible without scrolling at 900×600; final texts written in the mockup; only existing features shown | 1.1 | To do |
| 2.2 | Front end | Member 2 | **Public route** – guests see the landing page at `/`; signed-in users reach the usage tracker | `/` opens without login for guests; signed-in users get the tracker (same page or `/usage`); every menu link and redirect still works; Members 3 and 4 told about the route change | – | To do |
| 2.3 | Front end | Member 2 | **Hero and feature section** | Built as in 2.1; one card per feature | 2.1, 2.2 | To do |
| 2.4 | Front end | Member 2 | **How the estimate works** | States that numbers are estimates, names the Google source, mentions the 500-token assumption and the same factor for every Gemini model | 2.1, 2.2 | To do |
| 2.5 | Front end | Member 2 | **Register and Log in buttons** | Both work from the landing page; signed-in users do not see them | 2.3 | To do |

## 3. Sign-in and account pages

These pages come from the ASP.NET Core template.

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 3.1 | UI | Member 1 | **Login, register and account design** | Only working options shown; password rules visible on register | 1.1 | Review |
| 3.2 | Front end | Member 2 | **Login page** | "Use another service", "Forgot your password?" and "Resend email confirmation" removed (no external login, no email sending); passkey button kept or removed by team decision; login still works (the redesign still shows "Forgot password?") | 3.1, 1.4 | In progress |
| 3.3 | Front end | Member 2 | **Register page** | External-service block removed; password rules shown before submitting; registration still works | 3.1, 1.4 | Review |
| 3.4 | Front end | Member 2 | **Account pages** – password, two-factor, personal data | Every visible menu item works; items needing email or an external service are hidden | 3.1, 3.2 | To do |

## 4. Usage tracker

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 4.1 | UI | Member 1 | **Tracker design** – input form, totals, recommendations, recent records | Every field has a unit and an example; empty state drawn | 1.1 | Review |
| 4.2 | Front end | Member 3 | **Input form** | Invalid input shows a readable message; saving cannot be clicked twice | 4.1, 1.4 | Review |
| 4.3 | Front end | Member 3 | **Totals, recommendations and recent records** | Numbers have units; a saved record appears without reload | 4.2 | Review |
| 4.4 | UI | Member 1 | **"Detect AI app" design** – button, result, nothing found, privacy note | All states drawn; the note says that only app names are read | 4.1 | To do |
| 4.5 | Front end | Member 3 | **"Detect AI app" button** | Calls the service from 9.17; shows the detected app and session duration; fills the model name, which the user can still change; "nothing detected" shown | 4.4, 9.17 | To do |

## 5. Dashboard

The data is ready (`UsageTrackingService.GetTrendAsync`); today it is shown as tables with simple bars. The redesign only restyled this page.

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 5.1 | UI | Member 1 | **Dashboard design** – charts, period switch, per-model view | Chart type chosen per metric (CO₂, water, energy, queries) with axes and units | 1.1 | To do |
| 5.2 | Front end | Member 3 | **Charts** | Each metric visible over time; chart values equal the table values | 5.1, 1.4 | To do |
| 5.3 | Front end | Member 3 | **Period switch and per-model view** | Daily, Weekly and Monthly update chart and tables; selected period visible | 5.2 | To do |
| 5.4 | Front end | Member 3 | **Empty, loading and error states** | Each state has its own message; no blank screen | 5.2 | To do |

## 6. Ask Gemini and Compare models

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 6.1 | UI | Member 1 | **Ask Gemini and Compare design** | Long replies, waiting, limit and error messages, failed model and best result are drawn | 1.1 | In progress |
| 6.2 | Front end | Member 3 | **Ask Gemini page** | Loading state while waiting; long replies readable; character limit shown | 6.1, 1.4 | Review |
| 6.3 | Front end | Member 3 | **Compare models page** | Table readable at 900×600; lowest impact highlighted; failed models marked; ranking explanation visible | 6.1, 1.4 | To do |

## 7. Admin panel

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 7.1 | UI | Member 1 | **Admin panel design** – totals, account table, add form, confirmations | Actions easy to find; confirmation dialog and refusal messages drawn | 1.1 | To do |
| 7.2 | Front end | Member 2 | **Account table and actions** | Suspend and role changes ask for confirmation; refusals (own account, last admin) show their message | 7.1, 1.4 | To do |
| 7.3 | Front end | Member 2 | **Platform totals and add-account form** | Totals have units; password rules shown; form clears after success | 7.1, 1.4 | To do |

## 8. Windows window

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 8.1 | UI | Member 1 | **Icon, start screen and error message design** | Icon delivered as `.ico`; error text understandable for a non-developer | 1.1 | To do |
| 8.2 | Front end | Member 3 | **Application icon and title** | Own icon on the `.exe`, window and taskbar; correct title on every page | 8.1 | To do |
| 8.3 | Front end | Member 3 | **Start screen and error message** | Branded screen while the server starts; message as designed | 8.1 | To do |

---

## 9. Backend (Member 4 and Member 1)

Front-end work that depends on backend tasks is listed in sections 1–8: Members 2 and 3 build it, Member 1 designs it.

| Part | Owner |
| :--- | :--- |
| 9A already built, 9B class diagram, 9D Gemini | Member 4 |
| 9C detection of the AI app, 9E optional | Member 1 |
| 9.18 service notes (covers 9B and 9C) | Member 4, with input from Member 1 |

### 9A. Already built (Member 4)

| ID | Role | Owner | Task | Definition of Done (met) | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 9.1 | Backend | Member 4 | **Built-in admin account** `admin@email.com` | Created at startup; login verified; tests and documents updated; pushed | – | Done |
| 9.2 | Backend | Member 4 | **Accounts, roles and admin operations** (`AdminService`) | List, add, edit email, suspend, reactivate, grant and revoke admin; own account and last admin protected; tested | – | Done |
| 9.3 | Backend | Member 4 | **Usage records, totals and trends** (`UsageTrackingService`) | Save, recent list, totals, daily/weekly/monthly buckets in the user's time zone; tested | – | Done |
| 9.4 | Backend | Member 4 | **Impact estimate** (`TokenImpactEstimator`) | Energy → carbon → water; sources and limits in the file; matches the paper's figures; tested | – | Done |
| 9.5 | Backend | Member 4 | **Gemini client, model list and limits** (`GeminiService`, `AskLimiter`) | Allowed models only; prompt cap; per-user daily limit and pause; safe error messages; tested with a fake server (the live path is 9.19) | – | Done |
| 9.6 | Backend | Member 4 | **Model comparison** (`ModelComparisonService`) | Same prompt to two or more models; ranked; saved as usage; a failing model does not stop the others; tested | 9.5 | Done |
| 9.7 | Backend | Member 4 | **Database, Windows shell logic and package** | Migrations at startup; hidden server, port, shutdown; `publish.ps1` builds both programs; start-up check passed | – | Done |
| 9.23 | Backend | Member 4 | **Neon-only setup of the package** | Setup accepts the Neon console string, refuses other hosts, requires SSL; a packaged app set up with it starts; documents say Neon; committed and pushed | 9.7 | Review |

### 9B. Use the class diagram in the application (Member 4)

Today `User`, `Admin`, `RegisteredUser` and `AIUsageLog.ScanDeviceActivity` exist but the web app does not use them. Identity remains the only place for passwords and sign-in. **The approach needs approval (9.8) before the rest starts.**

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 9.8 | Backend | Member 4 | **Decide how the classes meet Identity** – user id (`int` in the diagram, text in Identity), password handling, password length (8 in the classes, 6 in Identity) | Decision recorded in `docs/decisions-and-system-design.md` and accepted by the team | – | To do (needs approval) |
| 9.9 | Backend | Member 4 | **Load domain users from the database** – create `RegisteredUser` / `Admin` objects from Identity accounts | An existing account can be loaded without a plain-text password; role and suspension are correct; tests for user, admin and suspended user | 9.8 | To do |
| 9.10 | Backend | Member 4 | **`RegisteredUser.recordAIUsage()`** – record usage through the class | Tracker and Ask Gemini save through it; it takes the query count and factor; it no longer scans processes by itself; saved data is the same as before; tests pass | 9.9 | To do |
| 9.11 | Backend | Member 4 | **`viewCarbonFootprint()` and `viewRecommendations()`** | Totals and advice shown by the app come from these methods, filled from the user's records; empty account handled; tests pass | 9.10 | To do |
| 9.12 | Backend | Member 4 | **`compareAIModels()`** – receive the chosen models | The model list is passed in (today it stays empty, so the method returns nothing); the compare page's ranking goes through it; tests pass | 9.9 | To do |
| 9.13 | Backend | Member 4 | **`Admin.manageUserAccounts()`, `addUser()`, `editUser()`, `suspendUser()`** | Admin panel actions go through these methods and are saved with Identity; reactivate still works; own-account and last-admin guards hold; existing admin tests pass | 9.9 | To do |
| 9.14 | Backend | Member 4 | **`User.login()` and `registerAccount()`** | Documented how Identity fulfils them; no second password check or plain-text password is introduced | 9.8 | To do |

### 9C. Detect the AI app in use (Member 1)

Works because in the Windows app the server runs on the user's own PC.

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 9.15 | Backend | Member 1 | **`scanDeviceActivity()` check** – verify detection on a real PC | Tried with a desktop AI app, a browser tab and nothing open; known limits written down (only a browser's active window title is visible) | – | To do |
| 9.16 | Backend | Member 1 | **Usage log and session duration** – `createUsageLog()`; decide whether to store app and duration | Decision recorded; if stored: new columns with a migration, tested on an existing database | 9.15, 9.8 | To do |
| 9.17 | Backend | Member 1 | **Detection service** for the tracker | Returns app name and duration, or "nothing detected"; reads only app names and window titles, stores no titles; runs only on request; tests pass | 9.15, 9.10 | To do |
| 9.18 | Backend | Member 4 | **Service notes for the front end** – what each page calls and what comes back | Short list in `docs/` of the methods, inputs, results and error messages used by Members 2 and 3; updated after 9B and 9C | 9.17, 9.13 | To do |

### 9D. Gemini with a real key (Member 4)

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 9.19 | Backend | Member 4 | **Verify the live path** | New key in user-secrets (shared keys revoked); a real reply with token counts is saved; both allowed models checked; numbers match the formula | – | Blocked (needs a key) |
| 9.20 | Backend | Member 4 | **Re-test models** that answered HTTP 503 (`gemini-3.1-flash-lite`, `gemini-3.8-flash`, `gemini-4-hydrogen`) | Each tested again; stable ones added to `Gemini:AllowedModels`; result noted in `docs/` | 9.19 | Blocked (needs a key) |

### 9E. Optional (Member 1)

| ID | Role | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 9.21 | Backend | Member 1 | Shorter session check for suspended users (now about 30 minutes) | A signed-in suspended user loses access within about a minute; tested | – | To do (needs decision) |
| 9.22 | Backend | Member 1 | Log file for the installed app | Server messages written to `%LOCALAPPDATA%\OxyGenAI\logs`; no secrets in the log | – | To do (needs decision) |

---

## 10. Checking by hand (shared)

The checker is never the person who built the page. Features added after Module 10 get no new black-box test round. Whoever finds a problem files an issue for the builder.

| ID | Checker | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 10.1 | Member 2 | **Windows window** | Opens, title follows the page, external links open in the browser, closing it stops the server | 8.3 | To do |
| 10.2 | Member 3 | **Landing, sign-in and account pages** | Work for guest, user and admin | 2.5, 3.4 | To do |
| 10.3 | Member 2 | **Tracker and dashboard** | Save a record, check totals and each period; wrong input refused; detection tried with an AI app open and closed | 4.3, 4.5, 5.4 | To do |
| 10.4 | Member 3 | **Admin panel** | Add, edit, suspend (then try to log in), reactivate, grant and revoke admin, both refusals | 7.3, 9.13 | To do |
| 10.5 | Member 4 | **Module 10 black-box test 23**, Ask Gemini and Compare | Successful reply with token counts and saved record; screenshot taken | 9.19, 6.3 | Blocked (needs a key) |
| 10.6 | Member 1 | **Visual review** of all pages against the mockups | Differences filed as issues or accepted by the team | Sections 1–8 | To do |
| 10.7 | Member 1 | **Package on a clean PC** (no .NET SDK) | Unzip, setup, start the window, register and log in; steps match the user guide | 8.3 | To do |

## 11. Release and demo (shared)

| ID | Owner | Task | Definition of Done | Depends on | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 11.1 | Member 1 | **Confirm the Windows-app approach** with the lecturer or assistant | Answer shared with the team and noted in `docs/progress-and-issues.md` | – | To do |
| 11.2 | Member 4 | **Merge into `main`** by pull request | Reviewed; builds and tests pass; `main` contains the final work | Sections 1–9 | To do |
| 11.3 | All | **Worksheet and documents** – each member writes the part about their own work | Impact formula, model and new features match the application; every member has checked their part | 11.2 | To do |
| 11.4 | Member 4 | **Final package** | `publish.ps1` run from `main`; zip passes 10.7 and is shared | 11.2 | To do |
| 11.5 | All | **Demo** – each member presents their own part | Steps written down and rehearsed once from start to end without errors | 11.4 | To do |

---

## Not in scope

| Item | Reason |
| :--- | :--- |
| Web deployment | The result is a Windows application that runs on the user's PC |
| Local database | The database is Neon, for development and for the packaged app |
| Email sending, CSV export, audit log, report table | Never part of the README or the class diagram |
| Limit on failed login attempts | Decided against for this demo |
| Different energy figures per Gemini model | No published source |
| Other AI providers | Future work |
| New black-box test round for features added after Module 10 | Decided against |
