# OxyGenAI – User Guide

OxyGenAI estimates the environmental impact (water, CO₂ and energy) of your generative-AI use.
You can log usage by hand, or send prompts to Google Gemini and let the app count the tokens for you.

---

## 1. What you need

| Requirement | Notes |
| :--- | :--- |
| Windows 10 or 11 (64-bit) | The package includes .NET, so you do **not** need to install the .NET SDK or runtime. |
| PostgreSQL 14 or newer | Download from <https://www.postgresql.org/download/windows/>. Remember the password you give the `postgres` user during installation. |
| Gemini API key (optional) | Only needed for the **Ask Gemini** page. Free from <https://aistudio.google.com/apikey>. |
| Microsoft Edge WebView2 Runtime | Shows the app window. It is already part of Windows 11 and of up-to-date Windows 10. If it is missing, OxyGenAI tells you and links to <https://developer.microsoft.com/microsoft-edge/webview2/>. |
| A web browser (optional) | Only if you prefer to use the app in a browser instead of its own window. |

---

## 2. Install

1. Extract `OxyGenAI-win-x64.zip` to a folder, for example `C:\OxyGenAI`.
2. Make sure PostgreSQL is running (it starts automatically after installation; in **Services** it is called `postgresql-x64-<version>`).
3. Double-click **`Setup OxyGenAI.bat`** and answer the questions. Press **Enter** to accept the value in brackets.

   | Question | What to enter |
   | :--- | :--- |
   | PostgreSQL host | `localhost` (default) |
   | PostgreSQL port | `5432` (default) |
   | Database name | `ai_environmental_db` (default). It does not need to exist: OxyGenAI creates it. |
   | PostgreSQL user | `postgres` (default) |
   | PostgreSQL password | The password you chose when you installed PostgreSQL (typing is hidden). |
   | Gemini API key | Paste your key, or press Enter to skip. |
   | Port the app listens on | `5260` (default) |

   The answers are saved to `oxygenai.settings.json` in the same folder. Keep that file private: it holds your database password and API key. Run the setup again any time to change them.

## 3. Start and stop

**As a Windows app (recommended)**
- **Start:** double-click **`OxyGenAI-Desktop.exe`**. The app opens in its own window; the database and its tables are created on the first start. If `oxygenai.settings.json` does not exist yet, the setup from section 2 runs first.
- **Stop:** close the window. The program that runs in the background stops with it.
- Links to other websites open in your default browser, not inside the window.

**In a browser (alternative)**
- **Start:** double-click **`Start OxyGenAI.bat`**. A console window opens and your browser opens at <http://localhost:5260>.
- **Stop:** close the console window (or press `Ctrl+C` in it).

Do not run both at the same time. The window version picks a free port by itself, so the "Port the app listens on" answer of the setup only applies to the browser version.

---

## 4. Using the app

### 4.1 Create an account and sign in
1. Click **Register**, enter your email and a password (at least 6 characters, with an upper-case letter, a lower-case letter, a digit and a symbol, e.g. `Green#2026`), confirm it and click **Register**.
2. You are signed in right away. Next time, use **Login**.
3. Click your email in the menu to manage your account (change password, two-factor authentication, download or delete your data). Click **Logout** to sign out.

### 4.2 Usage tracker (log usage by hand)
1. Open **Usage tracker**.
2. Fill in the AI model or application, the number of queries, and the water (mL), CO₂ (g) and energy (Wh) **per query**. These are your own estimates. As a reference, Google estimates one median Gemini text prompt at 0.26 mL water, 0.03 g CO₂e and 0.24 Wh.
3. Click **Save usage**. The app multiplies the values by the number of queries, saves the record, and shows:
   - **Your totals**: all your queries, water, CO₂ and energy so far;
   - **Recommendations**: tips for the metrics that are high for this record;
   - **Recent usage**: your last 20 records, in your browser's time zone.

### 4.3 Ask Gemini (web API service)
1. Open **Ask Gemini** (needs a Gemini API key, see section 2).
2. Type a prompt and click **Send to Gemini**.
3. The app calls the Google Gemini REST API, shows the reply, and reads the token counts from it. It estimates the impact as  
   `energy = 0.24 Wh × (total tokens ÷ 500)`, then  
   `CO₂e = energy × 94 g/kWh + embodied (0.010 g × tokens ÷ 500)` and  
   `water = (energy − overhead, PUE 1.09) × 1.15 L/kWh`  
   (method and sources: Google, arXiv:2508.15734; the 500-token reference is our own assumption)  
   and saves it to your usage history automatically, with recommendations.

You can pick the Gemini **model** in the list above the prompt. Only the models offered in the list can be used.

