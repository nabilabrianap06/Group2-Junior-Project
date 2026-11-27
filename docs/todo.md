# OxyGenAI – To-do

_Updated: 27 November 2026._

## Team

| Member | Name | Responsible for |
| :--- | :--- | :--- |
| 1 | **Satrio** | UI design + light backend |
| 2 | **Deva** | Front end |
| 3 | **Brian** | Front end |
| 4 | **Rere** | Backend (main) and the Neon database |

Rere holds the **Neon** database. Ask Rere for access or the connection string, and tell Rere before changing the database schema.

## Rules

1. Before you start, make sure the tasks you depend on are **done and working well**. If not, contact the owner, or create a new issue and assign it to them.
2. Before moving an issue or marking it as done, **make sure it really is done, then tell everyone**.
3. Done means: the "Done when" column is met, `dotnet build` and `dotnet test AIEnvironmentalTracker.slnx` pass, and you tried it in the app.
4. Front-end work starts after the design of that page is approved.
5. Work on your own branch. Merge into `main` through a pull request reviewed by one other person.
6. Whoever changes something also updates its documentation.
7. Never commit passwords, API keys or connection strings.
8. Blocked? Tell the team the same day.

## Tasks

Work from top to bottom. **Waits for** lists the task numbers that must be done first (including whatever those tasks wait for). **Nothing** means you can start now.

**Status:** `To do` · `In progress` · `Review` (built, needs checking) · `Needs key` · `Done`

| No | Task | Owner | Waits for | Done when | Status |
| :-: | :--- | :--- | :--- | :--- | :--- |
| 1 | Base design: colours, fonts, navigation bar, log in, register, usage tracker | Satrio | Nothing | Approved by the team | Review |
| 2 | Ask the lecturer whether the WinForms + WebView2 app meets the requirement | Satrio | Nothing | Answer shared with the team | To do |
| 3 | Decide how the class-diagram classes work with Identity (user ID type, password) | Rere | Nothing | Decision written in `docs/` and accepted | To do |
| 4 | Test Gemini with a real API key and re-test the models that returned error 503 | Rere | Nothing | A real reply is saved; stable models are added | Needs key |
| 5 | Try the AI app detection (`scanDeviceActivity`) on a real PC | Satrio | Nothing | Tried with an AI app open and closed; limits written down | To do |
| 6 | Remaining designs: landing, error pages, dashboard, compare, admin, Windows window, detect button | Satrio | 1 | All states drawn (normal, empty, loading, error) | In progress |
| 7 | Check the redesign result: theme and navigation bar | Deva | 1 | Matches the design; menu follows the role; active page highlighted | Review |
| 8 | Clean up log in and register | Deva | 1 | "Forgot password?" link removed; log in and register still work | In progress |
| 9 | Check the redesign result: usage tracker and Ask Gemini | Brian | 1 | Matches the design; wrong input shows a message; data is saved | Review |
| 10 | Load database users as `RegisteredUser` / `Admin` objects | Rere | 3 | No plain-text password; role and suspension correct; tests added | To do |
| 11 | Landing page (guests see it at `/`, signed-in users reach the tracker) | Deva | 6 | Shows features, how the estimate works, Register and Log in buttons | To do |
| 12 | Account pages and error pages | Deva | 6, 8 | Only working menu items shown; error pages have a way back | To do |
| 13 | Admin panel screen | Deva | 6 | Suspend and role changes ask for confirmation; refusal messages shown | To do |
| 14 | Dashboard with charts | Brian | 6 | Daily, weekly and monthly charts; values match the tables | To do |
| 15 | Compare models screen | Brian | 6 | Readable in a small window; best result highlighted; failed models visible | To do |
| 16 | Windows window: icon, loading screen, error message | Brian | 6 | Matches the design; icon on the `.exe` and taskbar | To do |
| 17 | Use `RegisteredUser`: record usage, footprint report, recommendations, model comparison | Rere | 10 | The app uses these methods; empty model list bug fixed; tests pass | To do |
| 18 | Use `Admin`: manage, add, edit and suspend users; document log in and register | Rere | 10 | Admin panel goes through the class; admin rules still hold; tests pass | To do |
| 19 | AI app detection service (app name and session duration) | Rere | 5, 17 | Reads app names only; "not detected" handled; stored in Neon if agreed; tests added | To do |
| 20 | Manual check: landing, log in, account, admin | Brian | 11, 12, 13, 18 | Tried as guest, user and admin; bugs filed as issues | To do |
| 21 | Manual check: black-box test 23, Ask Gemini, Compare | Rere | 4, 9, 15 | Real reply with token counts; screenshot taken | Needs key |
| 22 | "Detect AI app" button on the usage tracker | Brian | 6, 19 | Model name filled in automatically and still editable | To do |
| 23 | Short service notes for the front end (methods, input, result, errors) | Rere | 17, 18, 19 | Written in `docs/` | To do |
| 24 | Manual check: Windows window, tracker, dashboard | Deva | 14, 16, 22 | Everything tried, including closing the window; bugs filed as issues | To do |
| 25 | Visual review of all pages against the design | Satrio | 7, 8, 9, 11–16, 22 | Differences written down or accepted | To do |
| 26 | Final merge into `main`, build the `.zip` package and test it with Neon on a PC without .NET | Rere | 1–25 | Build and tests pass; `publish.ps1` run from `main`; setup with Neon works; zip shared | To do |
| 27 | Update the worksheet and documents, each member their own part | All | 26 | Content matches the app | To do |
| 28 | Demo rehearsal, each member explains their own part | All | 27 | One full rehearsal without errors | To do |

## Optional (only if the team agrees)

| Task | Owner | Done when |
| :--- | :--- | :--- |
| Suspended users are signed out quickly (now up to 30 minutes) | Rere | Access is lost within about 1 minute |
| Log file for the installed app | Rere | Logs saved without secrets |

## Already done

- Register, log in, admin role, admin panel, built-in admin account `admin@email.com`
- Usage tracker, recommendations, daily/weekly/monthly trend data
- Energy, carbon and water formula (sources in the code)
- Ask Gemini with model choice, Compare models, usage limits
- Neon database, package setup for Neon only
- Windows app (WinForms + WebView2), package script, 104 automated tests

## Not doing

Web deployment · local database · email, CSV export, audit log · limit on failed log-ins · different energy figures per Gemini model · other AI providers · new black-box test for features added after Module 10
