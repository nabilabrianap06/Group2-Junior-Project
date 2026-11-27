# OxyGenAI – To-do

_Updated: 27 November 2026._

## Final submission

**Due 30 November 2026, 23:59** (multiple submissions allowed). One `.zip` with:

| File | Requirements |
| :--- | :--- |
| Final PPT | Introduction · task distribution · tech stack · ERD and Use Case Diagram · OOP implementation · application testing · demo accounts (required if there is no sign-up) · at most 10 minutes without Q&A |
| Demo video | At most 3 minutes · application demo only · **the database must not be local** |
| `.exe` file | The application |

### Submission notes

- **Database:** Neon (not local).
- **Demo accounts:** the app has sign-up, but the admin cannot be created that way, so list `admin@email.com` / `Admin#2026`.
- **Running the `.exe`:** the package needs the Neon connection string (and a Gemini key for Ask Gemini). The lecturer's PC needs them from us (task 4). Options: hand them over for the setup screen, or add a prepared `oxygenai.settings.json` to the submitted zip only. Either way they leave our hands: use a key and database password made for the submission and reset them after grading. Never commit them.

## Team

| Member | Name | Responsible for |
| :--- | :--- | :--- |
| 1 | **Satrio** | UI design + light backend |
| 2 | **Deva** | Front end |
| 3 | **Brian** | Front end |
| 4 | **Rere** | Backend (main) |

**Rere holds the Neon database and the Gemini API key.** Ask Rere for access, and tell Rere before changing the database schema.

## Rules

1. Before you start, make sure the tasks you depend on are **done and working well**. If not, contact the owner, or create a new issue and assign it to them.
2. Before moving an issue or marking it as done, **make sure it really is done, then tell everyone**.
3. Done means: the "Done when" column is met, and for code `dotnet build` and `dotnet test AIEnvironmentalTracker.slnx` pass and you tried it in the app.
4. Front-end work starts after the design of that page is approved.
5. Work on your own branch. Merge into `main` through a pull request reviewed by one other person.
6. Whoever changes something also updates its documentation.
7. Never commit passwords, API keys or connection strings.
8. Blocked, or going to miss the date? Tell the team the same day.

## Tasks

Work from top to bottom. **Waits for** lists the task numbers that must be done first (including whatever those tasks wait for). **Nothing** means you can start now.

**Status:** `To do` · `In progress` · `Review` (built, needs checking) · `Done`

