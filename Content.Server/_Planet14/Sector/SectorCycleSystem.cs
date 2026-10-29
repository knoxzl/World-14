using Content.Shared._Planet14;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Server._Planet14.Sector;

/// <summary>
/// Opt-in, real-time sector clock. Publishes current state for future gameplay systems;
/// it does not yet spawn a shuttle, transport players, or destroy the world.
/// </summary>
public sealed partial class SectorCycleSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IResourceManager _resources = default!;

    private SectorCycle? _cycle;
    private SectorSnapshot? _previous;
    private DateTimeOffset _nextUpdate;
    private bool _enabled;
    private bool _loadFailed;

    public override void Initialize()
    {
        base.Initialize();
        _enabled = _config.GetCVar(Planet14CVars.Enabled);
        if (!_enabled)
            return;

        try
        {
            _cycle = SectorCycleStore.Load(_resources.UserData);
        }
        catch (Exception e)
        {
            // Never silently replace a corrupt/unreadable cycle with a fresh 15-day timer.
            _loadFailed = true;
            Log.Error($"Planet 14 sector clock could not be loaded: {e}");
        }
    }

    public bool TryGetSnapshot(out SectorSnapshot snapshot)
    {
        snapshot = default;
        if (_cycle == null)
            return false;

        snapshot = _cycle.GetSnapshot(DateTimeOffset.UtcNow);
        return true;
    }

    public bool TryStart(out string message)
    {
        if (!_enabled)
        {
            message = Loc.GetString("planet14-clock-disabled");
            return false;
        }

        if (_loadFailed)
        {
            message = Loc.GetString("planet14-clock-load-failed");
            return false;
        }

        if (_cycle != null)
        {
            message = Loc.GetString("planet14-clock-already-started");
            return false;
        }

        try
        {
            var cycle = new SectorCycle(DateTimeOffset.UtcNow, _config.GetCVar(Planet14CVars.DayLengthSeconds));
            SectorCycleStore.Create(_resources.UserData, cycle);
            _cycle = cycle;
            Log.Info($"Basirath sector cycle started at {cycle.StartedAtUtc:O}, {cycle.DayLengthSeconds} seconds per day.");
            message = Loc.GetString("planet14-clock-started");
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Planet 14 sector clock could not be started: {e}");
            message = Loc.GetString("planet14-clock-start-failed");
            return false;
        }
    }

    /// <summary>Called by the server frame loop, which runs even when simulation is paused.</summary>
    internal void UpdateClock()
    {
        var now = DateTimeOffset.UtcNow;
        if (_cycle == null || now < _nextUpdate)
            return;

        _nextUpdate = now.AddSeconds(1);
        var snapshot = _cycle.GetSnapshot(now);
        var previous = _previous;
        _previous = snapshot;

        // Startup publishes only the current phase, not a backlog of obsolete transitions.
        if (previous == null || previous.Value.Day != snapshot.Day ||
            previous.Value.BlackSpace != snapshot.BlackSpace || previous.Value.Phase != snapshot.Phase)
        {
            Log.Info($"Basirath day {snapshot.Day}: {snapshot.BlackSpace}, {snapshot.Phase}.");
            RaiseLocalEvent(new SectorPhaseChangedEvent(previous, snapshot));
        }

        // Do not replay missed heartbeats on restart or after a stall. Collapse has no pulses.
        if (previous != null && snapshot.Phase != SectorPhase.Collapsed &&
            snapshot.PulseNumber > previous.Value.PulseNumber)
        {
            RaiseLocalEvent(new BlackSpacePulseEvent(snapshot.PulseNumber));
        }
    }
}

/// <summary>
/// Current-state notification. Previous is null on startup; a single update can cross multiple
/// deadlines. Consumers must reconcile to Current rather than assuming adjacent phases.
/// </summary>
public sealed class SectorPhaseChangedEvent(SectorSnapshot? previous, SectorSnapshot current) : EntityEventArgs
{
    public SectorSnapshot? Previous { get; } = previous;
    public SectorSnapshot Current { get; } = current;
}

/// <summary>A live heartbeat notification. No missed pulses are replayed.</summary>
public sealed class BlackSpacePulseEvent(long pulseNumber) : EntityEventArgs
{
    public long PulseNumber { get; } = pulseNumber;
}
