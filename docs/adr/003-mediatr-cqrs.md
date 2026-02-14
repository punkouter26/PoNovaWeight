# ADR 003: Use MediatR for CQRS Pattern

## Status
**Accepted** | Date: February 2026

## Context
We needed to implement request/command handling in the API. The options considered were:
- MediatR (CQRS/Mediator pattern)
- Direct controller methods
- Minimal API with inline handlers

## Decision
We chose **MediatR** for the following reasons:

### 1. Separation of Concerns
- Clean separation between HTTP layer and business logic
- Commands and Queries are handled separately
- Easy to add cross-cutting concerns (validation, logging)

### 2. Pipeline Behaviors
- FluentValidation integration via behaviors
- Consistent error handling across all endpoints
- Easy to add logging, metrics, caching

### 3. Testability
- Easy to unit test handlers in isolation
- Mock dependencies directly in tests
- No HTTP controller complexity

### 4. Scalability
- Easy to add new commands/queries without modifying existing code
- Follows Open/Closed principle

## Consequences

### Positive
- Clean, testable code
- Consistent request handling
- Easy to add validation
- Good IDE support (Go to Definition)

### Negative
- Additional abstraction layer
- Slight learning curve for team
- More files to create per feature

## Implementation Notes
- Register MediatR in Program.cs
- Create behaviors for ValidationBehavior
- Use IRequestHandler for commands and queries
- FluentValidation validators in Shared project
