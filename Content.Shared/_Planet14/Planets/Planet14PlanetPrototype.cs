using Content.Shared.Parallax.Biomes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Planet14.Planets;

/// <summary>A reusable planet recipe. Terrain layers live in the referenced biome template.</summary>
[Prototype]
public sealed partial class Planet14PlanetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)] public string Name = string.Empty;
    [DataField(required: true)] public ProtoId<BiomeTemplatePrototype> Biome;
    [DataField] public int? Seed;
    [DataField] public bool Gravity = true;
    [DataField] public Color AmbientLight = Color.FromHex("#D8B059");
    [DataField] public PlanetDayNightSettings DayNight = new();
}

/// <summary>Uses SS14's existing light curve and synchronized sun shadows.</summary>
[DataDefinition]
public sealed partial class PlanetDayNightSettings
{
    [DataField] public bool Enabled = true;
    /// <summary>One complete sunrise-to-sunrise cycle, in simulation time.</summary>
    [DataField] public TimeSpan Duration = TimeSpan.FromMinutes(30);
    /// <summary>Phase offset into the lighting curve; zero starts at midnight.</summary>
    [DataField] public TimeSpan Offset;
    [DataField] public float MinLightLevel = 0.08f;
    [DataField] public float MaxLightLevel = 3f;
    [DataField] public float ClipLight = 1.25f;
    [DataField] public Color MinLevel = new(0.1f, 0.15f, 0.50f);
    [DataField] public Color MaxLevel = new(2f, 2f, 5f);
    [DataField] public Color ClipLevel = new(1f, 1f, 1.25f);

    public bool IsValid => Duration >= TimeSpan.FromSeconds(1) &&
        float.IsFinite(MinLightLevel) && float.IsFinite(MaxLightLevel) && float.IsFinite(ClipLight) &&
        MinLightLevel >= 0 && MaxLightLevel >= MinLightLevel && ClipLight >= 0;
}
