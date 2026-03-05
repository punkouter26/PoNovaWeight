# DevOps Guide: PoNovaWeight

## Deployment Architecture

PoNovaWeight is deployed to **Azure App Service** using a unified container model (Blazor WASM frontend + .NET API in same App Service).

### Deployment Topology

```
┌─────────────────────────────────────────────────────┐
│          Azure App Service (Linux)                  │
├─────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────┐  │
│  │  Container Image (Dockerfile)                │  │
│  │  ├─ .NET 10 API (port 8080 internal)        │  │
│  │  ├─ Blazor WASM assets (wwwroot)            │  │
│  │  └─ Reverse proxy → API                     │  │
│  └──────────────────────────────────────────────┘  │
│                                                      │
│  ┌──────────────────────────────────────────────┐  │
│  │  Application Insights (Observability)       │  │
│  │  ├─ Traces, Logs, Metrics                   │  │
│  │  ├─ Distributed tracing (OpenTelemetry)    │  │
│  │  └─ Availability tests                      │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
         ↓
    ┌─────────────────────────────────┐
    │   External Dependencies          │
    ├─────────────────────────────────┤
    │  • Azure Table Storage (data)   │
    │  • Azure Blob Storage (photos)  │
    │  • Azure Key Vault (secrets)    │
    │  • Azure OpenAI (meal analysis) │
    └─────────────────────────────────┘
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

```yaml
name: Deploy PoNovaWeight
on:
  push:
    branches:
      - main
jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      # Build & Test
      - run: dotnet build PoNovaWeight.sln
      - run: dotnet test PoNovaWeight.sln --no-build
      
      # Build Docker Image
      - run: docker build -t ponova:${{ github.sha }} .
      
      # Push to Azure Container Registry
      - run: |
          docker login -u ${{ secrets.ACR_USERNAME }} \
                       -p ${{ secrets.ACR_PASSWORD }} \
                       ponova.azurecr.io
          docker tag ponova:${{ github.sha }} \
                     ponova.azurecr.io/ponova:latest
          docker push ponova.azurecr.io/ponova:latest
      
      # Deploy to App Service
      - uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - uses: azure/webapps-deploy@v2
        with:
          app-name: 'PoNovaWeight'
          images: ponova.azurecr.io/ponova:latest
```

---

## Build & Deployment Commands

### Local Build
```bash
# Clean, restore, build
dotnet clean
dotnet restore
dotnet build PoNovaWeight.sln /property:GenerateFullPaths=true

# Build API only
dotnet build src/PoNovaWeight.Api/PoNovaWeight.Api.csproj
```

### Local Run
```bash
# Development (with hot reload)
dotnet watch run --project src/PoNovaWeight.Api

# Production-like
dotnet run --project src/PoNovaWeight.Api --configuration Release
```

### Docker Build & Run
```bash
# Build image
docker build -t ponova:local -f src/PoNovaWeight.Api/Dockerfile .

# Run container locally
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  ponova:local
```

### Azure Deployment (azd)
```bash
# Initialize (first time)
azd init

# Provision infrastructure (Bicep)
azd provision

# Deploy application
azd deploy

# Full cycle
azd up
```

---

## Environment Configuration

### Configuration Priority (Highest Wins)
1. **Environment variables** (Docker, App Service settings)
2. **User secrets** (local development only)
3. **Azure Key Vault** (production secrets)
4. **appsettings.{Environment}.json**
5. **appsettings.json** (defaults)

### Required Environment Secrets

#### Production (Azure App Service)

| Secret | Description | Source |
|--------|-------------|--------|
| `KeyVault:VaultUri` | Azure Key Vault URI | Manual setup |
| `OpenTelemetry:OtlpEndpoint` | OTLP collector endpoint | Azure Monitor |
| `ApplicationInsights:InstrumentationKey` | App Insights key | Auto-injected |
| `DataProtection:BlobUri` | Blob storage for DP keys | Manual setup |
| `AzureStorage:ConnectionString` | Table Storage connection | Azure Storage |

#### In Azure Key Vault

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `Google--ClientId` | Google OAuth app ID | `1234567890-abc...` |
| `Google--ClientSecret` | Google OAuth secret | `GOCSPX-abc...` |
| `Azure-OpenAI--Endpoint` | Azure OpenAI resource URL | `https://xxxxx.openai.azure.com/` |
| `Azure-OpenAI--Key` | Azure OpenAI API key | `sk-xxxxx` |
| `Azure-Storage--ConnectionString` | Table Storage conn str | `DefaultEndpointsProtocol=https;...` |

### Local Development

Create `src/PoNovaWeight.Api/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AspNetCore": {
    "Urls": "http://localhost:5000;https://localhost:5001"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "Azure": {
    "OpenAI": {
      "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
      "Key": "YOUR_OPENAI_KEY",
      "DeploymentName": "gpt-4o"
    },
    "Storage": {
      "ConnectionString": "UseDevelopmentStorage=true"
    }
  }
}
```

Set user secrets for local dev:
```bash
dotnet user-secrets set "Google:ClientId" "your-client-id"
dotnet user-secrets set "Google:ClientSecret" "your-secret"
dotnet user-secrets set "Azure:OpenAI:Endpoint" "https://..."
dotnet user-secrets set "Azure:OpenAI:Key" "your-key"
```

---

## Infrastructure as Code (Bicep)

### Main Infrastructure File
**`infra/main.bicep`** orchestrates all Azure resources:

