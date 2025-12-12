# Nova Food Journal - Architecture

## System Overview

The Nova Food Journal is a Progressive Web App (PWA) built with Blazor WebAssembly for the frontend and ASP.NET Core Minimal API for the backend. It follows a Clean Architecture pattern with feature-based organization.

```mermaid
graph TB
    subgraph Client["Blazor WASM Client"]
        Pages["Pages<br/>(Dashboard, DayDetail, Login, MealScan)"]
        Components["Components<br/>(UnitStepper, WaterTracker, DayCard)"]
        Services["Services<br/>(ApiClient, SessionService)"]
    end

    subgraph API["ASP.NET Core API"]
        Endpoints["Minimal API Endpoints"]
        Handlers["MediatR Handlers"]
        Repository["DailyLogRepository"]
    end

    subgraph External["External Services"]
        Storage["Azure Table Storage"]
        OpenAI["Azure OpenAI<br/>GPT-4o"]
    end

    Pages --> Components
    Pages --> Services
    Services --> Endpoints
    Endpoints --> Handlers
    Handlers --> Repository
    Handlers --> OpenAI
    Repository --> Storage
```

## Component Architecture

### Frontend (Blazor WASM)

```mermaid
graph LR
    subgraph Pages
        Dashboard["Index.razor<br/>Dashboard"]
        DayDetail["DayDetail.razor<br/>Day View"]
        Login["Login.razor<br/>Auth"]
        MealScan["MealScanConfirm.razor<br/>AI Scan"]
    end

    subgraph Components
        DayCard["DayCard"]
        UnitStepper["UnitStepper"]
        ProgressBar["ProgressBar"]
        WaterTracker["WaterTracker"]
        CameraCapture["CameraCapture"]
        InstallPrompt["InstallPrompt"]
    end

    subgraph Services
        ApiClient["ApiClient"]
        SessionService["SessionService"]
    end

    Dashboard --> DayCard
    DayDetail --> UnitStepper
    DayDetail --> ProgressBar
    DayDetail --> WaterTracker
    MealScan --> CameraCapture
    Dashboard --> ApiClient
    DayDetail --> ApiClient
    Login --> SessionService
```

### Backend (ASP.NET Core)

```mermaid
graph TB
    subgraph Endpoints
        DailyLogs["DailyLogs<br/>/api/daily-logs"]
        WeeklySummary["WeeklySummary<br/>/api/weekly-summary"]
        MealScanEndpoint["MealScan<br/>/api/meal-scan"]
        Auth["Auth<br/>/api/auth"]
        Health["Health<br/>/api/health"]
    end

    subgraph Handlers["MediatR Handlers"]
        GetDailyLog["GetDailyLog"]
        SaveDailyLog["SaveDailyLog"]
        UpdateUnits["UpdateUnits"]
        UpdateWater["UpdateWater"]
        GetWeeklySummary["GetWeeklySummary"]
        ScanMeal["ScanMeal"]
        VerifyPasscode["VerifyPasscode"]
    end

    subgraph Infrastructure
        Repository["DailyLogRepository"]
        MealAnalysis["MealAnalysisService"]
        AuthMiddleware["AuthMiddleware"]
    end

    DailyLogs --> GetDailyLog
    DailyLogs --> SaveDailyLog
    DailyLogs --> UpdateUnits
    DailyLogs --> UpdateWater
    WeeklySummary --> GetWeeklySummary
    MealScanEndpoint --> ScanMeal
    Auth --> VerifyPasscode

    GetDailyLog --> Repository
    SaveDailyLog --> Repository
    GetWeeklySummary --> Repository
    ScanMeal --> MealAnalysis
```

## Data Flow

### Daily Log Flow

```mermaid
sequenceDiagram
    participant User
    participant UI as Blazor UI
    participant API as API Endpoint
    participant Handler as MediatR Handler
    participant Repo as Repository
    participant Storage as Table Storage

    User->>UI: Tap unit stepper
    UI->>API: PUT /api/daily-logs/{date}/units/{category}
    API->>Handler: UpdateUnits Command
    Handler->>Repo: GetAsync (current state)
    Repo->>Storage: GetEntity
    Storage-->>Repo: DailyLogEntity
    Repo-->>Handler: Entity or null
    Handler->>Handler: Apply delta
    Handler->>Repo: UpsertAsync
    Repo->>Storage: UpsertEntity
    Storage-->>Repo: Success
    Repo-->>Handler: Complete
    Handler-->>API: Updated DTO
    API-->>UI: 200 OK + DTO
    UI-->>User: Update display
```

### AI Meal Scan Flow

