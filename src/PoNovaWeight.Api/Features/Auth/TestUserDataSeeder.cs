using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Features.Auth;

/// <summary>
/// Seeds a deterministic, realistic 3-year health history for the local demo user.
/// </summary>
public interface ITestUserDataSeeder
{
    Task<TestUserSeedResult> EnsureSeededAsync(CancellationToken cancellationToken = default);
}

public sealed record TestUserSeedResult(bool Seeded, int CreatedEntries, int TotalDays, int FullDays, int PartialDays, int MissingDays);

public sealed class TestUserDataSeeder(
    IDailyLogRepository dailyLogRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider,
    ILogger<TestUserDataSeeder> logger) : ITestUserDataSeeder
{
    public const string TestUserEmail = "test-user@local";
    public const string TestUserDisplayName = "Test User";

    private const decimal StartWeightLbs = 246.0m;
    private const decimal EndWeightLbs = 198.0m;

    public async Task<TestUserSeedResult> EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var startDate = today.AddYears(-3).AddDays(1);
        var totalDays = today.DayNumber - startDate.DayNumber + 1;

        logger.LogInformation("Ensuring demo data for test user {Email}. Date range {StartDate} to {EndDate} ({TotalDays} days)",
            TestUserEmail,
            startDate,
            today,
            totalDays);

        // Keep existing test data intact when present.
        var existing = await dailyLogRepository.GetRangeAsync(TestUserEmail, startDate, today, cancellationToken);
        if (existing.Count > 0)
        {
            logger.LogInformation("Skipped seeding test user data because {ExistingCount} entries already exist for {Email}", existing.Count, TestUserEmail);
            return new TestUserSeedResult(false, 0, totalDays, 0, 0, 0);
        }

        await EnsureTestUserProfileAsync(cancellationToken);

        var random = new Random(20260304);
        var dayStates = BuildDayStates(totalDays, random);

        var createdEntries = 0;
        var fullDays = dayStates.Count(s => s == DayState.Full);
        var partialDays = dayStates.Count(s => s == DayState.Partial);
        var missingDays = dayStates.Count(s => s == DayState.Missing);

        decimal? previousWeight = null;
        bool previousDayAlcohol = false;

        for (var i = 0; i < totalDays; i++)
        {
            var date = startDate.AddDays(i);
            var state = dayStates[i];

            if (state == DayState.Missing)
            {
                previousDayAlcohol = false;
                continue;
            }

            var entity = state == DayState.Full
                ? BuildFullEntity(date, i, totalDays, random, previousWeight, previousDayAlcohol)
                : BuildPartialEntity(date, i, totalDays, random, previousWeight, previousDayAlcohol);

            await dailyLogRepository.UpsertAsync(entity, cancellationToken);
            createdEntries++;

            if (entity.Weight.HasValue)
            {
                previousWeight = (decimal)entity.Weight.Value;
            }

            previousDayAlcohol = entity.AlcoholConsumed == true;
        }

        logger.LogInformation(
            "Seeded test user data for {Email}: {CreatedEntries} entries created ({FullDays} full, {PartialDays} partial, {MissingDays} missing)",
            TestUserEmail,
            createdEntries,
            fullDays,
            partialDays,
            missingDays);

        return new TestUserSeedResult(true, createdEntries, totalDays, fullDays, partialDays, missingDays);
    }

    private async Task EnsureTestUserProfileAsync(CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetAsync(TestUserEmail, cancellationToken);

        if (existingUser is null)
        {
            await userRepository.UpsertAsync(UserEntity.Create(TestUserEmail, TestUserDisplayName), cancellationToken);
            return;
        }

        existingUser.DisplayName = TestUserDisplayName;
        existingUser.LastLoginUtc = DateTimeOffset.UtcNow;
        await userRepository.UpsertAsync(existingUser, cancellationToken);
    }

    private static IReadOnlyList<DayState> BuildDayStates(int totalDays, Random random)
    {
        var fullDays = (int)Math.Round(totalDays * 0.95, MidpointRounding.AwayFromZero);
        var partialDays = (int)Math.Round(totalDays * 0.03, MidpointRounding.AwayFromZero);
        var missingDays = totalDays - fullDays - partialDays;

        var states = new List<DayState>(totalDays);
        states.AddRange(Enumerable.Repeat(DayState.Full, fullDays));
        states.AddRange(Enumerable.Repeat(DayState.Partial, partialDays));
        states.AddRange(Enumerable.Repeat(DayState.Missing, missingDays));

        // Fisher-Yates shuffle for realistic spacing of partial/missing days.
        for (var i = states.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (states[i], states[j]) = (states[j], states[i]);
        }

        return states;
    }

    private static DailyLogEntity BuildFullEntity(
        DateOnly date,
        int index,
        int totalDays,
        Random random,
        decimal? previousWeight,
        bool previousDayAlcohol)
    {
        var progress = totalDays <= 1 ? 1.0 : (double)index / (totalDays - 1);
        var alcoholConsumed = random.NextDouble() < 0.19;
        var omadCompliant = alcoholConsumed ? random.NextDouble() < 0.72 : random.NextDouble() < 0.86;

        var weight = ComputeWeight(progress, random, previousWeight, previousDayAlcohol, alcoholConsumed);
        var systolic = ComputeSystolic(progress, random, weight, omadCompliant, alcoholConsumed, previousDayAlcohol);
        var diastolic = ComputeDiastolic(systolic, random, alcoholConsumed);
        var heartRate = ClampInt(70 + (alcoholConsumed ? 4 : 0) + random.Next(-7, 8), 52, 110);

        return new DailyLogEntity
        {
            PartitionKey = TestUserEmail,
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = omadCompliant ? random.Next(5, 9) : random.Next(4, 7),
            Vegetables = random.Next(2, 6),
            Fruits = random.Next(1, 4),
            Starches = omadCompliant ? random.Next(1, 3) : random.Next(2, 5),
            Fats = random.Next(1, 4),
            Dairy = random.Next(0, 3),
            WaterSegments = alcoholConsumed ? random.Next(4, 8) : random.Next(5, 9),
            Weight = (double)weight,
            OmadCompliant = omadCompliant,
            AlcoholConsumed = alcoholConsumed,
            SystolicBP = (double)systolic,
            DiastolicBP = (double)diastolic,
            HeartRate = heartRate,
            BpReadingTime = PickReadingTime(random)
        };
    }

    private static DailyLogEntity BuildPartialEntity(
        DateOnly date,
        int index,
        int totalDays,
        Random random,
        decimal? previousWeight,
        bool previousDayAlcohol)
    {
        var progress = totalDays <= 1 ? 1.0 : (double)index / (totalDays - 1);
        var alcoholConsumed = random.NextDouble() < 0.16;
        var omadCompliant = random.NextDouble() < 0.78;

        var baseEntity = new DailyLogEntity
        {
            PartitionKey = TestUserEmail,
            RowKey = date.ToString("yyyy-MM-dd"),
            Proteins = 0,
            Vegetables = 0,
            Fruits = 0,
            Starches = 0,
            Fats = 0,
            Dairy = 0,
            WaterSegments = random.Next(2, 7),
            OmadCompliant = omadCompliant,
            AlcoholConsumed = alcoholConsumed
        };

        // Rotate partial patterns so data appears naturally inconsistent day-to-day.
        var pattern = random.Next(4);

        if (pattern is 0 or 3)
        {
            var weight = ComputeWeight(progress, random, previousWeight, previousDayAlcohol, alcoholConsumed);
            baseEntity.Weight = (double)weight;
        }

        if (pattern is 1 or 3)
        {
            var systolic = ComputeSystolic(progress, random, (decimal?)baseEntity.Weight ?? 210m, omadCompliant, alcoholConsumed, previousDayAlcohol);
            var diastolic = ComputeDiastolic(systolic, random, alcoholConsumed);
            baseEntity.SystolicBP = (double)systolic;
            baseEntity.DiastolicBP = (double)diastolic;
            baseEntity.HeartRate = ClampInt(72 + (alcoholConsumed ? 4 : 0) + random.Next(-8, 9), 55, 112);
            baseEntity.BpReadingTime = PickReadingTime(random);
        }

        if (pattern == 2)
        {
            baseEntity.Proteins = random.Next(2, 5);
            baseEntity.Vegetables = random.Next(1, 4);
            baseEntity.Starches = random.Next(1, 4);
            baseEntity.Fats = random.Next(1, 3);
        }

        return baseEntity;
    }

    private static decimal ComputeWeight(double progress, Random random, decimal? previousWeight, bool previousDayAlcohol, bool alcoholConsumed)
    {
        var trendWeight = StartWeightLbs + ((EndWeightLbs - StartWeightLbs) * (decimal)progress);
        var seasonal = (decimal)(Math.Sin(progress * 10 * Math.PI) * 0.8);
        var dailyNoise = (decimal)(random.NextDouble() * 1.4 - 0.7);
        var alcoholRetention = previousDayAlcohol ? 0.7m : (alcoholConsumed ? 0.2m : 0m);

        var current = Math.Round(trendWeight + seasonal + dailyNoise + alcoholRetention, 1);

        if (!previousWeight.HasValue)
        {
            return current;
        }

        // Keep changes plausible for day-to-day fluctuations.
        var min = previousWeight.Value - 2.4m;
        var max = previousWeight.Value + 2.4m;
        return decimal.Clamp(current, min, max);
    }

    private static decimal ComputeSystolic(double progress, Random random, decimal weight, bool omadCompliant, bool alcoholConsumed, bool previousDayAlcohol)
    {
        var baseValue = 133m - (decimal)(progress * 14.0);
        var weightEffect = (weight - 200m) * 0.06m;
        var omadEffect = omadCompliant ? -2.0m : 1.0m;
        var alcoholEffect = (alcoholConsumed ? 4.2m : 0m) + (previousDayAlcohol ? 1.8m : 0m);
        var noise = (decimal)(random.NextDouble() * 8.0 - 4.0);

        return decimal.Clamp(Math.Round(baseValue + weightEffect + omadEffect + alcoholEffect + noise, 0), 96m, 170m);
    }

    private static decimal ComputeDiastolic(decimal systolic, Random random, bool alcoholConsumed)
    {
        var gap = alcoholConsumed ? random.Next(34, 47) : random.Next(38, 53);
        var value = systolic - gap;
        return decimal.Clamp(Math.Round(value, 0), 58m, 108m);
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static string PickReadingTime(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.60)
        {
            return "Morning";
        }

        if (roll < 0.85)
        {
            return "Afternoon";
        }

        return "Evening";
    }

    private enum DayState
    {
        Full,
        Partial,
        Missing
    }
}