using Robust.Shared.Serialization;

namespace Content.Shared._Planet14;

[Serializable, NetSerializable]
public sealed class PlanetArrivalEvent(string title, string subtitle, string description) : EntityEventArgs
{
    public string Title = title;
    public string Subtitle = subtitle;
    public string Description = description;
}