```bicep
targetScope = 'resourceGroup'

param environmentName string = 'prod'
param location string = resourceGroup().location
param baseName string = 'PoNovaWeight'

// Modules
module storage 'modules/storage.bicep' = { ... }
module appInsights 'modules/app-insights.bicep' = { ... }
module appService 'modules/app-service.bicep' = { ... }

// Outputs
output webAppUrl string = appService.outputs.webAppUrl
output storageAccountName string = storage.outputs.name
```

### Key Modules

| Module | Purpose |
|--------|---------|
| `storage.bicep` | Azure Storage Account (Table + Blob) |
| `app-insights.bicep` | Application Insights for observability |
| `app-service.bicep` | App Service Plan + Web App |
| `budget.bicep` | Cost alerts (optional) |

### Deployment Command
```bash
# Create resource group
az group create -n PoNovaWeight-rg -l eastus

# Deploy Bicep
az deployment group create \
  -g PoNovaWeight-rg \
  -f infra/main.bicep \
  --parameters googleClientId=xxx googleClientSecret=yyy
```

---

## Application Insights Instrumentation

PoNovaWeight uses **OpenTelemetry** for distributed tracing and **Serilog** for structured logging.

### Telemetry Configuration (Program.cs)

```csharp
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Configure OpenTelemetry
builder.Services.ConfigureOpenTelemetryTracing(builder.Configuration);

// Add Serilog
builder.Host.UseSerilog((context, services, config) => 
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .WriteTo.Console()
          .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
);
```

### Key Traces & Metrics
- **HTTP/API calls**: Request/response times, status codes
- **Database operations**: Table Storage CRUD latency
- **GPT-4o calls**: Image upload, analysis latency
- **Auth flows**: OAuth round-trip time
- **Exceptions**: Full stack traces, context

### Viewing Telemetry
1. **Azure Portal** → Application Insights → Performance, Logs
2. **KQL Queries**: `requests | where timestamp > ago(1h)`
3. **Analytics Workbooks**: Pre-built dashboards for API performance, error rates

---

## Monitoring & Alerts

### Key Alerts

| Alert | Threshold | Action |
|-------|-----------|--------|
| **API Error Rate** | >1% | Page on-call |
| **Response Time (p95)** | >500ms | Investigate bottleneck |
| **App Service CPU** | >80% | Scale up |
| **Table Storage Throttling** | Any throttle | Increase RUs/check queries |
| **GPT-4o Quota** | >80% used | Monitor usage |
| **Uptime** | <99.5%/month | Incident review |

### Setting Up Alerts (Azure Portal)
1. **Application Insights** → **Alerts** → **Create new alert rule**
2. Condition: `failed requests` / `response time` / `exceptions`
3. Action group: Email, SMS, or webhook
4. Save and test

---

## Rollback & Disaster Recovery

### Blue-Green Deployment
```bash
# Deploy new version to staging slot
az webapp deployment slot create -g MyResourceGroup \
  -n MyApp --slot staging

# Swap slots (instant rollback possible)
az webapp deployment slot swap -g MyResourceGroup \
  -n MyApp --slot staging

# If issues, swap back
az webapp deployment slot swap -g MyResourceGroup \
  -n MyApp --slot staging
```

### Database Backup (Table Storage)
Table Storage is geo-redundant by default (RA-GRS). For additional safety:
1. Use Azure Backup to schedule snapshots
2. Export critical data monthly to blob storage

### Secrets Rotation
1. Generate new Google OAuth credentials
2. Update Azure Key Vault secret
3. App Service reads on next restart
4. Restart app with zero downtime (slots)

---

## Cost Optimization

### Current Azure Spend Estimate

| Service | Cost | Usage |
|---------|------|-------|
| App Service (B1) | $10/month | Shared compute |
| Table Storage | ~$1–2/month | 1–10GB per user/year |
| Blob Storage (meal photos) | ~$0.50/month | 100KB avg per user |
| Application Insights | Free | <100GB/month ingestion |
| Azure OpenAI (GPT-4o) | ~$2–5/month | 10–20 scans/day |
| **Total** | **<$5/month** | MVP scale |

### Cost Savings Tips
- Use **Azure App Service Free/B1 tier** for MVP
- Enable **Table Storage lifecycle** to archive old logs
- Use **client-side caching** to reduce API calls
- Monitor **OpenAI usage** to cap meal scans

---

## Checklists

### Pre-Deployment
- [ ] All tests pass locally (`dotnet test`)
- [ ] Docker builds without errors
- [ ] secrets are in Key Vault, not in code
- [ ] API response time <500ms (load test)
- [ ] Error rate <0.1% (synthetic monitoring)

### Post-Deployment
- [ ] App is accessible at `https://ponova.azurewebsites.net`
- [ ] Health check endpoint returns 200 OK
- [ ] Telemetry flowing to App Insights
- [ ] OAuth login works (test with test user)
- [ ] Meal scan completes in <5s

### Monthly Review
- [ ] Check Azure bill for unexpected charges
- [ ] Review error logs in App Insights
- [ ] Test alert thresholds
- [ ] Rotate secrets if needed
- [ ] Confirm backup/snapshot completed

---

## References

- [Azure App Service Documentation](https://learn.microsoft.com/en-us/azure/app-service/)
- [Bicep Language Guide](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Application Insights for ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)
- [Serilog Configuration](https://github.com/serilog/serilog-settings-configuration)
- [Docker for .NET](https://learn.microsoft.com/en-us/dotnet/devops/docker/)
