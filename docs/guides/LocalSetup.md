# Local Setup Guide: PoNovaWeight

A complete Day 1 guide for getting PoNovaWeight running locally with Docker Compose, enabling AI meal scanning and full feature testing.

---

## Prerequisites

Ensure you have installed:
- **Git** (for cloning the repo)
- **Docker Desktop** (includes Docker Compose)
- **.NET 10 SDK** (for local `dotnet` commands, optional if using Docker)
- **Node.js 18+** (for Tailwind CSS build)
- **VS Code** or **Visual Studio** (optional IDE)

### Verify Installations
```bash
git --version
docker --version
docker compose --version
dotnet --version
node --version
npm --version
```

---

## Quick Start (5 Minutes)

### 1. Clone Repository
```bash
git clone https://github.com/YOUR_ORG/PoNovaWeight.git
cd PoNovaWeight
```

### 2. Start Services with Docker Compose
```bash
docker compose up -d
```

This starts:
- **PoNovaWeight API** on `http://localhost:5000`
- **Azurite** (local Azure Storage emulator) on port 10000–10002

### 3. Access the App
Open your browser and navigate to:
```
http://localhost:5000
```

### 4. Login with Dev Credentials
Use the test user endpoint for demo data:
```
Email: test-user@local
Password: (any value, not checked in dev)
```

Or click "Dev Login" if available on the Login page.

---

## Local Setup with Docker Compose

### Docker Compose File (`docker-compose.yml`)

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: src/PoNovaWeight.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      AzureStorage__ConnectionString: "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;"
      Google__ClientId: "LOCAL_DEV"
      Google__ClientSecret: "LOCAL_DEV"
      Azure__OpenAI__Endpoint: "http://localhost:8000"
      Azure__OpenAI__Key: "LOCAL_DEV"
      Azure__OpenAI__DeploymentName: "gpt-4o"
    depends_on:
      - azurite
    volumes:
      - ./src:/app/src
    networks:
      - ponova-network

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite:latest
    ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"
    environment:
      AZURITE_ACCOUNTS: "devstoreaccount1:Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
    volumes:
      - azurite-data:/data
    networks:
      - ponova-network

volumes:
  azurite-data:

networks:
  ponova-network:
    driver: bridge
```

### Start Services
```bash
# Start in background
docker compose up -d

# View logs
docker compose logs -f api

# Stop services
docker compose down

# Remove all data (reset Azurite)
docker compose down -v
```

---

## Local Setup WITHOUT Docker (Native .NET)

### 1. Install Azurite Locally
```bash
npm install -g azurite

# Start Azurite in a terminal (keeps running)
azurite --silent --location ./azurite --debug ./azurite/debug.log
```

### 2. Install .NET Dependencies
```bash
cd PoNovaWeight
dotnet restore
```

### 3. Build Tailwind CSS
```bash
cd src/PoNovaWeight.Client
npm install
npm run build  # or 'npm run watch' for development
cd ../..
```

### 4. Run the API
```bash
dotnet run --project src/PoNovaWeight.Api
```

The API will start on `http://localhost:5000`.

---

## Configuration for Local Development

### User Secrets (Avoids Hardcoding Secrets)

Store local secrets securely without committing to git:

```bash
# Initialize user secrets (one-time)
dotnet user-secrets init --project src/PoNovaWeight.Api

# Set dev-only credentials
dotnet user-secrets set "Google:ClientId" "local-dev-id" \
  --project src/PoNovaWeight.Api
dotnet user-secrets set "Google:ClientSecret" "local-dev-secret" \
  --project src/PoNovaWeight.Api
dotnet user-secrets set "Azure:OpenAI:Endpoint" "http://localhost:8000" \
  --project src/PoNovaWeight.Api
dotnet user-secrets set "Azure:OpenAI:Key" "local-dev-key" \
  --project src/PoNovaWeight.Api
```

### appsettings.Development.json

Create or update `src/PoNovaWeight.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.txt",
          "rollingInterval": "Day"
        }
      }
    ],
    "Enrich": ["FromLogContext"]
  },
  "AspNetCore": {
    "Urls": "http://localhost:5000"
  },
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;"
  },
  "Google": {
    "ClientId": "local-dev",
    "ClientSecret": "local-dev"
  },
  "Azure": {
    "OpenAI": {
      "Endpoint": "http://localhost:8000",
      "Key": "local-dev",
      "DeploymentName": "gpt-4o"
    }
  }
}
```

---

## Testing the Setup

### 1. Health Check
```bash
curl http://localhost:5000/health
# Expected: 200 OK
```

### 2. Authentication Status
```bash
curl http://localhost:5000/api/auth/status
# Expected: {"isAuthenticated": false, "user": null}
```

### 3. Dev Test User Login (Demo Data)
```bash
curl -X POST http://localhost:5000/api/auth/dev-test-user-login
# Expected: JWT token + user info
```

### 4. Get Today's Log
```bash
AUTHORIZATION="Bearer YOUR_JWT_TOKEN_FROM_STEP_3"
curl -H "Authorization: $AUTHORIZATION" \
     http://localhost:5000/api/daily-logs/$(date +%Y-%m-%d)
# Expected: Daily log data or empty if none exists
```

### 5. Open UI in Browser
```bash
http://localhost:5000
```

You should see the Login page. Click "Dev Login" or use test-user credentials.

---

## Troubleshooting

### Port Already in Use
If port 5000 is already in use:
```bash
# On Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# On macOS/Linux
lsof -i :5000
kill -9 <PID>
```

