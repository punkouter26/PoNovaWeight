# Nova Food Journal

A personal food journaling Progressive Web App (PWA) designed for tracking daily nutritional intake using the Nova Physician Wellness Center's unit-based system.

## Features

### Core Functionality
- **Unit-Based Tracking**: Log daily intake across 6 food categories (Proteins, Vegetables, Fruits, Starches, Fats, Dairy) using a simple unit system
- **Water Tracking**: Visual 8-segment water tracker for daily hydration monitoring
- **Weekly Dashboard**: At-a-glance view of weekly progress with color-coded status indicators
- **AI Meal Scanning**: Take a photo of your meal and let AI suggest unit breakdowns

### Technical Features
- **Progressive Web App**: Installable to home screen with offline support
- **Mobile-First Design**: Optimized for smartphone use with touch-friendly controls
- **Google OAuth Authentication**: Secure sign-in with Google account

## Technology Stack

| Component | Technology |
|-----------|------------|
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core Minimal API |
| Database | Azure Table Storage |
| AI | Azure OpenAI GPT-4o |
| Hosting | Azure Container Apps (ACA) |
| Runtime | .NET 10 |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (for deployment)
- [Node.js](https://nodejs.org/) (for Tailwind CSS build)

## Quick Start

### 1. Clone and Install Dependencies

```bash
git clone <repository-url>
cd PoNovaWeight
dotnet restore
```

### 2. Start Local Storage Emulator

```bash
# Install Azurite globally
npm install -g azurite

# Start Azurite
azurite --silent --location ./azurite-data
```

### 3. Run the Application

```bash
cd src/PoNovaWeight.Api
dotnet run
```

The application will be available at:
- **Web App**: https://localhost:5001
- **API**: https://localhost:5001/api
- **Swagger**: https://localhost:5001/swagger

### 4. Authentication

The application uses Google OAuth for authentication. Configure credentials in `appsettings.Development.json` or via environment variables (see Configuration below).

## Configuration

### Local Development (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "AzureStorage": "UseDevelopmentStorage=true"
  },
  "Google": {
    "ClientId": "<your-google-client-id>",
    "ClientSecret": "<your-google-client-secret>"
  },
  "OpenAI": {
    "Endpoint": "<your-azure-openai-endpoint>",
    "ApiKey": "<your-azure-openai-key>",
    "DeploymentName": "gpt-4o",
    "UseStub": true
  }
}
```

### Production Configuration

Set the following environment variables or app settings:

| Setting | Description |
|---------|-------------|
| `ConnectionStrings__AzureStorage` | Azure Storage connection string |
| `Google__ClientId` | Google OAuth client ID |
| `Google__ClientSecret` | Google OAuth client secret |
| `AzureOpenAI__Endpoint` | Azure OpenAI endpoint URL |
| `AzureOpenAI__ApiKey` | Azure OpenAI API key |
| `AzureOpenAI__DeploymentName` | GPT-4o deployment name |

## Project Structure

```
PoNovaWeight/
├── src/
│   ├── PoNovaWeight.Api/           # ASP.NET Core API
│   │   ├── Features/               # MediatR handlers by feature
│   │   │   ├── Auth/               # Google OAuth authentication
│   │   │   ├── DailyLogs/          # Daily log CRUD operations
│   │   │   ├── MealScan/           # AI meal analysis
│   │   │   └── WeeklySummary/      # Weekly aggregation
│   │   └── Infrastructure/         # Repository, middleware
│   ├── PoNovaWeight.Client/        # Blazor WASM frontend
│   │   ├── Components/             # Reusable UI components
│   │   ├── Pages/                  # Route-able pages
│   │   └── Services/               # API client, session service
│   └── PoNovaWeight.Shared/        # Shared DTOs and contracts
├── tests/
│   ├── PoNovaWeight.Api.Tests/     # API unit & integration tests
│   └── PoNovaWeight.Client.Tests/  # Blazor component tests (bUnit)
├── docs/                           # Documentation
├── infra/                          # Azure Bicep IaC
└── scripts/                        # Development scripts
```

## Testing

### Run All Tests

```bash
dotnet test
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

- **Unit Tests**: Fast, isolated tests for handlers and components
- **Integration Tests**: Tests requiring Azurite (skipped if unavailable)
- **E2E Tests**: Playwright tests for full user flows (optional)

## Deployment

### Using Azure Developer CLI

```bash
azd auth login
azd up
```

### Manual Deployment

1. Create Azure resources using Bicep templates in `/infra`
2. Build the application: `dotnet publish -c Release`
3. Deploy to Azure Container Apps (ACA) using `azd deploy` or `azd up` (recommended).

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check |
| GET | `/api/daily-logs/{date}` | Get daily log for date |
| PUT | `/api/daily-logs/{date}` | Save daily log |
| PUT | `/api/daily-logs/{date}/units/{category}` | Update specific category |
| PUT | `/api/daily-logs/{date}/water` | Update water segments |
| GET | `/api/weekly-summary` | Get 7-day summary |
| POST | `/api/meal-scan` | Analyze meal photo with AI |
| GET | `/api/auth/login` | Initiate Google OAuth sign-in |
| GET | `/api/auth/logout` | Sign out and clear session |
| GET | `/api/auth/me` | Get current user info |

## Nova Unit System

The app uses the Nova Physician Wellness Center's unit-based tracking:

| Category | Daily Target | Example Serving |
|----------|-------------|-----------------|
| Proteins | 3 units | 4 oz chicken breast |
| Vegetables | 4 units | 1 cup raw vegetables |
| Fruits | 2 units | 1 medium apple |
| Starches | 2 units | 1 slice bread |
| Fats | 2 units | 1 tbsp olive oil |
| Dairy | 2 units | 8 oz milk |

## License

Private - Nova Physician Wellness Center

## Support

For issues or questions, contact the development team.
