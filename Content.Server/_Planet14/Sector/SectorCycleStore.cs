using System.IO;
using System.Text.Json;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server._Planet14.Sector;

/// <summary>Stores only the clock definition, not the world, characters, or evacuation seats.</summary>
public static class SectorCycleStore
{
    public static readonly ResPath StatePath = new("/planet14/sector-cycle.json");
    private static readonly ResPath TemporaryPath = new("/planet14/sector-cycle.json.tmp");

    public static SectorCycle? Load(IWritableDirProvider storage)
    {
        if (!storage.Exists(StatePath))
            return null;

        using var stream = storage.OpenRead(StatePath);
        return JsonSerializer.Deserialize<SectorCycle>(stream)
            ?? throw new InvalidDataException("The sector cycle file contains null.");
    }

    /// <summary>
    /// Creates a cycle without replacing an existing one. Publish only after the complete file
    /// has been written, so a failed write cannot make the running clock appear durable.
    /// </summary>
    public static void Create(IWritableDirProvider storage, SectorCycle cycle)
    {
        if (storage.Exists(StatePath))
            throw new IOException("A sector cycle already exists.");

        storage.CreateDir(StatePath.Directory);
        using (var stream = storage.Open(TemporaryPath, FileMode.Create))
        {
            JsonSerializer.Serialize(stream, cycle);
            stream.Flush();
        }

        storage.Rename(TemporaryPath, StatePath);
    }
}
