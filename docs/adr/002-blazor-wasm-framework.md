# ADR 002: Choose Blazor WebAssembly over React/Vue/Angular

## Status
**Accepted** | Date: February 2026

## Context
We needed to choose a frontend framework for building the PWA. The options considered were:
- Blazor WebAssembly (Microsoft)
- React with TypeScript
- Vue.js
- Angular

## Decision
We chose **Blazor WebAssembly** for the following reasons:

### 1. Full-Stack .NET
- Single language (C#) throughout the codebase
- Share DTOs and validation between frontend and backend
- Consistent development experience

### 2. Component Model
- Razor components are similar to React/Angular
- Easy to create reusable UI components
- Strongly-typed component parameters

### 3. PWA Support
- Native PWA support in .NET
- Service Worker integration out of the box
- Offline capability support

### 4. Integration
- Native integration with .NET backend
- MediatR can be used consistently
- Easy to debug with Visual Studio

## Consequences

### Positive
- Single language team (C# only)
- Shared code between client and server
- Strong IDE support (VS, VS Code)

### Negative
- Larger initial download size (~2-3MB)
- Fewer third-party components than React
- Smaller community compared to React

## Alternatives Considered

### React with TypeScript
- **Rejected**: Would require learning JavaScript/TypeScript ecosystem
- More boilerplate for API calls
- Would need separate validation libraries

### Vue.js
- **Rejected**: Smaller ecosystem than React
- Less corporate backing

### Angular
- **Rejected**: Too opinionated, steep learning curve
- TypeScript-only approach limits flexibility

## Implementation Notes
- Use Tailwind CSS for styling (not Blazor-specific CSS)
- Create custom components for complex UI
- Use HttpClient for API calls
