# Detailed Sequence Diagrams

## Key User Flows

This document provides detailed sequence diagrams for the main user interactions in PoNovaWeight.

---

## 1. OAuth Authentication Flow (Detailed)

```mermaid
sequenceDiagram
    participant U as User
    participant B as Browser
    participant API as PoNovaWeight API
    participant G as Google OAuth
    participant Session as Session Store

    Note over U,B: User visits application
    B->>API: GET /
    API-->>B: index.html (Blazor App)
    
    Note over U,B: Not authenticated
    B->>API: GET /auth/status
    API-->>B: 401 Unauthorized
    
    B->>U: Show Login Page
    U->>B: Click "Sign in with Google"
    
    B->>API: GET /auth/login
    API->>API: Generate state token
    API->>Session: Store state (anti-forgery)
    API-->>B: 302 Redirect to Google
    
    B->>G: GET oauth/v2/auth?client_id=...&state=...
    G->>U: Show consent screen
    
    Note over U,G: User grants permission
    U->>G: Click "Allow"
    
    G->>B: 302 Redirect to /auth/callback?code=...&state=...
    B->>API: GET /auth/callback?code=...&state=...
    
    Note over API: Validate OAuth
    API->>G: POST oauth/token (exchange code)
    G-->>API: Access token + ID token
    
    API->>API: Validate ID token
    API->>API: Extract user email/name
    
    Note over API: Create session
    API->>Session: Create session (30 min timeout)
    API->>Session: Set auth cookie (HttpOnly, Secure)
    
    API-->>B: 302 Redirect to /auth/microsoft-callback
    
    B->>API: GET /auth/microsoft-callback
    API-->>B: Set session cookie<br/>302 Redirect to /dashboard
    
    Note over U,B: Authenticated
    B->>API: GET /api/dailylogs/today
    API->>Session: Validate session
    API-->>B: 200 OK (DailyLogDto)
    
    B->>U: Render Dashboard
```

---

## 2. Add Food Unit Flow

```mermaid
sequenceDiagram
    participant U as User
    participant B as Blazor Client
    participant API as PoNovaWeight API
    participant M as MediatR
    participant H as IncrementUnitHandler
    participant R as DailyLogRepository
    participant T as Azure Table Storage

    Note over U,B: User on Dashboard
    U->>B: Tap "+" on Proteins category
    
    B->>B: UnitStepper.OnClick()
    
    B->>API: POST /api/dailylogs/increment
    Note over API: Request: {date: "2026-02-14", category: "Proteins"}
    
    API->>M: Send(IncrementUnitCommand)
    
    Note over M: Validation Pipeline
    M->>M: ValidationBehavior.Validate()
    M->>M: FluentValidation passes
    
    M->>H: Handle(IncrementUnitCommand)
    
    Note over H: Business Logic
    H->>R: GetDailyLogAsync(date)
    R->>T: Query Table Storage
    T-->>R: DailyLogEntity (or null)
    R-->>H: DailyLogDto
    
    H->>H: IncrementProteins()
    H->>R: UpsertDailyLogAsync(dto)
    R->>T: UpsertEntity()
    T-->>R: OK
    R-->>H: Updated DailyLogDto
    
    H-->>M: DailyLogDto
    M-->>API: DailyLogDto
    
    API-->>B: 200 OK (DailyLogDto with proteins+1)
    
    B->>B: Update UI State
    B->>U: Show updated count
```

---

## 3. Meal Scanning Flow (AI)

