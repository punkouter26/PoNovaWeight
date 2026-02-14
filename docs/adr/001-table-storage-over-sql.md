# ADR 001: Use Azure Table Storage over SQL Database

## Status
**Accepted** | Date: February 2026

## Context
We needed to choose a database solution for storing daily food logs and user data. The options considered were:
- Azure SQL Database (relational)
- Azure Table Storage (NoSQL key-value)
- Cosmos DB (NoSQL document)

## Decision
We chose **Azure Table Storage** for the following reasons:

### 1. Cost Efficiency
- Table Storage is significantly cheaper than SQL Database for our use case
- No need for expensive reserved capacity for a small user base
- Pay-per-query model fits our startup phase

### 2. Simplicity
- Schema-less design allows flexibility for adding new fields
- No migration scripts needed for schema changes
- Simple key-value access pattern matches our data model perfectly

### 3. Partition Strategy
- UserId as PartitionKey enables efficient per-user queries
- Date as RowKey enables time-series lookups
- Perfect fit for our "one partition per user" access pattern

### 4. Integration
- Native .NET SDK with excellent async support
- Azure Functions compatibility for future serverless triggers
- Easy backup with AzCopy

## Consequences

### Positive
- Lower hosting costs (~$10/month vs $50+/month for SQL)
- Faster development (no migrations, no EF complexity)
- Easier local development with Azurite

### Negative
- No complex queries (no JOINs, aggregations must be done in code)
- Limited indexing (only PartitionKey and RowKey)
- Must handle eventual consistency in code

## Alternatives Considered

### Azure SQL Database
- **Rejected**: Overkill for simple key-value storage
- Would require Entity Framework setup and migrations
- Higher cost without clear benefit

### Cosmos DB
- **Rejected**: More expensive than Table Storage
- Unnecessary features (multi-region, global distribution)
- SDK complexity not needed for our use case

## Implementation Notes
- Use `TableClient` from Azure.Data.Tables SDK
- Implement repository pattern for data access
- Use strongly-typed models mapped to TableEntity
