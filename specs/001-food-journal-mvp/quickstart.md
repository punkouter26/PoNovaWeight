# Quickstart Guide: PoNovaWeight Food Journal

**Prerequisites**: .NET 10 SDK, Node.js 20+, Azure CLI (optional), VS Code

## 1. Clone and Setup

```powershell
# Clone repository
git clone <repo-url> PoNovaWeight
cd PoNovaWeight

# Checkout feature branch
git checkout 001-food-journal-mvp
```

## 2. Install Dependencies

```powershell
# Restore .NET packages
dotnet restore

# Install Tailwind CSS (client project)
cd src/PoNovaWeight.Client
npm install
cd ../..
```

## 3. Configure Local Development

### Azurite (Local Azure Storage)

```powershell
# Install Azurite globally
npm install -g azurite

# Start Azurite (in separate terminal)
azurite --silent --location ./azurite --debug ./azurite/debug.log
```

### User Secrets (API project)

```powershell
cd src/PoNovaWeight.Api

# Initialize user secrets
dotnet user-secrets init

# Set Azure OpenAI credentials (for meal scan)
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o"

# Set passcode for dev
dotnet user-secrets set "Auth:Passcode" "1234"

cd ../..
```

### appsettings.Development.json

The API project uses connection string format for Azure Storage:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "UseDevelopmentStorage=true"
  },
  "Auth": {
    "Passcode": "1234"
  }
}
```

## 4. Run the Application

### Option A: VS Code Tasks

1. Open workspace in VS Code
2. Press `Ctrl+Shift+B` → Select **Run API + Client**
3. Open browser to `https://localhost:7001`

### Option B: CLI

```powershell
# Terminal 1: Azurite
azurite --silent --location ./azurite

# Terminal 2: API (hosts both API and Blazor client)
cd src/PoNovaWeight.Api
dotnet watch run
```

Application runs at:
- **App**: https://localhost:7001
- **API**: https://localhost:7001/api

## 5. First-Time Setup

1. Navigate to https://localhost:7001
2. Enter passcode: `1234` (dev default)
3. You'll see the weekly dashboard with empty days
4. Tap any day → Start logging units

## 6. Run Tests

```powershell
# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Unit tests only
dotnet test --filter "Category!=Integration&Category!=E2E"

# E2E tests (requires app running)
dotnet test --filter "Category=E2E"
```

## 7. Development Workflow

### Hot Reload

- **API**: `dotnet watch run` auto-recompiles on changes
- **Blazor**: Browser refresh reflects changes
- **Tailwind**: `npm run watch` for CSS changes (in Client project)

### Feature Flags

```json
// appsettings.Development.json
{
  "Features": {
    "MealScan": true,    // Enable AI scanning
    "WaterTracking": true // Enable water UI
  }
}
```

## 8. Common Tasks

### Reset Local Storage

```powershell
# Stop Azurite, delete data, restart
Stop-Process -Name "node" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ./azurite
azurite --silent --location ./azurite
```

### Test AI Meal Scanning

1. Ensure Azure OpenAI credentials in user-secrets
2. Navigate to any day's detail view
3. Tap "Scan Meal" → Allow camera
4. Take photo → Review suggestions → Confirm

### Check API Health

```powershell
# Health endpoint
curl https://localhost:7001/health

# Expected: {"status":"Healthy"}
```

## 9. Project Structure Reference

```
src/
├── PoNovaWeight.Api/         # Blazor Host + API
│   ├── Features/             # Vertical slices
│   │   ├── DailyLogs/        # Unit tracking
│   │   ├── WeeklySummary/    # Dashboard queries
│   │   ├── MealScan/         # AI integration
│   │   └── Auth/             # Passcode auth
│   └── Infrastructure/       # Storage, AI clients
│
├── PoNovaWeight.Client/      # Blazor WASM
│   ├── Features/             # UI components by feature
│   │   ├── DailyLogs/        # Day detail page
│   │   ├── Dashboard/        # Weekly view
│   │   └── MealScan/         # Camera + preview
│   └── Shared/               # Layout, navigation
│
└── PoNovaWeight.Shared/      # DTOs, contracts
    ├── DTOs/
    └── Validation/
```

## 10. Troubleshooting

| Issue | Solution |
|-------|----------|
| Azurite connection refused | Ensure Azurite is running on default port 10000 |
| AI scan returns 503 | Check Azure OpenAI credentials in user-secrets |
| Tailwind styles not updating | Run `npm run build` in Client project |
| Hot reload not working | Restart `dotnet watch run` |
| HTTPS cert error | Run `dotnet dev-certs https --trust` |

## Next Steps

- [ ] Review `spec.md` for full requirements
- [ ] Check `data-model.md` for entity schemas
- [ ] See `contracts/openapi.yaml` for API spec
- [ ] Run full test suite before committing