```mermaid
sequenceDiagram
    participant U as User
    participant B as Blazor Client
    participant C as Camera API
    participant API as PoNovaWeight API
    participant M as MediatR
    participant H as ScanMealHandler
    participant AI as Azure OpenAI
    participant R as DailyLogRepository
    participant T as Azure Table Storage

    Note over U,B: User taps camera icon
    B->>C: Request camera access
    C-->>B: Camera stream
    
    U->>B: Take photo
    B->>B: Compress image to JPEG
    B->>B: Convert to Base64
    
    B->>API: POST /api/mealscan
    Note over API: Request: {imageBase64: "...", date: "2026-02-14"}
    
    API->>M: Send(ScanMealCommand)
    M->>H: Handle(ScanMealCommand)
    
    Note over H: AI Analysis
    H->>AI: GetChatCompletion()
    Note over AI: Prompt: "Analyze this meal photo<br/>and suggest units for:<br/>Proteins, Veggies, Fruits,<br/>Starches, Fats, Dairy"
    AI-->>H: JSON response {proteins: 4, veggies: 3, ...}
    
    H->>H: Parse AI response
    H->>H: Validate suggested units
    
    H-->>M: MealScanResultDto
    M-->>API: MealScanResultDto
    
    API-->>B: 200 OK (MealScanResultDto)
    
    B->>U: Show confirmation dialog
    
    Note over U,B: User reviews
    U->>B: Adjust units if needed
    U->>B: Click "Confirm"
    
    B->>API: PUT /api/dailylogs
    Note over API: Request: {date: "...", proteins: 4, veggies: 3, ...}
    
    API->>R: UpsertDailyLogAsync()
    R->>T: UpsertEntity()
    T-->>R: OK
    R-->>API: DailyLogDto
    
    API-->>B: 200 OK
    
    B->>U: Show success message<br/>Navigate to Dashboard
```

---

## 4. Weekly Summary Aggregation Flow

```mermaid
sequenceDiagram
    participant U as User
    participant B as Blazor Client
    participant API as PoNovaWeight API
    participant M as MediatR
    participant H as GetWeeklySummaryHandler
    participant R as DailyLogRepository
    participant T as Azure Table Storage
    participant Cache as Output Cache

    Note over U,B: User views Dashboard
    B->>API: GET /api/weeklysummary/2026-02-14
    
    Note over API: Check cache first
    API->>Cache: Get("weeklysummary:2026-02-14")
    
    alt Cache Hit
        Cache-->>API: Cached WeeklySummaryDto
        API-->>B: 200 OK (from cache)
        B->>U: Display cached summary
    else Cache Miss
        API->>M: Send(GetWeeklySummaryQuery)
        M->>H: Handle(GetWeeklySummaryQuery)
        
        Note over H: Calculate week start
        H->>H: GetMondayOfWeek(2026-02-14)<br/>=> 2026-02-09
        
        H->>R: GetDailyLogsAsync(startDate, days=7)
        
        Note over R: Query Table Storage
        R->>T: Query DailyLogs table
        Note over T: PartitionKey: UserId<br/>RowKey >= "2026-02-09"<br/>RowKey < "2026-02-16"
        T-->>R: 7 DailyLogEntities
        R-->>H: List<DailyLogDto>
        
        Note over H: Aggregate data
        H->>H: Sum all units
        H->>H: Count OMAD days
        H->>H: Calculate average weight
        
        H-->>M: WeeklySummaryDto
        M-->>API: WeeklySummaryDto
        
        Note over API: Cache result
        API->>Cache: Set("weeklysummary:2026-02-14", dto, expires: 30s)
        
        API-->>B: 200 OK WeeklySummaryDto
        B->>U: Display weekly summary
    end
```

---

## 5. Weight Trends Analysis Flow

```mermaid
sequenceDiagram
    participant U as User
    participant B as Blazor Client
    participant API as PoNovaWeight API
    participant M as MediatR
    participant H as GetWeightTrendsHandler
    participant R as DailyLogRepository
    participant T as Azure Table Storage

    Note over U,B: User clicks "View Trends"
    B->>API: GET /api/dailylogs/trends
    
    API->>M: Send(GetWeightTrendsQuery)
    M->>H: Handle(GetWeightTrendsQuery)
    
    Note over H: Calculate date range
    H->>H: endDate = today<br/>startDate = today - 30 days
    
    H->>R: GetDailyLogsAsync(startDate, days=30)
    R->>T: Query DailyLogs (last 30 days)
    T-->>R: List<DailyLogEntity>
    R-->>H: List<DailyLogDto>
    
    Note over H: Filter and analyze
    H->>H: Filter: weight != null
    H->>H: Calculate: daily changes
    H->>H: Calculate: moving average (7-day)
    H->>H: Calculate: min, max, average
    
    H-->>M: WeightTrendsDto
    M-->>API: WeightTrendsDto
    
    API-->>B: 200 OK WeightTrendsDto
    
    B->>B: Render chart with Chart.js
    B->>U: Display weight trend chart
```

