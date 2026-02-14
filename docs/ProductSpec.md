# ProductSpec.md - PRD & Success Metrics

## PoNovaWeight Product Specification

This document defines the product requirements, business logic, and success metrics for the PoNovaWeight application.

---

## REGULAR VERSION

### Product Overview

**PoNovaWeight** is a personal food journaling Progressive Web App (PWA) designed for the Nova Physician Wellness Center's unit-based OMAD (One Meal A Day) nutritional tracking system. It helps users track their daily food intake using a simplified unit-based approach, monitor hydration, track OMAD compliance streaks, and analyze weight trends with alcohol correlation insights.

### Problem Statement

Physicians and wellness patients following the Nova Physician Wellness Center's program need a simple, mobile-friendly way to:
1. Track daily food intake using a standardized unit system (not calorie counting)
2. Monitor One Meal A Day (OMAD) compliance
3. Visualize weight trends over time
4. Understand how alcohol consumption affects their weight and wellness
5. Get AI-assisted meal analysis to simplify logging

### Target Users

| User Segment | Description | Key Needs |
|--------------|-------------|-----------|
| Primary Users | Physicians and wellness patients | Simple mobile logging, streak tracking, weight analysis |
| Secondary Users | Wellness coaches | Patient progress monitoring (future) |
| Admin Users | Clinic staff | User management, aggregate analytics (future) |

### Core Features

#### 1. Unit-Based Food Tracking
- **6 Food Categories**: Proteins, Vegetables, Fruits, Starches, Fats, Dairy
- **Daily Targets**: Based on Nova Physician Wellness guidelines:
  - Proteins: 5 units
  - Vegetables: 5 units
  - Fruits: 3 units
  - Starches: 2 units
  - Fats: 2 units
  - Dairy: 1 unit
- **Quick Increment**: Tap + button to add units
- **Visual Feedback**: Color-coded progress indicators

#### 2. Water Tracking
- **8-Segment Tracker**: Visual representation of 8 glasses/day
- **Quick Toggle**: Tap to increment water intake
- **Daily Reset**: Automatically resets each day

#### 3. OMAD (One Meal A Day) Tracking
- **Daily Toggle**: Mark each day as OMAD compliant or not
- **Streak Counter**: Current streak and longest streak display
- **Calendar View**: Visual OMAD compliance over time

#### 4. Weight Tracking
- **Daily Weigh-In**: Optional weight entry (50-500 lbs range)
- **30-Day Trends**: Line chart visualization
- **Moving Average**: Smoothed trend line

#### 5. Alcohol Correlation Analysis
- **Daily Log**: Track alcohol consumption yes/no
- **Correlation View**: Compare weight trends with/without alcohol
- **Insights**: AI-generated insights on alcohol's impact

#### 6. AI Meal Scanning (Optional Feature)
- **Photo Capture**: Take photo of meal
- **GPT-4o Analysis**: AI suggests unit breakdown
- **User Confirmation**: User reviews and confirms
- **Fallback**: Returns mock data when AI unavailable

### User Journeys

#### Journey 1: Daily Logging
```
1. User opens app (PWA on home screen)
2. App shows today's dashboard
3. User taps + on each food category as they eat
4. User taps water icon for hydration
5. At end of day, user toggles OMAD switch
6. User optionally logs weight
```

#### Journey 2: Review Progress
```
1. User opens Calendar view
2. Scrolls through past weeks/months
3. Clicks on specific day for details
4. Views weight trends chart
5. Checks alcohol correlation insights
```

#### Journey 3: Meal Scanning
```
1. User taps camera icon
2. Takes photo of meal
3. AI analyzes and suggests units
4. User adjusts if needed
5. User confirms and saves
```

### Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Daily Active Users (DAU) | >100 users/day | App Insights |
| Daily Logging Rate | >70% of users log | Table Storage queries |
| OMAD Logging Rate | >50% of users log OMAD | Table Storage queries |
| Average Session Duration | >2 minutes | App Insights |
| PWA Install Rate | >30% of users install | Browser analytics |
| API Response Time (p95) | <500ms | App Insights |
| Successful Meal Scans | >80% completion rate | API metrics |

### Technical Constraints

| Constraint | Description |
|------------|-------------|
| Platform | Modern browsers (Chrome, Safari, Edge) |
| PWA Support | Service Worker for offline support |
| Authentication | Google OAuth 2.0 only |
| Data Storage | Azure Table Storage |
| AI Service | Azure OpenAI GPT-4o (optional) |
| Hosting | Azure Container Apps |

### Future Roadmap

| Phase | Features | Timeline |
|-------|----------|----------|
| Phase 1 (Current) | Core logging, streaks, trends | Done |
| Phase 2 | Meal scanning AI | Current |
| Phase 3 | Coach/Admin dashboard | Q3 2026 |
| Phase 4 | Multi-user sharing | Q4 2026 |
| Phase 5 | Offline-first PWA | Q1 2027 |

---

## SIMPLIFIED VERSION

### What is PoNovaWeight?

A food journaling app for the Nova Physician Wellness Center program. Users track food using "units" instead of calories, following OMAD (One Meal A Day) principles.

### Core Features

1. **Food Tracking**: 6 categories (Proteins, Veggies, Fruits, Starches, Fats, Dairy)
2. **Water Tracking**: 8 glasses per day
3. **OMAD Tracking**: Mark days as "one meal a day"
4. **Weight Tracking**: Log daily weight with trends
5. **AI Meal Scan**: Take photo, AI suggests units

### User Flow

```
Open App → Log Food → Track Water → Mark OMAD → View Trends
```

### Success Goals

| Goal | Target |
|------|--------|
| Daily Users | 100+ |
| Log Their Food | 70% |
| Log OMAD | 50% |
| Response Time | Under 500ms |

### Tech Stack

- Frontend: Blazor WASM + Tailwind CSS
- Backend: ASP.NET Core API
- Database: Azure Table Storage
- AI: Azure OpenAI GPT-4o
- Auth: Google OAuth
- Hosting: Azure Container Apps
