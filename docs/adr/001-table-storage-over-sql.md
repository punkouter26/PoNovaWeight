# ADR 001: Azure Table Storage Over SQL Database

## Status

Accepted

## Date

2024-12-01

## Context

The Nova Food Journal MVP requires persistent storage for daily food log entries. Each entry contains:
- User identifier (single user for MVP)
- Date (one entry per day)
- Six food category unit counts (0-10 each)
- Water segments count (0-8)

We need to choose a storage solution that is:
1. Cost-effective for MVP development and low-volume production
2. Simple to develop against with minimal setup
3. Appropriate for the data access patterns
4. Deployable to Azure with infrastructure-as-code

### Options Considered

#### Option 1: Azure SQL Database
- **Pros**: Full relational capabilities, familiar SQL syntax, Entity Framework support
- **Cons**: Higher base cost (~$5/month minimum), more complex setup, overkill for key-value access

#### Option 2: Azure Cosmos DB
- **Pros**: Global distribution, multi-model, rich query capabilities
- **Cons**: Significant cost at scale, complex pricing model, excessive for MVP scope

#### Option 3: Azure Table Storage
- **Pros**: Very low cost (~$0.01/month for low volume), simple API, key-value optimized
- **Cons**: Limited query capabilities, no joins, no transactions across partitions

#### Option 4: SQLite (local file)
- **Pros**: Zero cost, no network dependency, simple deployment
- **Cons**: No cloud persistence, difficult to scale, data loss risk on App Service restart

## Decision

We will use **Azure Table Storage** for the MVP.

## Rationale

### 1. Access Pattern Alignment

Our data access is purely key-value based:
- **Get daily log**: PartitionKey (userId) + RowKey (date) → O(1) lookup
- **Get week of logs**: PartitionKey (userId) + RowKey range → efficient range query
- **Save daily log**: Upsert by PartitionKey + RowKey

We never need:
- Joins across tables
- Complex aggregations
- Full-text search
- Transactions spanning multiple entities

### 2. Cost Efficiency

For a single-user MVP with low data volume:
- Storage: ~$0.01/month for < 1GB
- Operations: ~$0.01/month for < 10,000 transactions
- **Total: < $1/month** vs. $5+ for SQL Database

### 3. Development Simplicity

- Azure.Data.Tables SDK provides clean async API
- Azurite emulator enables local development without Azure account
- No Entity Framework migrations or schema management
- Single table with simple entity class

### 4. Scalability Path

While Table Storage has limitations, they don't apply to this use case:
- Single user → no partition hotspots
- Date-based RowKey → natural data distribution
- Simple entities → no complex relationship mapping

If multi-tenant features are added later:
- PartitionKey per user maintains isolation
- Each user's data is independently accessible
- No cross-partition queries needed

## Consequences

### Positive
- Minimal hosting costs during development and low-usage production
- Local development with Azurite without Azure subscription
- Simple entity-to-table mapping with built-in optimistic concurrency
- Fast point lookups and range queries for weekly views

### Negative
- Cannot easily add computed columns or database-level aggregations
- Weekly summaries must be calculated in application code
- No ACID transactions if we later need cross-entity updates
- Team members unfamiliar with Table Storage may have learning curve

### Neutral
- Repository pattern abstracts storage implementation for future migration
- Integration tests require Azurite to be running
- No visual query tools like SQL Server Management Studio

## Migration Path

If requirements change (e.g., complex reporting, multi-user with shared data), we can:
1. Keep repository interface stable
2. Implement SqlDailyLogRepository using Entity Framework
3. Run data migration script from Table Storage to SQL
4. Switch DI registration in Program.cs

The IDailyLogRepository interface ensures application code remains unchanged.

## Verification

1. ✅ Azurite runs locally with docker or npm
2. ✅ Repository integration tests pass against Azurite
3. ✅ Point queries return in < 50ms
4. ✅ Weekly range queries return in < 100ms
5. ✅ Infrastructure cost projection < $1/month for MVP

## References

- [Azure Table Storage Documentation](https://docs.microsoft.com/azure/storage/tables/)
- [Azure.Data.Tables SDK](https://docs.microsoft.com/dotnet/api/azure.data.tables)
- [Azurite Storage Emulator](https://docs.microsoft.com/azure/storage/common/storage-use-azurite)
- [Table Storage Pricing](https://azure.microsoft.com/pricing/details/storage/tables/)
