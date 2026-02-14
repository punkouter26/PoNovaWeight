# ProjectManifest.md - Inventory of Documentation Assets

## PoNovaWeight Documentation Inventory

This document provides an inventory of all documentation assets in the /docs folder, designed for AI agents to parse and understand the project structure.

---

## Documentation File Index

| File | Type | Purpose | AI-Friendly |
|------|------|---------|-------------|
| [Architecture.mmd](Architecture.mmd) | Mermaid Diagram | System context & container architecture | ✅ |
| [ApplicationFlow.mmd](ApplicationFlow.mmd) | Mermaid Diagram | Auth flow & user journey | ✅ |
| [DataModel.mmd](DataModel.mmd) | Mermaid Diagram | Database schema & entity lifecycles | ✅ |
| [ComponentMap.mmd](ComponentMap.mmd) | Mermaid Diagram | Component tree & service dependencies | ✅ |
| [DataPipeline.mmd](DataPipeline.mmd) | Mermaid Diagram | Data workflow & CRUD operations | ✅ |
| [ProductSpec.md](ProductSpec.md) | Markdown | PRD & success metrics | ✅ |
| [ApiContract.md](ApiContract.md) | Markdown | API specs & error handling | ✅ |
| [DevOps.md](DevOps.md) | Markdown | Deployment pipeline & secrets | ✅ |
| [LocalSetup.md](LocalSetup.md) | Markdown | Day 1 guide & Docker | ✅ |
| [ProjectManifest.md](ProjectManifest.md) | Markdown | Documentation inventory | ✅ |
| [ImprovementSuggestions.md](ImprovementSuggestions.md) | Markdown | Top 5 improvement recommendations | ✅ |
| [InteractiveApiDocs.md](InteractiveApiDocs.md) | Markdown | Interactive API documentation guide | ✅ |
| [SequenceDiagrams.md](SequenceDiagrams.md) | Markdown | Detailed sequence diagrams | ✅ |
| [CICDPipeline.md](CICDPipeline.md) | Markdown | CI/CD pipeline visualization | ✅ |
| [adr/001-table-storage-over-sql.md](adr/001-table-storage-over-sql.md) | ADR | Database decision | ✅ |
| [adr/002-blazor-wasm-framework.md](adr/002-blazor-wasm-framework.md) | ADR | Frontend framework decision | ✅ |
| [adr/003-mediatr-cqrs.md](adr/003-mediatr-cqrs.md) | ADR | MediatR decision | ✅ |
| [adr/004-dev-auth-local.md](adr/004-dev-auth-local.md) | ADR | DevAuth decision | ✅ |
| [README.md](../README.md) | Markdown | Project overview & quick links | ✅ |

---

## Quick Reference for AI Agents

### Project Summary
- **Name**: PoNovaWeight
- **Type**: Personal Food Journaling PWA
- **Framework**: Blazor WebAssembly + ASP.NET Core API
- **Database**: Azure Table Storage
- **Auth**: Google OAuth 2.0

### Key Commands

```bash
# Build
dotnet build

# Run locally
dotnet run --project src/PoNovaWeight.AppHost

# Test
dotnet test
```

### Environment Variables Needed

| Variable | Required | Description |
|----------|----------|-------------|
| GoogleAuth:ClientId | Yes | Google OAuth Client ID |
| GoogleAuth:ClientSecret | Yes | Google OAuth Client Secret |
| AzureStorage:ConnectionString | Yes | Table Storage connection |
| AzureOpenAI:Endpoint | No | OpenAI endpoint |
| AzureOpenAI:ApiKey | No | OpenAI API key |

### API Base URL
- Local: http://localhost:5000
- Production: https://ponovaweight-api.{region}.azurecontainerapps.io

### Key Endpoints

| Endpoint | Method | Description |
|---------|--------|-------------|
| /auth/login | GET | Start OAuth flow |
| /api/dailylogs/{date} | GET/PUT | Daily log CRUD |
| /api/dailylogs/increment | POST | Add food unit |
| /api/weeklysummary/{date} | GET | Weekly summary |
| /api/mealscan | POST | AI meal scan |
| /health | GET | Health check |

### Data Model

**DailyLogDto**:
- Date, Proteins, Vegetables, Fruits, Starches, Fats, Dairy
- WaterSegments, Weight, OmadCompliant, AlcoholConsumed

**Table Storage**:
- PartitionKey: UserId (email)
- RowKey: Date (yyyy-MM-dd)

---

## File Descriptions

### Architecture.mmd
System context diagram showing:
- External systems (Google OAuth, Azure OpenAI)
- Azure cloud resources (Container Apps, Table Storage, Key Vault, App Insights)
- Technology stack overview
- Container architecture details

### ApplicationFlow.mmd
User journey and authentication flow:
- Google OAuth 2.0 sequence diagram
- Main navigation flow
- Security architecture (cookies, data protection)
- Session management

### DataModel.mmd
Data structures and state:
- ER diagram for User and DailyLog entities
- Azure Table Storage schema
- Unit category definitions
- Entity state transitions

### ComponentMap.mmd
Frontend and backend architecture:
- Blazor component tree (pages, components)
- Client service dependencies
- API feature modules
- Repository layer

### DataPipeline.mmd
Data flow and processing:
- CRUD operation pipelines
- Meal scanning AI flow
- Weekly summary aggregation
- Weight trends analytics

### ProductSpec.md
Business requirements:
- Problem statement and target users
- Core features (6 unit categories, water, OMAD, weight, AI)
- User journeys
- Success metrics and roadmap

### ApiContract.md
Technical API specification:
- All REST endpoints with parameters
- Request/response examples
- Error handling (RFC 7807)
- HTTP status codes

### DevOps.md
Infrastructure and deployment:
- Azure resource overview
- CI/CD pipeline (GitHub Actions)
- Environment configuration
- Secrets management (Key Vault)
- Health checks

### LocalSetup.md
Developer onboarding:
- Prerequisites (.NET 10, Docker, Azure CLI)
- Quick start guide
- User secrets configuration
- Docker compose setup
- Troubleshooting guide

---

## For AI Context Windows

When processing this project, AI agents should:

1. **Start here** (ProjectManifest.md) for overview
2. **Check Architecture.mmd** for system context
3. **Use ApiContract.md** for API details
4. **Reference ProductSpec.md** for business logic

### Code Locations

| Component | Location |
|-----------|----------|
| API Endpoints | `src/PoNovaWeight.Api/Features/` |
| Blazor Pages | `src/PoNovaWeight.Client/Pages/` |
| Components | `src/PoNovaWeight.Client/Components/` |
| DTOs | `src/PoNovaWeight.Shared/DTOs/` |
| Tests | `tests/PoNovaWeight.Api.Tests/` |
| Infrastructure | `src/PoNovaWeight.Api/Infrastructure/` |
| Azure Bicep | `infra/main.bicep` |

### Version Information

| Component | Version |
|-----------|---------|
| .NET | 10.0 |
| Blazor | WebAssembly |
| Tailwind CSS | 3.4 |
| Azure Table Storage SDK | Latest |
| MediatR | Latest |
| FluentValidation | Latest |

---

## Last Updated

This manifest was generated as part of the Po5Docs documentation initiative.

**Project**: PoNovaWeight  
**Repository**: https://github.com/punkouter26/PoNovaWeight  
**Documentation Date**: February 2026
