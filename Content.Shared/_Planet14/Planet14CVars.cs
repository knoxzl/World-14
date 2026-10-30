using Robust.Shared.Configuration;

namespace Content.Shared._Planet14;

[CVarDefs]
public sealed class Planet14CVars
{
    /// <summary>Forces the Planet 14 preset and starts the sector clock with the first round.</summary>
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("planet14.enabled", true, CVar.SERVERONLY);

    /// <summary>Real seconds per sector day, captured when a new cycle starts. 86400 in production.</summary>
    public static readonly CVarDef<int> DayLengthSeconds =
        CVarDef.Create("planet14.day_length_seconds", 86400, CVar.SERVERONLY);

    public static readonly CVarDef<bool> DayAnnouncements =
        CVarDef.Create("planet14.day_announcements", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> DayAnnouncementSound =
        CVarDef.Create("planet14.day_announcement_sound", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> StarterPlanet =
        CVarDef.Create("planet14.starter_planet", "Planet14Starter", CVar.SERVERONLY);
}