Then retry `docker compose up` or `dotnet run`.

### Azurite Connection Error
If you see `Unable to connect to Azurite`:
1. Verify Azurite is running: `docker ps | grep azurite`
2. Check connection string in `appsettings.Development.json`
3. Restart Azurite: `docker compose down && docker compose up -d azurite`

### NPM Build Error
If Tailwind CSS build fails:
```bash
cd src/PoNovaWeight.Client
rm -rf node_modules package-lock.json
npm install
npm run build
```

### Docker Image Build Failure
Examine the error and check:
1. Dockerfile syntax in `src/PoNovaWeight.Api/Dockerfile`
2. Base image availability (`mcr.microsoft.com/dotnet/aspnet:10`)
3. Network connectivity for package restoration

### Clear Everything & Restart Fresh
```bash
# Remove all containers, volumes, networks
docker compose down -v

# Rebuild from scratch
docker compose build --no-cache
docker compose up -d

# Watch logs
docker compose logs -f api
```

---

## Development Workflow

### 1. Start Local Services
```bash
docker compose up -d
```

### 2. Run Tests
```bash
# All tests
dotnet test

# Specific test file
dotnet test tests/PoNovaWeight.Api.Tests/Features/DailyLogs/GetDailyLogTests.cs

# With coverage
dotnet test /p:CollectCoverage=true
```

### 3. Watch Mode (Hot Reload)
Terminal 1 – API:
```bash
cd src/PoNovaWeight.Api
dotnet watch run
```

Terminal 2 – Tailwind CSS:
```bash
cd src/PoNovaWeight.Client
npm run watch
```

Terminal 3 – E2E Tests (Playwright):
```bash
cd tests/e2e
npm ci
npm run test:dev  # Only runs marked tests
```

### 4. Make Code Changes
- Edit `*.cs` or `*.razor` files
- `dotnet watch` automatically recompiles
- Refresh browser to see changes

### 5. Check Code Quality
```bash
# Run linter (if configured)
dotnet format --verify-no-changes

# Fix formatting
dotnet format
```

### 6. Commit & Push
```bash
git add .
git commit -m "feat: add daily log persistence"
git push origin main
```

---

## Using VS Code Tasks

VS Code is pre-configured with tasks in `.vscode/tasks.json`:

- **Ctrl+Shift+B**: Build solution
- **Ctrl+Shift+T**: Run tests
- **Kill dotnet + Build API**: Stops running process and rebuilds

---

## Database Management (Azurite Table Storage)

### View Tables in Azurite
```bash
# Using Azure Storage Explorer
# 1. Install: https://azure.microsoft.com/en-us/products/storage/storage-explorer/
# 2. Add connection: "Azurite (Default Ports)" at localhost:10002
# 3. Browse tables: DailyLog, UserSettings, etc.
```

### Reset Azurite Data
```bash
docker compose down -v
docker compose up -d azurite
```

---

## Advanced: Mock GPT-4o Locally

If you don't have Azure OpenAI access, use a mock server:

### Option 1: Mock HTTP Server
Create `localmocks/gpt-mock.js`:
```javascript
const http = require('http');

const server = http.createServer((req, res) => {
  if (req.url === '/deployments/gpt-4o/chat/completions') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      choices: [{
        message: {
          content: '{"proteins": 2, "vegetables": 3, "fruits": 1, "starches": 1, "fats": 0, "dairy": 0}'
        }
      }]
    }));
  }
});

server.listen(8000, () => console.log('Mock GPT-4o on :8000'));
```

Run it:
```bash
node localmocks/gpt-mock.js
```

Update `appsettings.Development.json`:
```json
"Azure": {
  "OpenAI": {
    "Endpoint": "http://localhost:8000",
    "Key": "mock"
  }
}
```

### Option 2: Mock in Tests
Use a mock client in unit/integration tests:
```csharp
var mockOpenAiClient = new Mock<IOpenAiClient>();
mockOpenAiClient
    .Setup(x => x.AnalyzeImageAsync(It.IsAny<Stream>()))
    .ReturnsAsync(new MealAnalysisResult { /* ... */ });
```

---

## Useful Commands Reference

```bash
# Build & Test
dotnet build
dotnet test

# Run locally
dotnet run -p src/PoNovaWeight.Api
dotnet watch run -p src/PoNovaWeight.Api

# Docker
docker compose up -d
docker compose logs -f api
docker compose down

# Database
azurite --silent --location ./azurite
curl -H "Authorization: Bearer TOKEN" http://localhost:5000/api/daily-logs

# Tailwind
npm run build --prefix src/PoNovaWeight.Client
npm run watch --prefix src/PoNovaWeight.Client

# Cleanup & Fresh Start
docker compose down -v && docker compose up -d
```

---

## Next Steps

1. **Explore the Code**: Start in `src/PoNovaWeight.Api/Program.cs`
2. **Read ADRs**: Check `docs/adr/` for architecture decisions
3. **Run Tests**: `dotnet test` to understand the codebase
4. **Make a Change**: Add a feature or fix a bug, then test locally
5. **Create a PR**: Push to a branch and open a pull request

---

## Documentation References

- [ProductSpec.md](ProductSpec.md) – Business requirements & features
- [DevOps.md](DevOps.md) – Deployment & environment config
- [Architecture.mmd](Architecture.mmd) – System topology
- [DataModel.mmd](DataModel.mmd) – Database schema

Happy coding! 🚀
