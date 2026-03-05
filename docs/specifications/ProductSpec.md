# Product Specification: PoNovaWeight

## Overview

**PoNovaWeight** is a personal food journaling Progressive Web App (PWA) that enables users to track daily nutritional intake using a unit-based system aligned with the Nova Physician Wellness Center's OMAD (One Meal A Day) dietary protocol.

---

## Problem Statement

Users following specialized wellness protocols need a frictionless way to:
- Log daily food intake using category-based units (not calorie counting)
- Track water hydration visually
- Monitor weight trends and correlations with other lifestyle factors
- Receive AI-powered meal suggestions from photos
- Maintain OMAD streaks and consistency metrics
- Access analytics across weeks and months

Existing calorie-based trackers don't align with the Nova protocol, making manual tracking cumbersome and error-prone.

---

## Solution

A lightweight, offline-capable PWA that:
- Enables unit-based food logging across 6 food categories
- Provides one-tap AI meal analysis via GPT-4o
- Displays weight trends, blood pressure monitoring, and health correlations
- Shows OMAD compliance streaks and visual hydration tracking
- Syncs securely with Azure backend for scalability
- Works offline with local cache; syncs when online

---

## Key Features

### 1. **Food Journaling (Unit-Based)**
- 6 food categories: Proteins, Vegetables, Fruits, Starches, Fats, Dairy
- Daily unit counters with +/- stepper controls
- Quick-log modal for rapid data entry
- Historical edits and corrections

### 2. **AI Meal Scanning**
- Camera-enabled meal photo upload
- GPT-4o powered meal-to-unit analysis
- Suggested unit breakdown displayed for user confirmation
- Photo archive in blob storage

### 3. **Health Tracking**
- Daily weight logging (50–500 lbs range)
- Systolic & Diastolic BP (70–200 mmHg)
- Heart rate frequency (30–220 bpm)
- OMAD compliance flagging (boolean)
- Alcohol consumption tracking (boolean)

### 4. **Water Hydration**
- Visual 8-segment water tracker
- Daily segment counter (0–8 units = 0–64 oz)
- Persistent state across sessions

### 5. **Analytics & Insights**
- 30-day weight trend chart with trendline
- Alcohol consumption correlation analysis
- Weekly summary dashboard
- BP trend visualization
- Health insights based on logged data

### 6. **Streak Management**
- Current OMAD compliance streak counter
- Longest streak achievement
- Daily reset trigger at midnight UTC
- Visual streak badge

### 7. **User Authentication**
- Google OAuth integration (production)
- Dev test-user login with deterministic demo data (development)
- Secure JWT token management
- User profile with display name and picture

---

## System Architecture

### Technology Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | Blazor WebAssembly, Tailwind CSS 3.4, JavaScript interop |
| **Backend** | ASP.NET Core Minimal API, MediatR CQRS, FluentValidation |
| **Database** | Azure Table Storage (schema-flexible, cost-effective) |
| **AI/ML** | Azure OpenAI (GPT-4o for meal analysis) |
| **Media Storage** | Azure Blob Storage (meal photos) |
| **Observability** | Azure Application Insights, OpenTelemetry, Serilog |
| **Hosting** | Azure App Service |
| **Authentication** | Azure Entra ID / Google OAuth |

### Deployment Topology

```mermaid
C4Context
    title PoNovaWeight - System Context (Azure Deployment)
    
    System_Boundary "Azure" {
        Person(user, "User", "Food journaling PWA user")
        System(app, "PoNovaWeight PWA", "Blazor WASM frontend + .NET 10 API")
        SystemDb(tableStorage, "Azure Table Storage", "Daily logs, user settings, predictions")
        System(openAi, "Azure OpenAI (GPT-4o)", "Meal image analysis & nutrition suggestions")
        System(appInsights, "Azure Application Insights", "Observability & distributed tracing")
    }
    
    Rel(user, app, "Uses PWA: login, track meals, view analytics")
    Rel(app, tableStorage, "Store/retrieve user data & daily logs via REST API")
    Rel(app, openAi, "Send meal photos, receive unit predictions")
    Rel(app, appInsights, "Send telemetry: logs, traces, metrics")
    
    style user fill:#e1f5ff,color:#000
    style app fill:#fff3cd,color:#000
    style tableStorage fill:#d4edda,color:#000
    style openAi fill:#cfe2ff,color:#000
    style appInsights fill:#f8d7da,color:#000
```

---

## Success Metrics

### Engagement & Usage
| Metric | Target |
|--------|--------|
| Monthly Active Users (MAU) | ≥10 users by Q1 2026 |
| Daily Active Users (DAU) | ≥5 users by Q1 2026 |
| Log Completion Rate | ≥80% of tracked days |
| Session Duration | ≥3 minutes avg |

### User Satisfaction
| Metric | Target |
|--------|--------|
| Feature Adoption Rate (AI Scan) | ≥60% of users within 2 weeks |
| Trend Chart Views | ≥30% weekly engagement |
| User Retention (Day 7) | ≥70% of new signups |
| App Store Rating | ≥4.5/5.0 |

### Technical Performance
| Metric | Target |
|--------|--------|
| Page Load Time (PWA) | <1.5 seconds |
| API Response Time (p95) | <500ms |
| Image Analysis Latency (GPT-4o) | <5 seconds |
| Uptime (SLA) | 99.5% |
| Error Rate | <0.1% |

