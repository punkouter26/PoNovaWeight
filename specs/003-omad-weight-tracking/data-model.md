# Data Model: OMAD Weight Tracking

**Feature**: 003-omad-weight-tracking  
**Date**: December 12, 2025  
**Purpose**: Define entities, relationships, and validation rules

---

## Entity Definitions

### DailyLogEntity (Extended)

**Description**: Azure Table Storage entity capturing all daily health metrics for a user. Extended from existing entity to include OMAD tracking fields.

**Storage**: Azure Table Storage, table name `DailyLogs`

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| PartitionKey | string | ✅ | User identifier (email from Google OAuth) |
| RowKey | string | ✅ | Date in `yyyy-MM-dd` format |
| Timestamp | DateTimeOffset? | Auto | Azure-managed timestamp |
| ETag | ETag | Auto | Optimistic concurrency token |
| **Existing Fields** ||||
| Proteins | int | ✅ | Protein unit count |
| Vegetables | int | ✅ | Vegetable unit count |
| Fruits | int | ✅ | Fruit unit count |
| Starches | int | ✅ | Starch unit count |
| Fats | int | ✅ | Fat unit count |
| Dairy | int | ✅ | Dairy unit count |
| WaterSegments | int | ✅ | Water intake segments (0-8) |
| **New OMAD Fields** ||||
| Weight | double? | ❌ | Weight in pounds (50-500 lbs) |
| OmadCompliant | bool? | ❌ | True if OMAD was followed, null if not logged |
| AlcoholConsumed | bool? | ❌ | True if alcohol was consumed |

**Key Design**:
- Partition key: User email provides efficient single-user queries
- Row key: Date string enables range queries for calendar/trends
- Nullable OMAD fields: Backward compatible with existing entries

---

## DTO Definitions

### DailyLogDto (Extended)

```csharp
public record DailyLogDto
{
    // Existing fields
    public required DateOnly Date { get; init; }
    public required int Proteins { get; init; }
    public required int Vegetables { get; init; }
    public required int Fruits { get; init; }
    public required int Starches { get; init; }
    public required int Fats { get; init; }
    public required int Dairy { get; init; }
    public required int WaterSegments { get; init; }
    
    // New OMAD fields
    public decimal? Weight { get; init; }
    public bool? OmadCompliant { get; init; }
    public bool? AlcoholConsumed { get; init; }
}
```

### MonthlyLogsDto (New)

```csharp
public record MonthlyLogsDto
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required IReadOnlyList<DailyLogSummary> Days { get; init; }
}

public record DailyLogSummary
{
    public required DateOnly Date { get; init; }
    public bool? OmadCompliant { get; init; }
    public bool? AlcoholConsumed { get; init; }
    public decimal? Weight { get; init; }
}
```

### StreakDto (New)

```csharp
public record StreakDto
{
    public required int CurrentStreak { get; init; }
    public DateOnly? StreakStartDate { get; init; }
}
```

### WeightTrendsDto (New)

```csharp
public record WeightTrendsDto
{
    public required IReadOnlyList<TrendDataPoint> DataPoints { get; init; }
    public required int TotalDaysLogged { get; init; }
    public decimal? WeightChange { get; init; }
}

public record TrendDataPoint
{
    public required DateOnly Date { get; init; }
    public decimal? Weight { get; init; }
    public required bool IsCarryForward { get; init; }
    public bool? AlcoholConsumed { get; init; }
}
```

### AlcoholCorrelationDto (New)

```csharp
public record AlcoholCorrelationDto
{
    public required int DaysWithAlcohol { get; init; }
    public required int DaysWithoutAlcohol { get; init; }
    public decimal? AverageWeightWithAlcohol { get; init; }
    public decimal? AverageWeightWithoutAlcohol { get; init; }
    public decimal? WeightDifference { get; init; }
    public required bool HasSufficientData { get; init; }
}
```

---

## Validation Rules

### DailyLogDto Validation (Extended)

| Field | Rule | Error Message |
|-------|------|---------------|
| Date | Must not be in future | "Cannot log entries for future dates" |
| Weight | If provided, must be 50-500 | "Weight must be between 50 and 500 lbs" |
| Weight | Decimal precision max 1 | "Weight can have at most 1 decimal place" |

### Business Rules

| Rule | Description |
|------|-------------|
| 5 lb Threshold | If weight change > 5 lbs from previous day, require confirmation |
| Streak Calculation | Only explicit `OmadCompliant = false` breaks streak; null (unlogged) does not |
| Trends Minimum | Weight trends require at least 3 days of data |
| Correlation Minimum | Alcohol correlation requires at least 7 days with both alcohol and non-alcohol entries |

---

## State Transitions

### Daily Log Entry States

```
┌─────────────┐
│   Empty     │ ← Initial state (no entry for date)
└─────────────┘
       │
       │ User logs entry
       ▼
┌─────────────┐
│   Created   │ ← Entry exists with data
└─────────────┘
       │
       │ User edits
       ▼
┌─────────────┐
│   Updated   │ ← Entry modified (Timestamp updated)
└─────────────┘
       │
       │ User deletes
       ▼
┌─────────────┐
│   Deleted   │ ← Entry removed (returns to Empty state)
└─────────────┘
```

### Calendar Day Display States

| State | OmadCompliant Value | Display |
|-------|---------------------|---------|
| Unlogged | null (no entry) | Gray indicator |
| Compliant | true | Green indicator |
| Non-compliant | false | Red indicator |

---

## Relationships

```
User (authenticated via Google OAuth)
  │
  └──< DailyLogEntry (0..n per user)
         │
         ├── Date (unique per user)
         ├── Nutrients (proteins, vegetables, etc.)
         ├── Water tracking
         └── OMAD tracking (weight, compliance, alcohol)

Derived/Calculated (not stored):
  ├── Streak (computed from DailyLogEntry.OmadCompliant)
  ├── WeightTrends (computed from DailyLogEntry.Weight)
  └── AlcoholCorrelation (computed from DailyLogEntry)
```

---

## Migration Notes

### Backward Compatibility

- Existing `DailyLogEntity` rows will have `null` for new OMAD fields
- Application gracefully handles null values (displays as "not logged")
- No data migration required - Azure Table Storage is schema-less

### Rollback Strategy

- OMAD fields can be ignored if feature is rolled back
- No breaking changes to existing functionality
