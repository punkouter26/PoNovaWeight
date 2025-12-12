# Quickstart: OMAD Weight Tracking

**Feature**: 003-omad-weight-tracking  
**Date**: December 12, 2025  
**Purpose**: Step-by-step guide to run and test the OMAD tracking feature

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) installed
- [Node.js 20+](https://nodejs.org/) for Tailwind CSS
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) for local storage emulation
- VS Code with C# Dev Kit extension

---

## 1. Start Local Storage

Open a terminal and start Azurite:

```powershell
# From repository root
azurite --silent --location ./azurite --debug ./azurite/debug.log
```

Or use the VS Code task:
- Press `Ctrl+Shift+P` → "Tasks: Run Task" → "azurite-start"

---

## 2. Build and Run

### Option A: VS Code (Recommended)

1. Open the repository in VS Code
2. Press `F5` to start debugging
3. The API will launch at `https://localhost:7001`
4. The browser will open automatically to the Client

### Option B: Command Line

```powershell
# Terminal 1: Start API
cd src/PoNovaWeight.Api
dotnet run

# Terminal 2: Watch Tailwind (if making CSS changes)
cd src/PoNovaWeight.Client
npm run watch
```

---

## 3. Test the Feature

### Daily Log with OMAD Fields

1. Navigate to `/day/2025-12-12` (or today's date)
2. You should see:
   - Existing nutrient steppers (Proteins, Vegetables, etc.)
   - Water tracker
   - **NEW**: OMAD section with:
     - Weight input (numeric, pounds)
     - OMAD Compliant toggle (Yes/No)
     - Alcohol Consumed toggle (Yes/No)
3. Enter weight: `175.5`
4. Toggle OMAD: Yes
5. Toggle Alcohol: No
6. The data auto-saves (observe network tab for PUT request)

### Calendar View

1. Navigate to `/calendar`
2. You should see a monthly grid
3. Days with entries show colored indicators:
   - 🟢 Green = OMAD compliant
   - 🔴 Red = OMAD not compliant
   - ⚫ Gray = Not logged
4. Click a day to navigate to that day's detail page

### Streak Display

1. Log 3+ consecutive OMAD-compliant days
2. Dashboard should show "🔥 3 day streak"
3. Log a non-compliant day (OMAD = No)
4. Streak should reset to 0

### Weight Threshold Confirmation

1. Log weight as `170 lbs` for yesterday
2. Navigate to today
3. Enter weight as `180 lbs` (10 lb difference)
4. A confirmation dialog should appear
5. Confirm to save, or cancel to adjust

---

## 4. Run Tests

```powershell
# Run all tests
dotnet test

# Run with coverage
.\scripts\generate-coverage.ps1

# Run specific test class
dotnet test --filter "FullyQualifiedName~CalculateStreakTests"
```

### Test Categories

| Category | Command | Coverage Target |
|----------|---------|-----------------|
| Unit Tests | `dotnet test --filter Category=Unit` | Handlers, validators |
| Integration | `dotnet test --filter Category=Integration` | API endpoints |
| bUnit | `dotnet test --project tests/PoNovaWeight.Client.Tests` | Blazor components |

---

## 5. API Testing with .http Files

Open `src/PoNovaWeight.Api/http/dailylogs.http` in VS Code:

```http
### Get daily log
GET {{baseUrl}}/api/dailylogs/2025-12-12
Cookie: nova-session={{sessionCookie}}

### Upsert with OMAD fields
PUT {{baseUrl}}/api/dailylogs/2025-12-12
Content-Type: application/json
Cookie: nova-session={{sessionCookie}}

{
  "proteins": 3,
  "vegetables": 4,
  "fruits": 2,
  "starches": 2,
  "fats": 2,
  "dairy": 1,
  "waterSegments": 6,
  "weight": 175.5,
  "omadCompliant": true,
  "alcoholConsumed": false
}

### Get monthly calendar data
GET {{baseUrl}}/api/dailylogs/month/2025/12
Cookie: nova-session={{sessionCookie}}

### Get streak
GET {{baseUrl}}/api/dailylogs/streak
Cookie: nova-session={{sessionCookie}}

### Get weight trends (last 30 days)
GET {{baseUrl}}/api/dailylogs/trends?days=30
Cookie: nova-session={{sessionCookie}}

### Get alcohol correlation
GET {{baseUrl}}/api/dailylogs/alcohol-correlation
Cookie: nova-session={{sessionCookie}}

### Delete a log
DELETE {{baseUrl}}/api/dailylogs/2025-12-12
Cookie: nova-session={{sessionCookie}}
```

---

## 6. Verify Success Criteria

| Criteria | How to Verify |
|----------|---------------|
| SC-001: 10-second logging | Time from page load to save confirmation |
| SC-002: Calendar < 2s | Browser DevTools Network tab |
| SC-003: Streak accuracy | Log compliant/non-compliant days, verify counter |
| SC-004: First-attempt success | Usability observation |
| SC-005: Trend chart with 3+ points | Log 3 days, view trends |
| SC-006: Cross-session persistence | Log data, close browser, reopen, verify data persists |

---

## Troubleshooting

### "Connection refused" errors
- Ensure Azurite is running
- Check `appsettings.Development.json` has correct connection string

### OMAD fields not appearing
- Clear browser cache
- Ensure you're on the latest code (`git pull`)
- Check browser console for JavaScript errors

### Authentication issues
- Ensure Google OAuth is configured in `appsettings.json`
- For local testing, the app falls back to "dev-user" if not authenticated

---

## Next Steps

After verifying the quickstart:

1. Run `/speckit.tasks` to generate implementation tasks
2. Begin implementation with P1 user story (Daily Log Entry)
3. Follow TDD workflow for each handler
