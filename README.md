# PoNovaWeight

A personal food journaling Progressive Web App (PWA) for tracking daily nutritional intake using the Nova Physician Wellness Center's unit-based OMAD system.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Azure](https://img.shields.io/badge/Azure-Container%20Apps-0078D4?logo=microsoftazure)](https://azure.microsoft.com/)

## Quick Start

```bash
# Clone and restore
git clone <repository-url>
cd PoNovaWeight
dotnet restore

# Run with Aspire (starts Azurite + API + Dashboard)
dotnet run --project src/PoNovaWeight.AppHost
```

**Access Points:**
- 🌐 **App**: http://localhost:5000
- 📊 **Aspire Dashboard**: https://localhost:15888

## Features

- **Unit-Based Tracking**: 6 food categories (Proteins, Vegetables, Fruits, Starches, Fats, Dairy)
- **Water Tracking**: Visual 8-segment hydration tracker
- **OMAD Streaks**: Track One Meal A Day compliance with streak counters
- **Weight Trends**: 30-day weight visualization with alcohol correlation analysis
- **AI Meal Scanning**: Take photos and let GPT-4o suggest unit breakdowns
- **PWA**: Installable to home screen with offline support

## Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | Blazor WebAssembly, Tailwind CSS 3.4 |
| Backend | ASP.NET Core Minimal API, MediatR |
| Database | Azure Table Storage |
| AI | Azure OpenAI GPT-4o |
| Orchestration | .NET Aspire |
| Hosting | Azure Container Apps |

## Project Structure

```
src/
├── PoNovaWeight.AppHost/      # Aspire orchestrator
├── PoNovaWeight.Api/          # ASP.NET Core API
├── PoNovaWeight.Client/       # Blazor WASM frontend
├── PoNovaWeight.Shared/       # DTOs & validation
└── PoNovaWeight.ServiceDefaults/  # OpenTelemetry config
```

## Documentation

- [Architecture Overview](docs/architecture.md)
- [Getting Started](docs/README.md)
- [ADR: Table Storage Decision](docs/adr/001-table-storage-over-sql.md)
- [C4 Diagrams](docs/mermaid/)

## Testing

```bash
# Run all tests
dotnet test

# Run excluding Docker tests
dotnet test --filter "Category!=Docker"
```

## License

MIT
