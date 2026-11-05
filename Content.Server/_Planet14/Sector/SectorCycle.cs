using System.Text.Json.Serialization;

namespace Content.Server._Planet14.Sector;

/// <summary>
/// The durable definition of a sector cycle. All deadlines are derived from its UTC origin,
/// including time spent offline. It deliberately has no dependency on SS14's round timer.
/// </summary>
public sealed class SectorCycle
{
    public const int ProductionDayLengthSeconds = 86400;
    public const int TotalDays = 15;

    public DateTimeOffset StartedAtUtc { get; }
    public int DayLengthSeconds { get; }

    [JsonConstructor]
    public SectorCycle(DateTimeOffset startedAtUtc, int dayLengthSeconds)
    {
        if (startedAtUtc == default)
            throw new ArgumentOutOfRangeException(nameof(startedAtUtc));

        // Bounds also prevent invalid persisted data from overflowing deadline calculations.
        if (dayLengthSeconds < 60 || dayLengthSeconds > ProductionDayLengthSeconds)
            throw new ArgumentOutOfRangeException(nameof(dayLengthSeconds));

        StartedAtUtc = startedAtUtc.ToUniversalTime();
        DayLengthSeconds = dayLengthSeconds;
    }

    public SectorSnapshot GetSnapshot(DateTimeOffset now)
    {
        var elapsed = now - StartedAtUtc;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        // Scale integer ticks rather than floating point days so every boundary is inclusive
        // at exactly its deadline, even with an accelerated test clock.
        var dayTicks = TimeSpan.FromSeconds(DayLengthSeconds).Ticks;
        var collapseTicks = TotalDays * dayTicks;
        var elapsedTicks = Math.Min(elapsed.Ticks, collapseTicks);
        var arrivalTicks = 13 * dayTicks;
        var boardingTicks = arrivalTicks + dayTicks / 12; // Two hours after arrival.
        var rushTicks = boardingTicks + dayTicks / 8; // Three hours of regular boarding.
        var musicTicks = rushTicks + dayTicks / 96; // Fifteen minutes into the rush.
        var departureTicks = rushTicks + dayTicks / 72; // Twenty-minute rush.

        var blackSpace = elapsedTicks >= collapseTicks ? BlackSpacePhase.Consumed
            : elapsedTicks >= 11 * dayTicks ? BlackSpacePhase.OneWay
            : elapsedTicks >= 3 * dayTicks ? BlackSpacePhase.Traversable
            : BlackSpacePhase.Unstable;

        var phase = elapsedTicks >= collapseTicks ? SectorPhase.Collapsed
            : elapsedTicks >= 14 * dayTicks ? SectorPhase.LastDay
            : elapsedTicks >= departureTicks ? SectorPhase.Departed
            : elapsedTicks >= musicTicks ? SectorPhase.FinalFiveMinutes
            : elapsedTicks >= rushTicks ? SectorPhase.Rush
            : elapsedTicks >= boardingTicks ? SectorPhase.Boarding
            : elapsedTicks >= arrivalTicks ? SectorPhase.ShieldCharging
            : elapsedTicks >= 12 * dayTicks ? SectorPhase.CapacityReview
            : SectorPhase.Survival;

        return new SectorSnapshot(
            Math.Min((int) (elapsedTicks / dayTicks) + 1, TotalDays),
            blackSpace,
            phase,
            TimeSpan.FromTicks(collapseTicks - elapsedTicks),
            // Four sector minutes. Consumers coalesce missed pulses rather than replaying them.
            elapsedTicks * 360 / dayTicks);
    }
}

public enum BlackSpacePhase : byte
{
    Unstable,
    Traversable,
    OneWay,
    Consumed,
}

public enum SectorPhase : byte
{
    Survival,
    CapacityReview,
    ShieldCharging,
    Boarding,
    Rush,
    FinalFiveMinutes,
    Departed,
    LastDay,
    Collapsed,
}

public readonly record struct SectorSnapshot(
    int Day,
    BlackSpacePhase BlackSpace,
    SectorPhase Phase,
    TimeSpan UntilCollapse,
    long PulseNumber);