### 4.4 Compare models
1. Open **Compare models**.
2. Tick two or more models, type a prompt and click **Compare**.
3. The same prompt is sent to each model. The table lists the prompt, answer and thinking tokens and the estimated energy, CO₂ and water, lowest impact first. Open "Answer of …" to read each reply.
4. Every answer is a real request: it is saved to your usage history and counts towards your daily allowance.

All models use the same energy per token (Google publishes one figure for Gemini, not one per model), so the ranking shows which model needed fewer tokens for your prompt.

### 4.5 Dashboard
Open **Dashboard** and choose **Daily** (14 days), **Weekly** (12 weeks) or **Monthly** (12 months) to see your queries, water, CO₂ and energy over time, and a table per AI model.

### 4.6 Limits
- A prompt can have at most 4,000 characters.
- Each user can send 30 Gemini requests per day (resets at midnight UTC), with a pause of a few seconds between requests. A comparison of three models uses three requests.
- Google's free tier also has its own limits for all users together; when it is reached you see the message "Gemini rate limit or quota reached".

### 4.7 Admin panel (administrators only)
Administrators see **Admin** in the menu. It shows platform-wide totals and lets you add accounts, change an email, suspend or reactivate an account, and grant or remove the Admin role. Suspended accounts cannot sign in, but their history is kept. You cannot suspend yourself, remove your own admin role, or remove the last active admin.

These figures are **estimates, not measurements**.

---

## 5. Troubleshooting

| Problem | Solution |
| :--- | :--- |
| A message box says *"OxyGenAI could not start"* | Read the lines in the box. Most often PostgreSQL is not running or the password is wrong: start the PostgreSQL service and run `Setup OxyGenAI.bat` again. |
| A message box says the *WebView2 Runtime is not installed* | Install it from <https://developer.microsoft.com/microsoft-edge/webview2/> and start the app again. |
| The window stays on *"Starting OxyGenAI…"* for more than a minute | The server is slow to start or blocked. Close the window and start again; if it repeats, use `Start OxyGenAI.bat` and read the console. |
| Console shows *"Could not apply database migrations"* and stops | PostgreSQL is not running, or the password/port is wrong. Start the PostgreSQL service, then run `Setup OxyGenAI.bat` again. |
| Console shows *"The PostgreSQL connection string is not set"* | `oxygenai.settings.json` is missing. Run `Setup OxyGenAI.bat`. |
| *"Failed to bind to address … address already in use"* | Another program uses port 5260. Run the setup again and choose another port, e.g. `5280`. |
| "You have used your … Gemini requests for today" or "Please wait … second(s)" | You reached your own daily allowance or sent requests too fast. Wait, or ask an administrator to raise `AskLimits:DailyRequestsPerUser`. |
| A model shows "temporarily unavailable" in Compare models | Google's server is busy (HTTP 503). Try again in a moment; the other models still answer. |
| Ask Gemini says the API key is not set | Run the setup, paste your key, and restart OxyGenAI. |
| *"The Gemini API key was rejected"* | The key is wrong or was deleted. Create a new key in Google AI Studio and run the setup again. |
| *"Gemini rate limit or quota reached"* | The free tier allows a limited number of requests per minute. Wait a minute and try again. |
| Windows SmartScreen warns about `OxyGenAI-Desktop.exe` or `OxyGenAI.exe` | The programs are not code-signed. Click **More info → Run anyway**. |
| Browser does not open | Open <http://localhost:5260> yourself. |

---

## 6. For developers

Run from source with the .NET 10 SDK (see `README.md`):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ai_environmental_db;Username=postgres;Password=YOUR_PASSWORD" --project src/AIEnvironmentalTracker.Web
dotnet run --project src/AIEnvironmentalTracker.Web --launch-profile http
```

Run the Windows shell from source (build first: the shell starts the built web project in the background):

```powershell
dotnet build AIEnvironmentalTracker.slnx
dotnet run --project src/AIEnvironmentalTracker.Desktop
```

Check that the shell, the server and WebView2 work together (no clicks needed; writes one result line and exits):

```powershell
src\AIEnvironmentalTracker.Desktopin\Debug
et10.0-windows\OxyGenAI-Desktop.exe --check "C:\pathesult.txt"
```

Build the installable package (writes `dist/OxyGenAI-win-x64.zip` with `OxyGenAI-Desktop.exe` and `OxyGenAI.exe`, about 100 MB because each is self-contained):

```powershell
powershell -ExecutionPolicy Bypass -File publish.ps1
```

Settings are read in this order, later ones winning: `appsettings.json` → user secrets (Development only) → `oxygenai.settings.json` → environment variables (e.g. `ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`) → command-line arguments.
