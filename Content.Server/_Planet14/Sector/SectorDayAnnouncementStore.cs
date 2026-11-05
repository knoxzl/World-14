using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server._Planet14.Sector;

/// <summary>At-most-once daily broadcasts, keyed by the durable cycle origin.</summary>
public static class SectorDayAnnouncementStore
{
    public static bool TryMarkSent(IWritableDirProvider storage, DateTimeOffset origin, int day)
    {
        if (day is < 1 or > SectorCycle.TotalDays)
            throw new ArgumentOutOfRangeException(nameof(day));

        var path = new ResPath($"/planet14/announcements/{origin.UtcTicks}/{day}.sent");
        if (storage.Exists(path))
            return false;

        storage.CreateDir(path.Directory);
        // A marker is intentionally written before dispatch. A crash cannot cause a repeat,
        // though one that occurs between this write and dispatch can lose that day's message.
        using var stream = storage.Open(path, FileMode.CreateNew);
        return true;
    }
}
