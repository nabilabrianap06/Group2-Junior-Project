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
| A web browser | Chrome, Edge or Firefox. |

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

- **Start:** double-click **`Start OxyGenAI.bat`**. A console window opens, the database and its tables are created on the first start, and your browser opens at <http://localhost:5260>.
- **Stop:** close the console window (or press `Ctrl+C` in it).

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
   `impact = Google's median-prompt estimate × (total tokens ÷ 500)`  
   and saves it to your usage history automatically, with recommendations.

These figures are **estimates, not measurements**.

---

## 5. Troubleshooting

| Problem | Solution |
| :--- | :--- |
| Console shows *"Could not apply database migrations"* and stops | PostgreSQL is not running, or the password/port is wrong. Start the PostgreSQL service, then run `Setup OxyGenAI.bat` again. |
| Console shows *"The PostgreSQL connection string is not set"* | `oxygenai.settings.json` is missing. Run `Setup OxyGenAI.bat`. |
| *"Failed to bind to address … address already in use"* | Another program uses port 5260. Run the setup again and choose another port, e.g. `5280`. |
| Ask Gemini says the API key is not set | Run the setup, paste your key, and restart OxyGenAI. |
| *"The Gemini API key was rejected"* | The key is wrong or was deleted. Create a new key in Google AI Studio and run the setup again. |
| *"Gemini rate limit or quota reached"* | The free tier allows a limited number of requests per minute. Wait a minute and try again. |
| Windows SmartScreen warns about `OxyGenAI.exe` | The program is not code-signed. Click **More info → Run anyway**. |
| Browser does not open | Open <http://localhost:5260> yourself. |

---

## 6. For developers

Run from source with the .NET 10 SDK (see `README.md`):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ai_environmental_db;Username=postgres;Password=YOUR_PASSWORD" --project src/AIEnvironmentalTracker.Web
dotnet run --project src/AIEnvironmentalTracker.Web --launch-profile http
```

Build the installable package (writes `dist/OxyGenAI-win-x64.zip`):

```powershell
powershell -ExecutionPolicy Bypass -File publish.ps1
```

Settings are read in this order, later ones winning: `appsettings.json` → user secrets (Development only) → `oxygenai.settings.json` → environment variables (e.g. `ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`) → command-line arguments.
