using System.Linq;
using System.Numerics;
using Content.Server._Planet14.Planets;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Server.GameTicking;
using Content.Server.Parallax;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared._Planet14;
using Content.Shared._Planet14.Planets;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Parallax;
using Content.Shared.Shuttles.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Planet14.Space;

public sealed partial class SectorSpaceSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private ShuttleSystem _shuttles = default!;
    [Dependency] private Planet14PlanetSystem _planets = default!;
    [Dependency] private BiomeSystem _biomes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    private static readonly ProtoId<Planet14SectorPrototype> SettingsId = "Basirath";
    private static readonly EntProtoId PlanetVisual = "Planet14PlanetVisual";
    public EntityUid? SpaceMap { get; private set; }
    private readonly Dictionary<EntityUid, EntityUid> _lastMap = new();
    private readonly HashSet<Vector2i> _chunks = new();
    private float _update;
    public Planet14SectorPrototype Settings => ProtoMan.Index(SettingsId);

    public override void Initialize()
    {
        SubscribeLocalEvent<ShuttleConsoleComponent, GetVerbsEvent<AlternativeVerb>>(OnConsoleVerbs);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            SpaceMap = null;
            _lastMap.Clear();
            _chunks.Clear();
            _impacts.Clear();
        });
    }

    public void StartSector(EntityUid starter)
    {
        if (SpaceMap is { } previous && !Deleted(previous))
            return;
        var space = _maps.CreateMap(out var mapId);
        SpaceMap = space;
        _atmosphere.SetMapAtmosphere(space, true, GasMixture.SpaceGas);
        _metadata.SetEntityName(space, "Basirath sector space");
        EnsureComp<ParallaxComponent>(space);
        _shuttles.TryAddFTLDestination(mapId, true, false, false, out _);
        RegisterPlanet(starter);
        foreach (var recipe in Settings.Planets)
        {
            if (recipe.Id == Comp<PlanetSurfaceComponent>(starter).Recipe)
                continue;
            if (_planets.TryCreate(recipe.Id, null, out var planet, out _))
                RegisterPlanet(_maps.GetMapOrInvalid(planet));
        }
        for (var x = -2; x <= 2; x++)
        for (var y = -2; y <= 2; y++)
            GenerateChunk(new Vector2i(x, y));
    }

    public EntityUid RegisterPlanet(EntityUid surface)
    {
        var info = Comp<PlanetSurfaceComponent>(surface);
        if (info.Marker is { } existing && !Deleted(existing))
            return existing;
        if (SpaceMap is not { } space)
            throw new InvalidOperationException("The sector has not been started.");
        var recipe = ProtoMan.Index<Planet14PlanetPrototype>(info.Recipe);
        var grid = _maps.CreateGridEntity(Transform(space).MapID);
        var position = recipe.SpacePosition;
        var markers = EntityQueryEnumerator<PlanetMarkerComponent>();
        while (markers.MoveNext(out var other, out _))
        {
            if (Vector2.Distance(_transform.GetWorldPosition(other), position) < 40)
                position += new Vector2(200, 0);
        }
        _transform.SetWorldPosition(grid.Owner, position);
        var tiles = new List<(Vector2i, Tile)>();
        for (var x = -5; x < 5; x++)
        for (var y = -5; y < 5; y++)
            tiles.Add((new Vector2i(x, y), new Tile(_tiles["Planet14OrbitalVoid"].TileId)));
        _maps.SetTiles(grid, grid.Comp, tiles);
        RemComp<ShuttleComponent>(grid);
        _physics.SetBodyType(grid, BodyType.Static);
        _physics.SetCanCollide(grid, false);
        EnsureComp<IFFComponent>(grid);
        _shuttles.SetIFFColor(grid, Color.CornflowerBlue);
        _metadata.SetEntityName(grid, recipe.Name);
        EnsureComp<PlanetMarkerComponent>(grid).Surface = surface;
        EnsureComp<Planet14OrbitMarkerComponent>(grid);
        var orbit = EnsureComp<Planet14OrbitComponent>(surface);
        orbit.SpaceMap = GetNetEntity(space);
        orbit.Position = position;
        Dirty(surface, orbit);
        _shuttles.TryAddFTLDestination(Transform(surface).MapID, true, false, false, out _);
        SpawnAttachedTo(PlanetVisual, new EntityCoordinates(grid, Vector2.Zero));
        info.Marker = grid;
        info.MeteorCountdown = _random.NextFloat(Settings.MeteorMinSeconds, Settings.MeteorMaxSeconds);
        return grid;
    }

    private void OnConsoleVerbs(EntityUid uid, ShuttleConsoleComponent console, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || SpaceMap == null ||
            Transform(uid).GridUid is not { } ship || !HasComp<ShuttleComponent>(ship))
            return;
        var map = Transform(ship).MapUid;
        if (map is { } surface && HasComp<PlanetSurfaceComponent>(surface))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = "Ascend to space",
                Act = () => TryAscend(ship),
            });
        }
        else if (map == SpaceMap)
        {
            var markers = EntityQueryEnumerator<PlanetMarkerComponent>();
            while (markers.MoveNext(out var marker, out var planet))
            {
                if (!WithinDescentRange(ship, marker))
                    continue;
                var destination = planet.Surface;
                args.Verbs.Add(new AlternativeVerb
                {
                    Text = $"Descend to {Name(destination)}",
                    Act = () => TryDescend(ship, destination),
                });
            }
        }
    }

    public bool WithinDescentRange(EntityUid ship, EntityUid marker) =>
        Transform(ship).MapUid == SpaceMap &&
        Vector2.Distance(_maps.GetGridPosition((ship, Comp<PhysicsComponent>(ship), Transform(ship))),
            _transform.GetWorldPosition(marker)) <= 20;

    private bool CanTravel(EntityUid ship)
    {
        if (!HasComp<ShuttleComponent>(ship) || HasComp<MapComponent>(ship) || HasComp<FTLComponent>(ship))
            return false;
        var docks = EntityQueryEnumerator<DockingComponent, TransformComponent>();
        while (docks.MoveNext(out _, out var dock, out var transform))
        {
            if (dock.Docked && transform.GridUid == ship)
                return false;
        }
        return true;
    }

    public bool TryAscend(EntityUid ship)
    {
        if (!CanTravel(ship) || SpaceMap is not { } space || Transform(ship).MapUid is not { } surface ||
            !TryComp<PlanetSurfaceComponent>(surface, out var planet))
            return false;
        var marker = RegisterPlanet(surface);
        // Leave room to fly around the planet, while keeping descent immediately available.
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var position = _transform.GetWorldPosition(marker) + _random.NextVector2(15, 19);
            var radius = MathF.Max(Comp<MapGridComponent>(ship).LocalAABB.Width, Comp<MapGridComponent>(ship).LocalAABB.Height) + 2;
            var occupied = new List<Entity<MapGridComponent>>();
            _maps.FindGridsIntersecting(Transform(space).MapID,
                new Box2(position - new Vector2(radius), position + new Vector2(radius)), ref occupied, includeMap: false);
            if (occupied.Any(e => e.Owner != marker))
                continue;
            MoveShip(ship, space, position - _transform.GetWorldRotation(ship).RotateVec(Comp<PhysicsComponent>(ship).LocalCenter));
            return true;
        }
        return false;
    }

    public bool TryDescend(EntityUid ship, EntityUid surface)
    {
        if (!CanTravel(ship) || !TryComp<PlanetSurfaceComponent>(surface, out var planet) ||
            planet.Marker is not { } marker || !WithinDescentRange(ship, marker))
            return false;
        var grid = Comp<MapGridComponent>(ship);
        var radius = (int)MathF.Ceiling(MathF.Max(grid.LocalAABB.Width, grid.LocalAABB.Height)) + 2;
        if (radius > 64)
            return false;
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var centre = _random.NextVector2(80, 300);
            var area = new Box2(centre - new Vector2(radius), centre + new Vector2(radius));
            var occupied = new List<Entity<MapGridComponent>>();
            _maps.FindGridsIntersecting(Transform(surface).MapID, area, ref occupied, includeMap: false);
            if (occupied.Count > 0)
                continue;
            var blocks = new HashSet<EntityUid>();
            _lookup.GetEntitiesIntersecting(Transform(surface).MapID, area, blocks);
            if (blocks.Any(e => HasComp<ActorComponent>(e) || Transform(e).Anchored))
                continue;
            _biomes.ReserveTiles(surface, area, new List<(Vector2i, Tile)>());
            MoveShip(ship, surface, centre);
            return true;
        }
        return false;
    }

    private void MoveShip(EntityUid ship, EntityUid destination, Vector2 position)
    {
        _transform.SetCoordinates(ship, new EntityCoordinates(destination, position));
        _physics.SetLinearVelocity(ship, Vector2.Zero);
        _physics.SetAngularVelocity(ship, 0);
    }

    public override void Update(float frameTime)
    {
        if (SpaceMap is not { } space || Deleted(space))
            return;
        _update -= frameTime;
        if (_update > 0)
            return;
        _update = 1;
        var surfaces = EntityQueryEnumerator<PlanetSurfaceComponent>();
        while (surfaces.MoveNext(out var surface, out var planet))
        {
            if (planet.Marker == null)
                RegisterPlanet(surface);
            planet.MeteorCountdown--;
            if (planet.MeteorCountdown <= 0)
            {
                StartMeteorShower(surface);
                planet.MeteorCountdown = _random.NextFloat(Settings.MeteorMinSeconds, Settings.MeteorMaxSeconds);
            }
        }
        var players = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        var present = new HashSet<EntityUid>();
        while (players.MoveNext(out var uid, out var actor, out var transform))
        {
            present.Add(uid);
            if (transform.MapUid is not { } map || map != space && !HasComp<PlanetSurfaceComponent>(map))
            {
                _lastMap.Remove(uid);
                continue;
            }
            if (!_lastMap.TryGetValue(uid, out var oldMap) || map != oldMap)
            {
                _lastMap[uid] = map;
                var recipe = TryComp<PlanetSurfaceComponent>(map, out var surface)
                    ? ProtoMan.Index<Planet14PlanetPrototype>(surface.Recipe) : null;
                RaiseNetworkEvent(new PlanetArrivalEvent(recipe == null ? "Entering Space!" : $"Entering {recipe.Name}",
                    recipe == null ? "Cold with no air!" : $"Biome: {recipe.BiomeName}",
                    recipe?.Description ?? "Hope you brought a space suit!"), actor.PlayerSession.Channel);
            }
            if (map == space)
            {
                var position = _transform.GetWorldPosition(uid);
                var chunk = new Vector2i((int)MathF.Floor(position.X / 256), (int)MathF.Floor(position.Y / 256));
                for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                    GenerateChunk(chunk + new Vector2i(x, y));
            }
        }
        foreach (var disconnected in _lastMap.Keys.Where(e => !present.Contains(e)).ToArray())
            _lastMap.Remove(disconnected);
        UpdateMeteorImpacts();
    }
}