| No | Task | Owner | Waits for | Due | Done when | Status |
| :-: | :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | Base design: colours, fonts, navigation bar, log in, register, usage tracker | Satrio | Nothing | 27 Nov | Approved by the team | Review |
| 2 | Decide how the class-diagram classes work with Identity (user ID type, password) | Rere | Nothing | 27 Nov | Decision written in `docs/` | To do |
| 3 | Test Gemini with the real API key and re-test the models that returned error 503 | Rere | Nothing | 27 Nov | A real reply is saved; stable models are added | To do |
| 4 | Decide how the lecturer's PC connects the `.exe` to Neon and Gemini (see *Submission notes*) | Rere | Nothing | 27 Nov | Choice agreed by the team | To do |
| 5 | Try the AI app detection (`scanDeviceActivity`) on a real PC | Satrio | Nothing | 27 Nov | Tried with an AI app open and closed; limits written down | To do |
| 6 | Use Case Diagram | Satrio | Nothing | 27 Nov | Covers guest, user and admin; matches the app | To do |
| 7 | Remaining designs: landing, error pages, dashboard, compare, admin, Windows window, detect button | Satrio | 1 | 28 Nov | All states drawn (normal, empty, loading, error) | In progress |
| 8 | Check the redesign result: theme and navigation bar | Deva | 1 | 28 Nov | Matches the design; menu follows the role; active page highlighted | Review |
| 9 | Clean up log in and register | Deva | 1 | 28 Nov | "Forgot password?" link removed; log in and register still work | In progress |
| 10 | Check the redesign result: usage tracker and Ask Gemini | Brian | 1 | 28 Nov | Matches the design; wrong input shows a message; data is saved | Review |
| 11 | Load database users as `RegisteredUser` / `Admin` objects | Rere | 2 | 28 Nov | No plain-text password; role and suspension correct; tests added | To do |
| 12 | Landing page (guests see it at `/`, signed-in users reach the tracker) | Deva | 7 | 28 Nov | Shows features, how the estimate works, Register and Log in buttons | To do |
| 13 | Account pages and error pages | Deva | 7, 9 | 28 Nov | Only working menu items shown; error pages have a way back | To do |
| 14 | Admin panel screen | Deva | 7 | 28 Nov | Suspend and role changes ask for confirmation; refusal messages shown | To do |
| 15 | Dashboard with charts | Brian | 7 | 28 Nov | Daily, weekly and monthly charts; values match the tables | To do |
| 16 | Compare models screen | Brian | 7 | 28 Nov | Readable in a small window; best result highlighted; failed models visible | To do |
| 17 | Windows window: icon, loading screen, error message | Brian | 7 | 28 Nov | Matches the design; icon on the `.exe` and taskbar | To do |
| 18 | Use `RegisteredUser`: record usage, footprint report, recommendations, model comparison | Rere | 11 | 28 Nov | The app uses these methods; empty model list bug fixed; tests pass | To do |
| 19 | Use `Admin`: manage, add, edit and suspend users; document log in and register | Rere | 11 | 28 Nov | Admin panel goes through the class; admin rules still hold; tests pass | To do |
| 20 | AI app detection service (app name and session duration) | Rere | 5, 18 | 29 Nov | Reads app names only; "not detected" handled; tests added | To do |
| 21 | Manual check: landing, log in, account, admin | Brian | 12–14, 19 | 29 Nov | Tried as guest, user and admin; bugs fixed or filed | To do |
| 22 | Manual check: black-box test 23, Ask Gemini, Compare | Rere | 3, 10, 16 | 29 Nov | Real reply with token counts; screenshot taken | To do |
| 23 | "Detect AI app" button on the usage tracker | Brian | 7, 20 | 29 Nov | Model name filled in automatically and still editable | To do |
| 24 | Short service notes for the front end (methods, input, result, errors) | Rere | 18–20 | 29 Nov | Written in `docs/` | To do |
| 25 | Manual check: Windows window, tracker, dashboard | Deva | 15, 17, 23 | 29 Nov | Everything tried, including closing the window; bugs fixed or filed | To do |
| 26 | Visual review of all pages against the design | Satrio | 8–10, 12–17, 23 | 29 Nov | Differences fixed or accepted | To do |
| 27 | ERD of the Neon database | Rere | 20 | 29 Nov | Matches the real tables and relations | To do |
| 28 | Final merge into `main`, build the package and test it with Neon on a PC without .NET | Rere | 1–27 | 29 Nov | Build and tests pass; `publish.ps1` run from `main`; `.exe` works on the other PC | To do |
| 29 | Slides: introduction, task distribution, tech stack | Satrio | Nothing | 29 Nov | Three sections done; task distribution matches this file | To do |
| 30 | Slides: OOP implementation | Rere | 18, 19 | 29 Nov | Encapsulation, inheritance and polymorphism shown with real code from the app | To do |
| 31 | Slides: application testing | Deva | 21, 22, 25 | 29 Nov | Black-box results, 104+ automated tests and manual checks shown | To do |
| 32 | Assemble the final PPT, including ERD, Use Case Diagram and demo accounts | Satrio | 6, 27, 29–31 | 30 Nov | All required sections present; fits in 10 minutes | To do |
| 33 | Record the demo video | Brian | 28 | 30 Nov | At most 3 minutes; demo only, no introduction; the app uses Neon (not a local database) | To do |
| 34 | Update documents, each member their own part | All | 28 | 30 Nov | Content matches the app | To do |
| 35 | Presentation rehearsal, each member explains their own part | All | 32 | 30 Nov | One full rehearsal within 10 minutes | To do |
| 36 | Put the PPT, video and `.exe` package into one `.zip` and submit | Rere | 28, 32, 33 | 30 Nov | Zip opened and checked; submitted before 30 Nov 23:59 (aim for 18:00) | To do |

**If we are running late,** drop tasks 5, 20, 23 (AI app detection) first, then take them out of the "Waits for" column of the later tasks.

## Optional (only if everything above is done)

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
