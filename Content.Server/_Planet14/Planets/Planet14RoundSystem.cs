using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Parallax;
using Content.Server.Station.Systems;
using Content.Shared._Planet14;
using Content.Shared._Planet14.Planets;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server._Planet14.Planets;

/// <summary>Connects the Planet 14 preset to normal round-start and late-join spawning.</summary>
public sealed partial class Planet14RoundSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private Planet14PlanetSystem _planets = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private ServerStationSystem _stations = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private BiomeSystem _biomes = default!;
    [Dependency] private IRobustRandom _random = default!;

    private EntityUid? _starterMap;
    private static readonly ProtoId<GameMapPrototype> StarterMap = "Planet14";
    private static readonly ProtoId<ContentTileDefinition> ArrivalTile = "FloorPlanetGrass";

    /// <summary>Apply after development presets, before the ticker and map manager initialize.</summary>
    public static void Configure(IConfigurationManager config)
    {
        if (!config.GetCVar(Planet14CVars.Enabled))
            return;

        config.SetCVar(CCVars.GameLobbyEnabled, true);
        config.SetCVar(CCVars.GameLobbyDefaultPreset, "Planet14");
        config.SetCVar(CCVars.GameMap, "Planet14");
        config.SetCVar(CCVars.GameMapPool, "Planet14");
        config.SetCVar(CCVars.GameMapRotation, false);
        config.SetCVar(CCVars.GameLobbyFallbackEnabled, false);
        config.SetCVar(CCVars.NewCharacterJobs, "Survivor");
        config.SetCVar(CCVars.GameRoleTimers, false);
        config.SetCVar(CCVars.GameRoleLoadoutTimers, false);
        config.SetCVar(CCVars.ICRandomCharacters, false);
        config.SetCVar(CCVars.PreloadGrids, false);
        config.SetCVar(CCVars.ProcgenPreload, false);
        config.SetCVar(CCVars.ArrivalsShuttles, false);
        config.SetCVar(CCVars.EmergencyShuttleEnabled, false);
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoadingMapsEvent>(OnLoadingMaps);
        SubscribeLocalEvent<PostGameMapLoad>(OnMapLoaded, after: [typeof(ServerStationSystem)]);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _starterMap = null);
    }

    private void OnLoadingMaps(LoadingMapsEvent args)
    {
        if (!_config.GetCVar(Planet14CVars.Enabled))
            return;
        args.Maps.Clear();
        args.Maps.Add(ProtoMan.Index(StarterMap));
    }

    private void OnMapLoaded(PostGameMapLoad args)
    {
        if (!_config.GetCVar(Planet14CVars.Enabled) || args.GameMap.ID != "Planet14")
            return;

        var uid = _maps.GetMapOrInvalid(args.Map);
        var recipe = ProtoMan.Index<Planet14PlanetPrototype>(_config.GetCVar(Planet14CVars.StarterPlanet));
        _planets.ApplyRecipe(uid, recipe);
        _starterMap = uid;

        // A small, permanently clear arrival area prevents trees or rocks spawning inside survivors.
        var tiles = new List<(Vector2i, Tile)>();
        var grass = ProtoMan.Index(ArrivalTile);
        for (var x = -6; x <= 6; x++)
        for (var y = -6; y <= 6; y++)
            tiles.Add((new Vector2i(x, y), new Tile(grass.TileId)));
        _maps.SetTiles(uid, Comp<MapGridComponent>(uid), tiles);
        _biomes.ReserveTiles(uid, new Box2(-6, -6, 7, 7), new List<(Vector2i, Tile)>());

        // Keep job/ID/station bookkeeping valid after moving players off the tiny seed grid.
        foreach (var grid in args.Grids)
        {
            if (_stations.GetOwningStation(grid) is not { } station)
                continue;
            _stations.AddGridToStation(station, uid);
            break;
        }
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent args)
    {
        if (!_config.GetCVar(Planet14CVars.Enabled) || _starterMap is not { } map || Deleted(map))
            return;
        var position = new Vector2(_random.Next(-4, 5) + 0.5f, _random.Next(-4, 5) + 0.5f);
        _transform.SetCoordinates(args.Mob, new EntityCoordinates(map, position));
    }
}
