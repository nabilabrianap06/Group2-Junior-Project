# OxyGenAI – User Guide

OxyGenAI estimates the environmental impact (water, CO₂ and energy) of your generative-AI use.
You can log usage by hand, or send prompts to Google Gemini and let the app count the tokens for you.

---

## 1. What you need

OxyGenAI runs **on your own PC**; it is not a website and is not deployed anywhere. Its data is stored in a **Neon** database (PostgreSQL in the cloud), so the PC needs an internet connection.

| Requirement | Notes |
| :--- | :--- |
| Windows 10 or 11 (64-bit) | The package includes .NET, so you do **not** need to install the .NET SDK or runtime. |
| Internet connection | Needed for the database and for Gemini. |
| Neon connection string | From the project's Neon console (<https://neon.tech>): open the project, press **Connect**, copy the connection string. Ask the project owner if you do not have access. No database software is installed on the PC. |
| Gemini API key (optional) | Only needed for **Ask Gemini** and **Compare models**. Free from <https://aistudio.google.com/apikey>. |
| Microsoft Edge WebView2 Runtime | Shows the app window. It is already part of Windows 11 and of up-to-date Windows 10. If it is missing, OxyGenAI tells you and links to <https://developer.microsoft.com/microsoft-edge/webview2/>. |
| A web browser (optional) | Only if you prefer to use the app in a browser instead of its own window. |

---

## 2. Install

1. Extract `OxyGenAI-win-x64.zip` to a folder, for example `C:\OxyGenAI`.
2. Double-click **`Setup OxyGenAI.bat`** and answer the questions. Typed or pasted secrets are hidden.

   | Question | What to enter |
   | :--- | :--- |
   | Neon connection string | Paste what the Neon console shows (`postgresql://...`) and press Enter. The `Host=...;Database=...` form works too. Only Neon hosts (`.neon.tech`) are accepted. |
   | Gemini API key | Paste your key, or press Enter to skip. |
   | Port for the browser version | Press Enter for `5260`. |

   The setup adds `SSL Mode=Require` and uses Neon's direct (not "pooler") address. The answers are saved to `oxygenai.settings.json` in the same folder. **Keep that file private:** it holds the database password and API key. Run the setup again any time to change them.

Everyone who uses the same connection string shares the same data (accounts and usage records).

## 3. Start and stop

**As a Windows app (recommended)**
- **Start:** double-click **`OxyGenAI-Desktop.exe`**. The app opens in its own window; the tables are created or updated in the Neon database at start-up. If `oxygenai.settings.json` does not exist yet, the setup from section 2 runs first.
- **Stop:** close the window. The program that runs in the background stops with it.
- Links to other websites open in your default browser, not inside the window.

**In a browser (alternative)**
- **Start:** double-click **`Start OxyGenAI.bat`**. A console window opens and your browser opens at <http://localhost:5260>.
- **Stop:** close the console window (or press `Ctrl+C` in it).

Do not run both at the same time. The window version picks a free port by itself, so the "Port the app listens on" answer of the setup only applies to the browser version.

---

## 4. Using the app

### 4.1 Create an account and sign in
1. On the log-in screen choose **Create an account**, enter your email and a password (at least 6 characters, with an upper-case letter, a lower-case letter, a digit and a symbol, e.g. `Green#2026`), confirm it and click **Create account**.
2. You are signed in right away. Next time, use **Log in**.
3. The pages are in the bar at the top. Click your name there to manage your account (change password, two-factor authentication, download or delete your data) and **Log out** to sign out.

The app does not send email, so the "Forgot password?" link cannot reset a password.

### 4.2 Usage tracker (log usage by hand)
1. Open **Usage tracker**.
2. Under **Record usage**, enter the AI model or application (or pick one of the quick choices), the number of queries, and the water (mL), CO₂ (g) and energy (Wh) **per query**. These are your own estimates. **Use them** fills in Google's figures for a median Gemini prompt (0.26 mL, 0.03 g, 0.24 Wh). A preview shows the totals of the record.
3. Save the record. The page shows your totals, your water use by model, recommendations, and your recent records in your browser's time zone.

### 4.3 Ask Gemini (web API service)
1. Open **Ask Gemini** (needs a Gemini API key, see section 2).
2. Pick a **model**, type a prompt and send it. Only the models offered in the list can be used.
3. The app calls the Google Gemini REST API, shows the reply, and reads the token counts from it. The **This reply** panel lists the tokens and the estimated impact:  
   `energy = 0.24 Wh × (total tokens ÷ 500)`, then  
   `CO₂e = energy × 94 g/kWh + embodied (0.010 g × tokens ÷ 500)` and  
   `water = (energy − overhead, PUE 1.09) × 1.15 L/kWh`  
   (method and sources: Google, arXiv:2508.15734; the 500-token reference is our own assumption).  
   Each reply is saved to your usage history.

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
OxyGenAI creates an administrator account the first time it starts. Sign in with:

- **Email:** `admin@email.com`
- **Password:** `Admin#2026`

This password is the same on every installation. Change it after the first sign-in (click the email in the menu) if other people can use the computer. To start with different values, add `"Admin": { "Email": "...", "Password": "..." }` to `oxygenai.settings.json` before the first start (the password needs at least 6 characters with an upper-case letter, a lower-case letter, a digit and a symbol; running the setup again rewrites the file). These values do not change the password of an account that already exists.

Administrators see **Admin** in the menu. It shows platform-wide totals and lets you add accounts, change an email, suspend or reactivate an account, and grant or remove the Admin role. Suspended accounts cannot sign in, but their history is kept. You cannot suspend yourself, remove your own admin role, or remove the last active admin.

These figures are **estimates, not measurements**.

---

## 5. Troubleshooting

| Problem | Solution |
| :--- | :--- |
| A message box says *"OxyGenAI could not start"* | Read the lines in the box. Most often there is no internet connection or the connection string is wrong: check the connection, then run `Setup OxyGenAI.bat` again. |
| A message box says the *WebView2 Runtime is not installed* | Install it from <https://developer.microsoft.com/microsoft-edge/webview2/> and start the app again. |
| The window stays on *"Starting OxyGenAI…"* for more than a minute | The database may be waking up or the network is slow. Close the window and start again; if it repeats, use `Start OxyGenAI.bat` and read the console. |
| Console shows *"Could not apply database migrations"* and stops | No internet, or the Neon host, user or password is wrong. Copy the connection string again from the Neon console and run `Setup OxyGenAI.bat`. |
| Setup says the host *is not a Neon host* | Paste the connection string of a Neon project; a local database or another provider is not accepted. |
| The first page after a pause is slow | The free Neon database sleeps when idle and needs a few seconds to wake up. |
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
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=ep-your-endpoint.region.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=Require" --project src/AIEnvironmentalTracker.Web
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
et10.0-windows\OxyGenAI-Desktop.exe --check "C:\path
esult.txt"
```

Build the installable package (writes `dist/OxyGenAI-win-x64.zip` with `OxyGenAI-Desktop.exe` and `OxyGenAI.exe`, about 100 MB because each is self-contained):

```powershell
powershell -ExecutionPolicy Bypass -File publish.ps1
```

Settings are read in this order, later ones winning: `appsettings.json` → user secrets (Development only) → `oxygenai.settings.json` → environment variables (e.g. `ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`) → command-line arguments.
