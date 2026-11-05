namespace Content.Server.Lathe;

/// <summary>Allows manual fabricators to pause work without discarding materials or progress.</summary>
[ByRefEvent]
public struct LatheProgressAttemptEvent
{
    public bool Cancelled;
}
