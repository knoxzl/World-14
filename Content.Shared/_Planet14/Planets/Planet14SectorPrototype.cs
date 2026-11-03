using Robust.Shared.Prototypes;

namespace Content.Shared._Planet14.Planets;

[Prototype]
public sealed partial class Planet14SectorPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public List<ProtoId<Planet14PlanetPrototype>> Planets = new();
    [DataField] public float MeteorMinSeconds = 1800;
    [DataField] public float MeteorMaxSeconds = 3600;
    [DataField] public float WreckChance = 0.12f;
    [DataField] public float AsteroidChance = 0.3f;
    [DataField] public int MaxChunks = 256;
    [DataField] public int MaxMeteorRocks = 4096;
}