```mermaid
sequenceDiagram
    participant User
    participant Camera as CameraCapture
    participant Compress as ImageCompressor
    participant API as MealScan API
    participant OpenAI as Azure OpenAI
    participant Confirm as MealScanConfirm

    User->>Camera: Capture/Select photo
    Camera->>Compress: Resize to 800px
    Compress-->>Camera: Base64 image
    Camera->>API: POST /api/meal-scan
    API->>OpenAI: GPT-4o Vision + System Prompt
    OpenAI-->>API: JSON with unit suggestions
    API-->>Confirm: MealScanResultDto
    Confirm-->>User: Show suggestions with steppers
    User->>Confirm: Adjust values
    User->>Confirm: Tap Apply
    Confirm->>API: PUT /api/daily-logs/{date}
    API-->>Confirm: Updated log
    Confirm->>User: Navigate to day view
```

## Data Model

### Azure Table Storage Schema

```mermaid
erDiagram
    DailyLogs {
        string PartitionKey "User ID"
        string RowKey "Date (yyyy-MM-dd)"
        int Proteins "0-10"
        int Vegetables "0-10"
        int Fruits "0-10"
        int Starches "0-10"
        int Fats "0-10"
        int Dairy "0-10"
        int WaterSegments "0-8"
        datetime Timestamp "Azure managed"
        string ETag "Concurrency"
    }
```

### DTO Structure

```mermaid
classDiagram
    class DailyLogDto {
        +DateOnly Date
        +int Proteins
        +int Vegetables
        +int Fruits
        +int Starches
        +int Fats
        +int Dairy
        +int WaterSegments
        +GetUnits(category) int
        +IsOverTarget(category) bool
    }

    class WeeklySummaryDto {
        +DateOnly WeekStart
        +DateOnly WeekEnd
        +List~DailyLogDto~ Days
    }

    class MealScanResultDto {
        +bool Success
        +string? Error
        +List~SuggestedUnit~ Suggestions
    }

    class SuggestedUnit {
        +UnitCategory Category
        +int Units
        +string FoodItem
        +string Reasoning
    }

    WeeklySummaryDto --> DailyLogDto
    MealScanResultDto --> SuggestedUnit
```

## Authentication Flow

```mermaid
stateDiagram-v2
    [*] --> CheckAuth: App Loads
    CheckAuth --> Login: Not Authenticated
    CheckAuth --> Dashboard: Has Valid Session

    Login --> VerifyPasscode: Enter 4-digit code
    VerifyPasscode --> Login: Invalid
    VerifyPasscode --> SetCookie: Valid
    SetCookie --> Dashboard: Store Session

    Dashboard --> [*]: User Navigates

    note right of CheckAuth: SessionService.CheckAuthStatusAsync()
    note right of SetCookie: nova_session cookie (HttpOnly)
```

## Deployment Architecture

```mermaid
graph TB
    subgraph Azure["Azure (East US 2)"]
        subgraph AppService["App Service (B1)"]
            API["ASP.NET Core API"]
            Blazor["Blazor WASM<br/>(static files)"]
        end

        Storage["Azure Storage<br/>Table: DailyLogs"]
        OpenAI["Azure OpenAI<br/>GPT-4o"]
        AppInsights["Application Insights"]
        Budget["Budget Alert<br/>$5 / 80%"]
    end

    subgraph Client["User Device"]
        PWA["PWA<br/>(Service Worker)"]
        Cache["Cached Assets<br/>+ Offline Data"]
    end

    PWA --> AppService
    API --> Storage
    API --> OpenAI
    API --> AppInsights
    Blazor --> PWA
    PWA --> Cache
```

## Technology Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Frontend | Blazor WASM | Single language (C#), strong typing, component model |
| Backend | Minimal API | Lightweight, fast startup, simple routing |
| Database | Azure Table Storage | Low cost, simple key-value access, MVP appropriate |
| CQRS | MediatR | Clean handler separation, testability |
| Validation | FluentValidation | Expressive rules, easy testing |
| Testing | xUnit + bUnit + Moq | Standard .NET testing stack |
| AI | Azure OpenAI GPT-4o | Vision capability, enterprise compliance |

## Security Considerations

1. **Passcode Protection**: Simple 4-digit code prevents unauthorized access
2. **Session Cookies**: HttpOnly, Secure, SameSite=Strict cookies
3. **Single User MVP**: No multi-tenant concerns for initial release
4. **API Protection**: AuthMiddleware validates session on protected routes
5. **AI Cost Control**: Passcode gates access to AI features

## Performance Considerations

1. **PWA Caching**: Service worker caches static assets and API responses
2. **Image Compression**: Client-side resize before AI upload (800px max)
3. **Lazy Loading**: Blazor WASM assemblies loaded on demand
4. **Table Storage**: O(1) lookups via PartitionKey + RowKey
5. **Minimal API**: Reduced overhead vs. full MVC controllers
