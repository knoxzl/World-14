namespace Content.Server._Planet14.Space;

[RegisterComponent]
public sealed partial class PlanetSurfaceComponent : Component
{
    [DataField] public string Recipe = string.Empty;
    public EntityUid? Marker;
    public float MeteorCountdown = 2400;
}
