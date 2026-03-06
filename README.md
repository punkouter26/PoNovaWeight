# PoNovaWeight

A personal food journaling Progressive Web App (PWA) for tracking daily nutritional intake using the Nova Physician Wellness Center's unit-based OMAD system.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Azure](https://img.shields.io/badge/Azure-App%20Service-0078D4?logo=microsoftazure)](https://azure.microsoft.com/)

## Quick Start

```bash
# Clone and restore
git clone <repository-url>
cd PoNovaWeight
dotnet restore

# Start Azurite for local Table Storage
azurite --silent --location ./azurite --debug ./azurite/debug.log

# Run the API directly
dotnet run --project src/PoNovaWeight.Api
```

**Access Points:**
- 🌐 **App**: http://localhost:5000

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
| Hosting | Azure App Service |

## Project Structure

```
src/
├── PoNovaWeight.Api/          # ASP.NET Core API
├── PoNovaWeight.Client/       # Blazor WASM frontend
└── PoNovaWeight.Shared/       # DTOs & validation
```

## Documentation

### Architecture & Visualization (Mermaid Diagrams)
- [Architecture Overview](docs/Architecture.mmd) - System context & container architecture
- [Application Flow](docs/ApplicationFlow.mmd) - Auth flow & user journey
- [Data Model](docs/DataModel.mmd) - Database schema & entity lifecycles
- [Component Map](docs/ComponentMap.mmd) - Component tree & service dependencies
- [Data Pipeline](docs/DataPipeline.mmd) - Data workflow & CRUD operations

### Technical Documentation
- [Product Specification](docs/specifications/ProductSpec.md) - PRD & success metrics
- [API Contract](docs/ApiContract.md) - API specs & error handling
- [DevOps Guide](docs/guides/DevOps.md) - Deployment pipeline & environment secrets
- [Local Setup](docs/guides/LocalSetup.md) - Day 1 guide for direct API and Docker workflows

### Architecture Decision Records (ADRs)
- [001: Table Storage over SQL](docs/adr/001-table-storage-over-sql.md)
- [002: Blazor WASM Framework](docs/adr/002-blazor-wasm-framework.md)
- [003: MediatR CQRS](docs/adr/003-mediatr-cqrs.md)
- [004: Dev Auth Local](docs/adr/004-dev-auth-local.md)

## Testing

```bash
# Run all tests
dotnet test

# Run excluding Docker tests
dotnet test --filter "Category!=Docker"
```

## License

MIT
# Trigger CI
