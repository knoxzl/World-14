using System.Numerics;
using System.Linq;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Planet14.Space;

public sealed partial class SectorSpaceSystem
{
    private static readonly EntProtoId[] AsteroidRocks = ["WallRock", "WallRockTin", "WallRockCoal", "WallRockQuartz"];
    private static readonly EntProtoId[] MeteorRocks = ["WallRock", "WallRockGold", "WallRockSilver", "WallRockUranium", "WallRockPlasma", "WallRockDiamond"];
    private static readonly EntProtoId FallingMeteor = "Planet14FallingMeteor";
    private static readonly EntProtoId[] WreckSalvage = ["CrateGenericSteel", "PartRodMetal10", "SheetSteel10", "CableApcStack10"];
    private static readonly EntProtoId WreckWall = "WallSolid";
    private readonly List<(EntityUid Surface, Vector2 Centre, int Delay)> _impacts = new();

    public void GenerateChunk(Vector2i chunk)
    {
        if (SpaceMap is not { } space || _chunks.Count >= Settings.MaxChunks || !_chunks.Add(chunk))
            return;
        var position = new Vector2(chunk.X * 256 + 128, chunk.Y * 256 + 128) + _random.NextVector2(0, 70);
        var markers = EntityQueryEnumerator<PlanetMarkerComponent>();
        while (markers.MoveNext(out var marker, out _))
        {
            if (Vector2.Distance(_transform.GetWorldPosition(marker), position) < 80)
                return;
        }
        if (_random.Prob(Settings.WreckChance))
            CreateWreck(position);
        else if (_random.Prob(Settings.AsteroidChance))
            CreateAsteroid(position);
    }

    public EntityUid CreateAsteroid(Vector2 position)
    {
        var grid = _maps.CreateGridEntity(Transform(SpaceMap!.Value).MapID);
        _transform.SetWorldPosition(grid.Owner, position);
        _metadata.SetEntityName(grid, "Basirath asteroid");
        var radius = _random.Next(2, 5);
        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
        {
            if (x * x + y * y > radius * radius)
                continue;
            _maps.SetTile(grid, new Vector2i(x, y), new Tile(_tiles["FloorAsteroidSand"].TileId));
            SpawnAttachedTo(_random.Pick(AsteroidRocks), new EntityCoordinates(grid, new Vector2(x + 0.5f, y + 0.5f)));
        }
        RemComp<ShuttleComponent>(grid);
        _physics.SetBodyType(grid, BodyType.Static);
        return grid;
    }

    public EntityUid CreateWreck(Vector2 position)
    {
        var grid = _maps.CreateGridEntity(Transform(SpaceMap!.Value).MapID);
        _transform.SetWorldPosition(grid.Owner, position);
        _metadata.SetEntityName(grid, "Unidentified wreck");
        for (var x = -3; x <= 3; x++)
        for (var y = -2; y <= 2; y++)
        {
            if (_random.Prob(0.15f))
                continue;
            _maps.SetTile(grid, new Vector2i(x, y), new Tile(_tiles["Plating"].TileId));
            if (Math.Abs(x) == 3 && Math.Abs(y) == 2)
                SpawnAttachedTo(WreckWall, new EntityCoordinates(grid, new Vector2(x + 0.5f, y + 0.5f)));
        }
        _maps.SetTile(grid, Vector2i.Zero, new Tile(_tiles["Plating"].TileId));
        foreach (var item in WreckSalvage)
            SpawnAtPosition(item, new EntityCoordinates(grid, new Vector2(0.5f)));
        return grid;
    }

    public void StartMeteorShower(EntityUid surface)
    {
        // One group near each current settlement's players, plus independent distant strikes.
        // Limit nearby groups so large populations do not turn an uncommon shower into a storm.
        var centres = new List<Vector2>();
        var players = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (players.MoveNext(out var player, out _, out var transform))
        {
            if (transform.MapUid != surface || centres.Count >= 3)
                continue;
            var centre = _transform.GetWorldPosition(player) + _random.NextVector2(25, 65);
            centres.Add(centre);
        }
        centres.Add(_random.NextVector2(500, 1500));
        centres.Add(_random.NextVector2(1600, 3000));
        foreach (var centre in centres)
        for (var i = 0; i < 5; i++)
        {
            var target = centre + _random.NextVector2(0, 12);
            _impacts.Add((surface, target, 2 + i));
        }
    }

    private void UpdateMeteorImpacts()
    {
        for (var i = _impacts.Count - 1; i >= 0; i--)
        {
            var impact = _impacts[i];
            if (Deleted(impact.Surface))
            {
                _impacts.RemoveAt(i);
                continue;
            }
            if (impact.Delay > 0)
            {
                if (impact.Delay == 2)
                {
                    var meteor = SpawnAtPosition(FallingMeteor,
                        new EntityCoordinates(impact.Surface, impact.Centre + new Vector2(20, 35)));
                    _physics.SetLinearVelocity(meteor, new Vector2(-10, -17.5f));
                }
                _impacts[i] = (impact.Surface, impact.Centre, impact.Delay - 1);
                continue;
            }
            DepositMeteor(impact.Surface, impact.Centre);
            _impacts.RemoveAt(i);
        }
    }

    public int DepositMeteor(EntityUid surface, Vector2 centre)
    {
        var created = 0;
        var remaining = Math.Max(0, Settings.MaxMeteorRocks - EntityQuery<MeteorResourceComponent>().Count());
        var ore = _random.Pick(MeteorRocks);
        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        {
            if (created >= remaining)
                return created;
            var position = centre + new Vector2(x, y);
            var area = new Box2(position, position + Vector2.One);
            var occupied = new HashSet<EntityUid>();
            _lookup.GetEntitiesIntersecting(Transform(surface).MapID, area, occupied);
            if (occupied.Count > 0)
                continue;
            var grids = new List<Entity<MapGridComponent>>();
            _maps.FindGridsIntersecting(Transform(surface).MapID, area, ref grids, includeMap: false);
            if (grids.Count > 0)
                continue;
            var indices = new Vector2i((int)MathF.Floor(position.X), (int)MathF.Floor(position.Y));
            var tile = new Tile(_tiles["FloorPlanetDirt"].TileId);
            var tiles = new List<(Vector2i, Tile)> { (indices, tile) };
            _biomes.ReserveTiles(surface, new Box2(indices.X, indices.Y, indices.X + 1, indices.Y + 1), tiles);
            _maps.SetTile((surface, Comp<MapGridComponent>(surface)), indices, tile);
            var rock = SpawnAttachedTo(_random.Prob(0.6f) ? ore : AsteroidRocks[0],
                new EntityCoordinates(surface, new Vector2(indices.X + 0.5f, indices.Y + 0.5f)));
            EnsureComp<MeteorResourceComponent>(rock);
            created++;
        }
        return created;
    }
}