### Business Metrics
| Metric | Target |
|--------|--------|
| Cost per Active User | <$0.50/month |
| Azure Spend | <$5/month (free tier + minimal compute) |
| User Support Ticket Count | <2/month |

---

## Data Model

### Core Entities

**User**
- UserId (UUID, PK)
- Email (Unique)
- DisplayName
- ProfilePictureUrl
- CreatedAt, UpdatedAt

**DailyLog**
- PartitionKey: UserId
- RowKey: LogDate (YYYY-MM-DD)
- Proteins, Vegetables, Fruits, Starches, Fats, Dairy (int units)
- WaterSegments (0–8)
- Weight (decimal, nullable, lbs)
- OmadCompliant (bool, nullable)
- AlcoholConsumed (bool, nullable)
- SystolicBP, DiastolicBP (decimal, nullable, mmHg)
- HeartRate (int, nullable, bpm)
- CreatedAt, UpdatedAt

**UserSettings**
- PartitionKey: UserId
- RowKey: "SETTINGS"
- TargetWeight (decimal, lbs)
- UpdatedAt

**MealScan**
- PartitionKey: UserId
- RowKey: ScanId (UUID)
- LogDate (date ref)
- ImageUrl (blob storage URI)
- GptAnalysisJson (raw GPT response)
- CreatedAt

**Predictions** (WeightPrediction, BpPrediction)
- PartitionKey: UserId
- RowKey: PredictionId (UUID)
- PredictedValue (decimal)
- ConfidenceScore (int 0–100)
- CreatedAt

---

## User Journeys

### Journey 1: Daily Log Entry
1. User opens PWA
2. If not authenticated, logs in via Google or dev endpoint
3. Sees dashboard with calendar grid for current month
4. Clicks on today's date
5. DayDetail page opens
6. User enters food units (Proteins, Veggies, etc.) via steppers
7. Optionally logs weight, BP, HR
8. Optionally marks OMAD compliance
9. Saves → updates DailyLog in Table Storage
10. Dashboard re-renders, streak updates

### Journey 2: AI Meal Scanning
1. User clicks "Scan Meal" button
2. Camera capture page opens
3. User takes photo of meal
4. Image preview displayed
5. User confirms; sends to GPT-4o
6. GPT-4o returns unit suggestions (e.g., 2 proteins, 3 veggies, 1 fruit)
7. User reviews and confirms
8. Units auto-fill in quick-log modal
9. User adjusts if needed, then saves
10. MealScan record created with image + analysis

### Journey 3: Analytics & Insights
1. User clicks "Trends" button
2. WeeklySummary page opens
3. Shows:
   - 30-day weight chart (with trendline)
   - BP trends (systolic/diastolic)
   - Alcohol correlation (weight change vs. alcohol days)
   - Health insights (e.g., "BP improved 5% this week")
4. User scrolls through historical weekly summaries
5. Data pulled from DailyLog and Predictions tables

---

## Roadmap

### Phase 1: MVP (Completed)
- ✅ Unit-based daily logging
- ✅ AI meal scanning
- ✅ Basic weight & BP tracking
- ✅ OMAD streaks
- ✅ 30-day trend charts
- ✅ offline support via localStorage

### Phase 2: Enhanced Analytics (In Development)
- 🔄 Advanced correlation analysis (alcohol, exercise, mood)
- 🔄 Predictive analytics (ML-driven weight/BP forecasts)
- 🔄 Social features (optional sharing, accountability)

### Phase 3: Ecosystem Integration (Planning)
- 📅 Calendar sync, reminders
- 🏥 HL7 FHIR export for healthcare providers
- 📲 Native Android/iOS app with Capacitor

---

## Success Criteria

The product is considered successful when:
1. At least 10 active users consistently log daily meals
2. AI meal scanning adoption reaches 60% of users
3. Weekly trend charts are viewed by ≥30% of users
4. System maintains 99.5% uptime across a month
5. Average API response time stays below 500ms
6. User retention at day 7 exceeds 70%
7. Monthly Azure cost remains below $5

---

## Non-Goals

- Calorie-based logging (out of scope; unit system is preferred)
- Social networking features (phase beyond MVP)
- Native mobile apps (using PWA for now)
- Integration with fitness trackers (future phase)
- Meal planning or recipe suggestions (out of scope)

---

## Assumptions & Dependencies

- **Assumption 1**: Users have stable internet for OAuth login and API sync
- **Assumption 2**: Azure OpenAI availability and quota suffice for 10–20 meal scans/day
- **Assumption 3**: Users are motivated by Nova Wellness Center protocols
- **Dependency 1**: Azure Entra ID OAuth provider configured
- **Dependency 2**: Azure OpenAI deployment with GPT-4o available
- **Dependency 3**: Table Storage access and Blob Storage containers provisioned

---

## Constraints

- **Budget**: Monthly Azure spend must not exceed $10
- **Storage**: DailyLog retention for 3+ years (minimal overhead due to Table Storage pricing)
- **Latency**: API calls must complete within 500ms for responsive UX
- **Image Size**: Meal scan images limited to 5 MB per photo
- **Concurrency**: Support ≤20 simultaneous API requests

---

## References

- [Architecture Overview](Architecture.mmd)
- [Application Flow](ApplicationFlow.mmd)
- [Data Model](DataModel.mmd)
- [Component Map](ComponentMap.mmd)
- [Data Pipeline](DataPipeline.mmd)
