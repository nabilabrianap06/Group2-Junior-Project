# **OxyGenAI**
## **Junior Project DTETI UGM | Group 2**

## Project Description
OxyGenAI is designed to track the environmental impact of generative AI usage. By analyzing token consumption and computational processes, the system estimates the corresponding carbon dioxide (CO2) emissions and water usage for each interaction.

## Group Member
- Group Leader: Deva Zukananda - 24/546873/TK/60780 
- Member 1 : Ursula Maurentti Amarely - 24/533008/TK/59050
- Member 2 : Satrio Suryo Anggoro Azis -24/536769/TK/59571
- Member 3 : Nabil Abrian Aryo Prabowo - 24/546496/TK/60770

---

## Tech Stack
This project is built using a pure **.NET ecosystem** to ensure a unified and maintainable architecture:

| Component | Technology |
| :--- | :--- |
| **Framework** | ASP.NET Core 10.0 (Blazor Web App) |
| **Language** | C# |
| **Database** | Neon (cloud PostgreSQL) + Entity Framework Core (Npgsql) |
| **Authentication** | ASP.NET Core Identity (Role-based: User & Admin) |
| **AI Integration** | Google Gemini REST API (`HttpClient`) |
| **Frontend UI** | Blazor Components + Bootstrap |
| **Windows app** | WinForms window hosting the app in Microsoft Edge WebView2 (`src/AIEnvironmentalTracker.Desktop`) |
| **Tests** | xUnit (`tests/AIEnvironmentalTracker.Tests`) |

---

## Key Features

### 1. Dynamic Token Tracking
- Integrates directly with the **Google Gemini API** to retrieve real-time usage metadata (`promptTokenCount`, `candidatesTokenCount`).
- Stores manually entered usage estimates in PostgreSQL.

### 2. Environmental Impact Calculator
- Uses a **token-weighted approximation** based on Google's published median-prompt estimates (May 2025).
- Calculates three core metrics:
  - **Energy Consumption** (Wh)
  - **Carbon Emissions** (gCO₂e)
  - **Water Consumption** (mL)

### 3. User Dashboard
- Visualizes daily, weekly, and monthly environmental trends.
- Provides personalized recommendations to reduce AI-related carbon footprints.

### 4. Admin Panel
- Role-based access control for administrators.
- Platform-wide aggregation of total tokens and environmental impact.
- User management (Add, Edit, Suspend).

---

## Project Architecture

The application follows a **modular monolithic** layered architecture:

```text
src/AIEnvironmentalTracker.Domain/        # Plain C# domain model
└── Models/              # EnvironmentalFactor, ImpactCalculator, CarbonFootprintReport,
                         # RecommendationEngine, TokenImpactEstimator, AIComparison, ...
src/AIEnvironmentalTracker.Desktop/       # Windows app: WinForms window + WebView2, runs the Web app in the background
src/AIEnvironmentalTracker.Web/           # Blazor Server app
├── Components/
│   ├── Pages/           # Home (usage tracker), Dashboard, AskGemini, Compare, Admin
│   ├── Usage/           # Reusable usage components
│   └── Account/         # Identity pages
├── Data/                # EF Core DbContext, entities & migrations
├── Services/            # Business logic
│   ├── GeminiService.cs            # Gemini REST calls, model allow-list
│   ├── AskLimiter.cs               # Per-user daily allowance for Gemini requests
│   ├── ModelComparisonService.cs   # Same prompt to several models, ranked
│   ├── UsageTrackingService.cs     # Persistence, totals, daily/weekly/monthly trends
│   └── AdminService.cs             # Accounts, roles, platform totals
└── Program.cs           # Entry point & dependency injection
tests/AIEnvironmentalTracker.Tests/       # xUnit tests (in-memory database, no PostgreSQL needed)
docs/                    # User guide, progress, design decisions
```

---

## Class Diagram
![Class Diagram](module3_classdiagram.png)

## Homework 8: PostgreSQL and OOP

The usage tracker on the home page demonstrates **encapsulation**. `EnvironmentalFactor` validates and holds per-query factors; `ImpactCalculator.ComputeImpact` calculates totals from a usage log and those factors; `UsageTrackingService.RecordAsync` saves the result in PostgreSQL (Neon) for the signed-in user. The values entered in the form are user-provided estimates, not measurements from an AI provider. The existing `ai_usage_logs` and `chat_logs` tables are left intact.

## Module 10: Minimum Viable Product

