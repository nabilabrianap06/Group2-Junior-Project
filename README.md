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
| **Framework** | ASP.NET Core 9.0 (Blazor Web App) |
| **Language** | C# |
| **Database** | Microsoft SQL Server + Entity Framework Core |
| **Authentication** | ASP.NET Core Identity (Role-based: User & Admin) |
| **AI Integration** | Google.GenAI SDK (.NET) |
| **Proxy** | YARP (Yet Another Reverse Proxy) |
| **Frontend UI** | Blazor Components + Bootstrap |

---

## Key Features

### 1. Dynamic Token Tracking
- Integrates directly with the **Google Gemini API** to retrieve real-time usage metadata (`promptTokenCount`, `candidatesTokenCount`).
- Logs every interaction into a structured SQL Server database.

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
AIEnvironmentalTracker.Web/
├── Components/          # Blazor UI (Pages, Layouts, Dashboard)
├── Data/                # EF Core DbContext & Migrations
├── Models/              # Database Entities (AIUsageLog, EnvironmentalFactor)
├── Services/            # Business Logic
│   ├── GeminiService.cs           # API Communication
│   ├── ImpactCalculator.cs        # Environmental Estimation Logic
│   └── UsageTrackingService.cs    # Data Persistence
└── Program.cs           # Entry point & Dependency Injection

---
## Class Diagram
!Complete Class Diagram(images/my-screenshot.png)
