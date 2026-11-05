using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._Planet14.Planets;
using Content.Shared._Planet14;
using Content.Shared.Interaction;
using Content.Shared.Shuttles.Components;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class ShipbuildingTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Fresh = true, Destructive = true };

    [Test]
    public async Task StarterCreatesIndependentShipAndRodsExtendItOverPlanetGround()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<Planet14PlanetSystem>().TryCreate("Planet14Starter", 14,
                out var map, out var message), Is.True, message);
            var maps = SEntMan.System<SharedMapSystem>();
            var mapUid = maps.GetMapOrInvalid(map);
            var transform = SEntMan.System<SharedTransformSystem>();
            SEntMan.SpawnEntity("Planet14ShipStarter", new EntityCoordinates(mapUid, new Vector2(0.5f)));
            var ship = SEntMan.EntityQuery<Planet14ShipGridComponent, MapGridComponent>()
                .Single(e => SEntMan.GetComponent<TransformComponent>(e.Item1.Owner).MapUid == mapUid).Item1.Owner;
            var grid = SEntMan.GetComponent<MapGridComponent>(ship);
            Assert.That(ship, Is.Not.EqualTo(mapUid));
            Assert.That(SEntMan.HasComponent<ShuttleComponent>(ship), Is.True);
            Assert.That(SEntMan.GetComponent<PhysicsComponent>(ship).BodyType, Is.Not.EqualTo(BodyType.Static));
            Assert.That(maps.GetTileRef(ship, grid, Vector2i.Zero).Tile.IsEmpty, Is.False);

            var user = SEntMan.SpawnEntity(null, new EntityCoordinates(mapUid, new Vector2(1.5f, 1.5f)));
            var rods = SEntMan.SpawnEntity("PartRodMetal10", new EntityCoordinates(mapUid, new Vector2(1.5f, 1.5f)));
            var before = SEntMan.GetComponent<StackComponent>(rods).Count;
            // Cursor coordinates originate on the planet, next to the ship's first tile.
            var interaction = new AfterInteractEvent(user, rods, null,
                new EntityCoordinates(mapUid, new Vector2(1.5f, 0.5f)), true);
            SEntMan.EventBus.RaiseLocalEvent(rods, interaction);
            Assert.That(interaction.Handled, Is.True);
            Assert.That(SEntMan.GetComponent<StackComponent>(rods).Count, Is.EqualTo(before - 1));
            Assert.That(maps.GetTileRef(ship, grid, new Vector2i(1, 0)).Tile.IsEmpty, Is.False);
            var original = transform.GetWorldPosition(ship);
            transform.SetWorldPosition(ship, original + new Vector2(10, 0));
            Assert.That(transform.GetWorldPosition(mapUid), Is.EqualTo(Vector2.Zero));
            Assert.That(transform.GetWorldPosition(ship), Is.EqualTo(original + new Vector2(10, 0)));
            SEntMan.DeleteEntity(mapUid);
        });
    }
}
