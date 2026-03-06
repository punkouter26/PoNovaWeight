# LocalSetup.md - Day 1 Guide & Docker Compose

## PoNovaWeight Local Setup Guide

This document provides step-by-step instructions for setting up the PoNovaWeight application locally for development.

---

## REGULAR VERSION

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0+ | Build and run .NET applications |
| Docker Desktop | Latest | Run Azurite (local storage) |
| Azure CLI | 2.50+ | Azure authentication |
| Git | Latest | Source control |
| Visual Studio Code | Latest | IDE (optional) |

### Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/punkouter26/PoNovaWeight.git
cd PoNovaWeight

# 2. Restore dependencies
dotnet restore

# 3. Start Azurite (local Table Storage)
azurite --silent --location ./azurite --debug ./azurite/debug.log

# 4. Run the API directly
dotnet run --project src/PoNovaWeight.Api
```

**After running, access:**
- 🌐 **App**: http://localhost:5000

### Detailed Setup

#### Step 1: Install Prerequisites

```bash
# Install .NET 10 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/10.0

# Install Docker Desktop
# Download from: https://www.docker.com/products/docker-desktop

# Install Azure CLI
winget install Microsoft.AzureCLI

# Verify installations
dotnet --version
docker --version
az --version
```

#### Step 2: Configure User Secrets

Create a `secrets.json` file or use the .NET CLI:

```bash
# Navigate to API project
cd src/PoNovaWeight.Api

# Set user secrets
dotnet user-secrets set "AzureStorage:ConnectionString" "UseDevelopmentStorage=true"
dotnet user-secrets set "GoogleAuth:ClientId" "your-google-client-id"
dotnet user-secrets set "GoogleAuth:ClientSecret" "your-google-client-secret"

# Optional: OpenAI (meal scanning will use mock data if not set)
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-openai.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
```

#### Step 3: Get Google OAuth Credentials

1. Go to Google Cloud Console (https://console.cloud.google.com/)
2. Create a new project or select existing
3. Navigate to **APIs & Services** > **Credentials**
4. Create **OAuth 2.0 Client IDs** (Application type: Web application)
5. Add the following **Authorized redirect URIs** (Blazor WASM uses `/authentication/login-callback`):
   - `http://localhost:5000/authentication/login-callback`
   - `https://localhost:5001/authentication/login-callback`
6. Click **Create** and copy the **Client ID** to the client's `wwwroot/appsettings.json`:
   ```json
   {
     "Google": {
       "ClientId": "YOUR_CLIENT_ID_HERE.apps.googleusercontent.com"
     }
   }
   ```

**Note**: If you previously configured `/auth/callback`, you must update the redirect URIs. Blazor WASM OIDC automatically uses `/authentication/login-callback` as the callback path.

#### Step 4: Run Azurite (Local Azure Storage)

Start Azurite before launching the API:

```bash
# Using Docker
docker run -d --name azurite \
  -p 10000:10000 \
  -p 10001:10001 \
  -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite

# Azurite endpoints:
# Table: http://localhost:10001
# Blob:  http://localhost:10000
# Queue: http://localhost:10002
```

#### Step 5: Run the Application

```bash
# Option 1: Run the API directly (recommended)
dotnet run --project src/PoNovaWeight.Api

# Option 2: Run Client separately during frontend-only work
# Terminal 1: Run API
dotnet run --project src/PoNovaWeight.Api

# Terminal 2: Run Client (requires API running)
dotnet run --project src/PoNovaWeight.Client
```

### Docker Compose Setup

Create a `docker-compose.yml` for local development:

```yaml
version: '3.8'

services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    container_name: ponovaweight-azurite
    ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"
    volumes:
      - azurite-data:/data
    networks:
      - ponova-network

  api:
    build:
      context: .
      dockerfile: src/PoNovaWeight.Api/Dockerfile
    container_name: ponovaweight-api
    ports:
      - "5000:8080"
      - "5001:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - AzureStorage__ConnectionString=UseDevelopmentStorage=true
    depends_on:
      - azurite
    networks:
      - ponova-network

volumes:
  azurite-data:

networks:
  ponova-network:
    driver: bridge
```

Run with:

```bash
docker-compose up -d
```

### Development Mode Features

| Feature | Description | How to Enable |
|---------|-------------|---------------|
| Dev Auth | Bypass Google OAuth | Set `DevAuth:Enabled` in appsettings |
| Mock AI | Mock meal scanning | Don't configure OpenAI |
| Verbose Logging | Detailed logs | Set `Serilog:MinimumLevel` to Debug |

### Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "Category=Unit"

# Run integration tests
dotnet test --filter "Category=Integration"

# Run excluding Docker tests
dotnet test --filter "Category!=Docker"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Troubleshooting

#### "Connection refused" errors
- Ensure Azurite is running: `docker ps | grep azurite`
- Check Azurite ports: `netstat -an | grep 1000`

#### "Invalid OAuth state" errors
- Clear browser cookies and try again
- Ensure redirect URIs match exactly in Google Console

#### "Table not found" errors
- Azurite creates tables automatically on first use
- Restart Azurite container if issues persist

#### Build errors
- Ensure .NET 10 SDK is installed: `dotnet --version`
- Clear NuGet cache: `dotnet nuget locals all --clear`

### Project Structure

```
PoNovaWeight/
├── src/
│   ├── PoNovaWeight.Api/         # Backend API
│   ├── PoNovaWeight.Client/      # Blazor WASM frontend
│   ├── PoNovaWeight.Shared/      # Shared DTOs
├── tests/
│   ├── PoNovaWeight.Api.Tests/   # API tests
│   ├── PoNovaWeight.Client.Tests/# Component tests
│   └── e2e/                      # E2E tests
├── infra/                        # Azure Bicep templates
├── docs/                         # Documentation
└── scripts/                     # Build scripts
```

---

## SIMPLIFIED VERSION

### 5-Minute Setup

```bash
# 1. Clone
git clone https://github.com/punkouter26/PoNovaWeight.git
cd PoNovaWeight

# 2. Restore
dotnet restore

# 3. Start Azurite
azurite --silent --location ./azurite --debug ./azurite/debug.log

# 4. Run the API
dotnet run --project src/PoNovaWeight.Api
```

### What You Need

| Tool | Get It From |
|------|-------------|
| .NET 10 | dotnet.microsoft.com |
| Docker | docker.com |
| Azure CLI | winget install Microsoft.AzureCLI |

### Get Google Credentials

1. Go to Google Cloud Console
2. Create OAuth Client ID
3. Add redirect: `http://localhost:5000/auth/callback`
4. Copy ID/Secret to user secrets

### User Secrets

```bash
cd src/PoNovaWeight.Api
dotnet user-secrets set "GoogleAuth:ClientId" "YOUR_ID"
dotnet user-secrets set "GoogleAuth:ClientSecret" "YOUR_SECRET"
```

### Access

- App: http://localhost:5000

### Test

```bash
dotnet test
```

### Troubleshooting

| Problem | Fix |
|---------|-----|
| Can't connect to storage | Start Azurite or Docker Desktop |
| OAuth error | Check redirect URIs match |
| Build fails | Install .NET 10 SDK |