- **Usage tracker** (home page): manual records go through `EnvironmentalFactor` and `ImpactCalculator`. After saving, `RecommendationEngine` shows advice for that record. A summary shows your total queries, water (mL), CO₂ (g) and energy (Wh), held in a `CarbonFootprintReport`. Times are shown in your browser's time zone.
- **Ask Gemini** (signed-in users only): `GeminiService` calls the Gemini REST API (`generateContent`) and reads `usageMetadata` from the reply. The model is chosen from `Gemini:AllowedModels` (default `Gemini:Model`, currently `gemini-3.5-flash-lite`); **Compare models** sends one prompt to several of them. `AskLimits` caps the requests per user per day. `TokenImpactEstimator` turns the total token count into an `EnvironmentalFactor`. It follows Google's accounting chain (energy → carbon → water, [arXiv:2508.15734](https://arxiv.org/abs/2508.15734)) for the median Gemini Apps text prompt (May 2025: 0.24 Wh, 0.03 gCO₂e, 0.26 mL water), assuming that median prompt is 500 tokens and scaling energy linearly with total tokens. Sources, equations and limitations are at the top of `TokenImpactEstimator.cs`. The result is saved through `UsageTrackingService`. These figures are estimates, not measurements.
- Registration no longer needs email confirmation, and database migrations are applied automatically on startup.

### Install the packaged app (no .NET SDK needed)

OxyGenAI runs on your PC and is not deployed anywhere; only its database is in the cloud (Neon), so internet access is needed.

1. Get the connection string of the Neon project (Neon console, **Connect**).
2. Extract `OxyGenAI-win-x64.zip` (built with `publish.ps1`, see below).
3. Double-click `Setup OxyGenAI.bat` and paste the Neon connection string and, optionally, your Gemini API key. They are saved in `oxygenai.settings.json` next to `OxyGenAI.exe`, which is never committed.
4. Double-click **`OxyGenAI-Desktop.exe`**. OxyGenAI opens in its own Windows window; the tables are created in the Neon database at start-up. (It needs the Microsoft Edge WebView2 Runtime, which Windows 11 already has.) Alternatively, `Start OxyGenAI.bat` runs the same app and opens it in your browser at `http://localhost:5260`.

The full user guide, with troubleshooting, is in [docs/USER_GUIDE.md](docs/USER_GUIDE.md).

To build the package yourself: `powershell -ExecutionPolicy Bypass -File publish.ps1`. It writes a self-contained `dist/OxyGenAI-win-x64.zip` (about 100 MB) with `OxyGenAI-Desktop.exe` (the window) and `OxyGenAI.exe` (the server it starts in the background).

### Run locally

1. Install the .NET 10 SDK and get the Neon connection string. The app creates and updates the tables itself when it starts. Developers who share a database also share its schema changes; use a separate Neon branch for experiments.
2. Set the connection string and Gemini API key outside source control. Never put them in `appsettings.json`.

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=ep-your-endpoint.region.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=Require" --project src/AIEnvironmentalTracker.Web
   dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY" --project src/AIEnvironmentalTracker.Web
   ```

   User secrets are only loaded in the Development environment. In any other environment, set the environment variables `ConnectionStrings__DefaultConnection` and `Gemini__ApiKey` instead. You can get a Gemini key from Google AI Studio. To use a different model, set `Gemini__Model`.
3. Run the app:

   ```powershell
   dotnet run --project src/AIEnvironmentalTracker.Web --launch-profile http
   ```

4. Open `http://localhost:5260` and register; you are signed in right away. Use **Usage tracker** to enter manual estimates, **Ask Gemini** to send live prompts, **Compare models** to compare models, and **Dashboard** for trends.

**Admin account.** The app creates an admin account at start-up, so there is nothing to register:

| Email | Password |
| :--- | :--- |
| `admin@email.com` | `Admin#2026` |

These are demo values written in the code (`AdminService`), so anyone who can read this repository knows them. If the app is shared, change the password after signing in (click the email in the menu), or set `Admin:Email` and `Admin:Password` before the account is created. An account that already exists keeps its password.

Run the tests with `dotnet test AIEnvironmentalTracker.slnx`.

To run the Windows window from source, build the solution first (the window starts the built web project in the background), then:

```powershell
dotnet build AIEnvironmentalTracker.slnx
dotnet run --project src/AIEnvironmentalTracker.Desktop
```

If the Gemini key is missing, the Ask Gemini page explains how to set it. Network errors, rejected keys and rate limits appear as messages, and the app keeps running.
