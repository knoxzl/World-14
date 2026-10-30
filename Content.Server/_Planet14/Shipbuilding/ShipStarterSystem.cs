using System.Numerics;
using Content.Shared._Planet14;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Planet14.Shipbuilding;

public sealed partial class ShipStarterSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShipStarterComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ShipStarterComponent> ent, ref MapInitEvent args)
    {
        var coordinates = _transform.ToMapCoordinates(Transform(ent).Coordinates);
        var grid = _maps.CreateGridEntity(coordinates.MapId);
        // The construction preview marks the tile centre; grids use tile corners.
        _transform.SetWorldPosition(grid.Owner, coordinates.Position - new Vector2(0.5f));
        EnsureComp<Planet14ShipGridComponent>(grid);
        _maps.SetTile(grid, Vector2i.Zero, new Tile(_tiles["Lattice"].TileId));
        QueueDel(ent);
    }
}
