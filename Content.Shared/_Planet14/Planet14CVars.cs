using Robust.Shared.Configuration;

namespace Content.Shared._Planet14;

[CVarDefs]
public sealed class Planet14CVars
{
    /// <summary>Enables the sector clock on server startup. Does not replace the SS14 game preset.</summary>
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("planet14.enabled", false, CVar.SERVERONLY);

    /// <summary>Real seconds per sector day, captured when a new cycle starts. 86400 in production.</summary>
    public static readonly CVarDef<int> DayLengthSeconds =
        CVarDef.Create("planet14.day_length_seconds", 86400, CVar.SERVERONLY);

    public static readonly CVarDef<bool> DayAnnouncements =
        CVarDef.Create("planet14.day_announcements", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> DayAnnouncementSound =
        CVarDef.Create("planet14.day_announcement_sound", true, CVar.SERVERONLY);
}