---

## 6. Alcohol Correlation Analysis Flow

```mermaid
sequenceDiagram
    participant U as User
    participant B as Blazor Client
    participant API as PoNovaWeight API
    participant M as MediatR
    participant H as GetAlcoholCorrelationHandler
    participant R as DailyLogRepository
    participant T as Azure Table Storage

    Note over U,B: User views Alcohol Insights
    B->>API: GET /api/dailylogs/alcohol-correlation
    
    API->>M: Send(GetAlcoholCorrelationQuery)
    M->>H: Handle(GetAlcoholCorrelationQuery)
    
    H->>R: GetDailyLogsAsync(days: 30)
    R->>T: Query DailyLogs
    T-->>R: List<DailyLogEntity>
    R-->>H: List<DailyLogDto>
    
    Note over H: Analyze correlation
    H->>H: Group by AlcoholConsumed
    
    H->>H: Group A: AlcoholConsumed = true<br/>Calculate avg weight
    
    H->>H: Group B: AlcoholConsumed = false<br/>Calculate avg weight
    
    H->>H: Compare: avgWithAlcohol - avgWithoutAlcohol
    
    H->>H: Determine correlation direction:<br/>- positive: drinking = higher weight<br/>- negative: drinking = lower weight<br/>- neutral: no significant difference
    
    H-->>M: AlcoholCorrelationDto
    M-->>API: AlcoholCorrelationDto
    
    API-->>B: 200 OK AlcoholCorrelationDto
    
    B->>U: Display insights with chart
```

---

## Error Handling Flow

```mermaid
sequenceDiagram
    participant U as User
    participant B as Blazor Client
    participant API as PoNovaWeight API
    participant E as ExceptionHandler
    participant L as Serilog

    Note over U,B: User performs action
    B->>API: POST /api/dailylogs/increment
    
    alt Success
        API-->>B: 200 OK
        B->>U: Update UI
    else Validation Error
        API->>E: ValidationException
        E->>L: Log warning
        API-->>B: 422 Unprocessable Entity
        B->>U: Show validation errors
    else Authentication Error
        API->>E: UnauthorizedException
        API-->>B: 401 Unauthorized
        B->>U: Redirect to Login
    else Server Error
        API->>E: Exception
        E->>L: Log error with stack trace
        E->>E: Check IsDevelopment
        alt Development
            API-->>B: 500 with details
        else Production
            API-->>B: 500 Generic message
        end
        B->>U: Show error toast
    end
```

---

## Caching Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant API as PoNovaWeight API
    participant Cache as Output Cache
    participant H as Handler
    participant DB as Table Storage

    C->>API: GET /api/dailylogs/2026-02-14
    
    API->>Cache: Check cache
    
    alt Cache Hit
        Cache-->>API: Cached response
        API-->>C: 200 OK (cached)
    else Cache Miss
        API->>H: Execute handler
        H->>DB: Query database
        DB-->>H: Data
        H-->>API: Response
        
        API->>Cache: Store in cache (60 seconds)
        API-->>C: 200 OK
    end
    
    Note over C: User performs update
    C->>API: PUT /api/dailylogs
    
    API->>DB: Update data
    DB-->>API: Success
    
    API->>Cache: Invalidate related caches
    API-->>C: 200 OK
    
    Note over API: Next read gets fresh data
```
