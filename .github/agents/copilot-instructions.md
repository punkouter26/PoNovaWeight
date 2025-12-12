# PoNovaWeight Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-10

## Active Technologies
- C# 13 / .NET 10 + `Microsoft.AspNetCore.Authentication.Google`, `Azure.Data.Tables` (002-google-auth)
- Azure Table Storage (Azurite for local dev) — Users table with email as PartitionKey (002-google-auth)
- C# 14 / .NET 10 + MediatR, FluentValidation, Azure.Data.Tables, Blazor WASM (003-omad-weight-tracking)
- Azure Table Storage (via Azurite for local dev) (003-omad-weight-tracking)

- C# 13 / .NET 10 (per Constitution; user spec mentioned .NET 8—upgraded) + Blazor WASM, Minimal APIs, MediatR, FluentValidation, Serilog, Azure.Data.Tables, Azure.AI.OpenAI (001-food-journal-mvp)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for C# 13 / .NET 10 (per Constitution; user spec mentioned .NET 8—upgraded)

## Code Style

C# 13 / .NET 10 (per Constitution; user spec mentioned .NET 8—upgraded): Follow standard conventions

## Recent Changes
- 003-omad-weight-tracking: Added C# 14 / .NET 10 + MediatR, FluentValidation, Azure.Data.Tables, Blazor WASM
- 002-google-auth: Added C# 13 / .NET 10 + `Microsoft.AspNetCore.Authentication.Google`, `Azure.Data.Tables`

- 001-food-journal-mvp: Added C# 13 / .NET 10 (per Constitution; user spec mentioned .NET 8—upgraded) + Blazor WASM, Minimal APIs, MediatR, FluentValidation, Serilog, Azure.Data.Tables, Azure.AI.OpenAI

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
