using Content.Server.Parallax;
using Content.Shared._Planet14.Planets;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Robust.Shared.Map;

namespace Content.Server._Planet14.Planets;

/// <summary>Creates fresh maps from YAML recipes without modifying existing player worlds.</summary>
public sealed partial class Planet14PlanetSystem : EntitySystem
{
    [Dependency] private BiomeSystem _biomes = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    public bool TryCreate(string prototype, int? seed, out MapId mapId, out string message)
    {
        mapId = default;
        if (!ProtoMan.TryIndex<Planet14PlanetPrototype>(prototype, out var planet) ||
            !ProtoMan.TryIndex(planet.Biome, out var biome))
        {
            message = Loc.GetString("planet14-planet-unknown", ("prototype", prototype));
            return false;
        }

        seed ??= planet.Seed;
        if (seed < 0 || !planet.DayNight.IsValid)
        {
            message = Loc.GetString("planet14-planet-invalid", ("prototype", prototype));
            return false;
        }

        EntityUid? createdMap = null;
        try
        {
            var uid = _maps.CreateMap(out var id, runMapInit: false);
            createdMap = uid;
            _metadata.SetEntityName(uid, planet.Name);
            _biomes.EnsurePlanet(uid, biome, seed, mapLight: planet.AmbientLight);

            var gravity = Comp<GravityComponent>(uid);
            gravity.Enabled = planet.Gravity;
            gravity.Inherent = planet.Gravity;
            Dirty(uid, gravity);

            var settings = planet.DayNight;
            var light = Comp<LightCycleComponent>(uid);
            light.Enabled = settings.Enabled;
            light.InitialOffset = false;
            light.Duration = settings.Duration;
            light.Offset = settings.Offset;
            light.MinLightLevel = settings.MinLightLevel;
            light.MaxLightLevel = settings.MaxLightLevel;
            light.ClipLight = settings.ClipLight;
            light.MinLevel = settings.MinLevel;
            light.MaxLevel = settings.MaxLevel;
            light.ClipLevel = settings.ClipLevel;
            Dirty(uid, light);

            // A fixed-light planet should not keep rotating its shadows.
            if (!settings.Enabled)
            {
                RemComp<SunShadowCycleComponent>(uid);
                RemComp<SunShadowComponent>(uid);
            }

            // Apply all settings before MapInit copies the light duration/offset to shadows.
            _maps.InitializeMap(uid);
            mapId = id;
            message = Loc.GetString("planet14-planet-created", ("name", planet.Name), ("map", id));
            Log.Info($"Created Planet 14 planet {prototype} on map {id} (seed override: {seed}).");
            return true;
        }
        catch (Exception e)
        {
            if (createdMap is { } uid)
                QueueDel(uid);
            Log.Error($"Could not generate Planet 14 planet {prototype}: {e}");
            message = Loc.GetString("planet14-planet-failed");
            return false;
        }
    }
}
