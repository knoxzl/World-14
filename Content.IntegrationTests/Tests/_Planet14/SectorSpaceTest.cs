using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Client._Planet14;
using Content.Server._Planet14.Planets;
using Content.Server._Planet14.Space;
using Content.Shared._Planet14;
using Content.Shared.Atmos.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class SectorSpaceTest : GameTest
{
    private static readonly EntProtoId PlanetVisual = "Planet14PlanetVisual";
    public override PoolSettings PoolSettings => new() { Connected = true, Fresh = true, Destructive = true };

    [Test]
    public async Task ArrivalBannersFollowPassengersBetweenPlanetAndSpace()
    {
        EntityUid ship = default;
        EntityUid surface = default;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<Planet14PlanetSystem>().TryCreate("Planet14Starter", 14,
                out var planet, out var message), Is.True, message);
            surface = SEntMan.System<SharedMapSystem>().GetMapOrInvalid(planet);
            SEntMan.System<SectorSpaceSystem>().StartSector(surface);
            SEntMan.SpawnEntity("Planet14ShipStarter", new EntityCoordinates(surface, new Vector2(0.5f)));
            ship = SEntMan.EntityQuery<Planet14ShipGridComponent>().Single().Owner;
            var passenger = SEntMan.SpawnEntity("MobHuman", new EntityCoordinates(ship, new Vector2(0.5f)));
            Server.PlayerMan.SetAttachedEntity(Pair.Player, passenger);
        });
        await Pair.RunSeconds(2);
        await Client.WaitAssertion(() =>
        {
            var prototype = CProtoMan.Index(PlanetVisual);
            Assert.That(prototype.HasComp<PlanetBackdropComponent>(Client.ResolveDependency<IComponentFactory>()), Is.True);
            Assert.That(prototype.HasComp<Robust.Client.GameObjects.SpriteComponent>(Client.ResolveDependency<IComponentFactory>()), Is.False);
            Assert.That(Client.ResolveDependency<Robust.Client.Graphics.IOverlayManager>()
                .GetOverlay<PlanetBackdropOverlay>().Space, Is.EqualTo(Robust.Shared.Enums.OverlaySpace.WorldSpaceBelowWorld));
            var banner = CEntMan.System<PlanetArrivalSystem>().CurrentArrival;
            Assert.That(banner, Is.Not.Null);
            Assert.That(banner!.Title, Does.Contain("Basirath starter planet"));
            Assert.That(banner.Subtitle, Does.Contain("Temperate grasslands"));
        });
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<SectorSpaceSystem>().TryAscend(ship), Is.True));
        await Pair.RunSeconds(2);
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<PlanetArrivalSystem>().CurrentArrival!.Title,
            Is.EqualTo("Entering Space!")));
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<SectorSpaceSystem>().TryDescend(ship, surface), Is.True));
        await Pair.RunSeconds(2);
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<PlanetArrivalSystem>().CurrentArrival!.Title,
            Does.Contain("Basirath starter planet")));
        var showerOrigin = Vector2.Zero;
        await Server.WaitAssertion(() =>
        {
            showerOrigin = SEntMan.System<SharedTransformSystem>().GetWorldPosition(Pair.Player!.AttachedEntity!.Value);
            SEntMan.System<SectorSpaceSystem>().StartMeteorShower(surface);
        });
        await Pair.RunSeconds(10);
        await Server.WaitAssertion(() =>
        {
            var transform = SEntMan.System<SharedTransformSystem>();
            var deposits = SEntMan.EntityQuery<MeteorResourceComponent, TransformComponent>()
                .Where(e => e.Item2.MapUid == surface)
                .Select(e => transform.GetWorldPosition(e.Item1.Owner)).ToArray();
            Assert.That(deposits.Any(p => Vector2.Distance(p, showerOrigin) < 100), Is.True,
                "A shower should supply materials near the player.");
            Assert.That(deposits.Any(p => Vector2.Distance(p, showerOrigin) > 400), Is.True,
                "A shower must also seed distant exploration sites.");
        });
        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(Pair.Player, null));
    }

    [Test]
    public async Task OrbitalMarkersTravelDebrisAndMeteorDepositsWork()
    {
        await Server.WaitAssertion(() =>
        {
            var planets = SEntMan.System<Planet14PlanetSystem>();
            var sector = SEntMan.System<SectorSpaceSystem>();
            var maps = SEntMan.System<SharedMapSystem>();
            var transforms = SEntMan.System<SharedTransformSystem>();
            Assert.That(planets.TryCreate("Planet14Starter", 14, out var planetMap, out var message), Is.True, message);
            var surface = maps.GetMapOrInvalid(planetMap);
            sector.StartSector(surface);
            Assert.That(sector.SpaceMap, Is.Not.Null);
            var space = sector.SpaceMap!.Value;
            Assert.That(space, Is.Not.EqualTo(surface));
            var atmosphere = SEntMan.GetComponent<MapAtmosphereComponent>(space);
            Assert.That(atmosphere.Space, Is.True);
            Assert.That(atmosphere.Mixture.TotalMoles, Is.Zero);
            var marker = SEntMan.GetComponent<PlanetSurfaceComponent>(surface).Marker!.Value;
            var markerGrid = SEntMan.GetComponent<MapGridComponent>(marker);
            Assert.That(markerGrid.LocalAABB.Width, Is.EqualTo(10));
            Assert.That(markerGrid.LocalAABB.Height, Is.EqualTo(10));
            Assert.That(SEntMan.GetComponent<PhysicsComponent>(marker).CanCollide, Is.False);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(marker).EntityName,
                Is.EqualTo("Basirath starter planet"));
            Assert.That(SEntMan.HasComponent<IFFComponent>(marker), Is.True);
            var barren = SEntMan.EntityQuery<PlanetSurfaceComponent>()
                .Single(p => p.Recipe == "Planet14Barren").Owner;
            var barrenMarker = SEntMan.GetComponent<PlanetSurfaceComponent>(barren).Marker!.Value;

            SEntMan.SpawnEntity("Planet14ShipStarter", new EntityCoordinates(surface, new Vector2(0.5f)));
            var ship = SEntMan.EntityQuery<Planet14ShipGridComponent>().Single().Owner;
            Assert.That(sector.TryAscend(ship), Is.True);
            Assert.That(SEntMan.GetComponent<TransformComponent>(ship).MapUid, Is.EqualTo(space));
            transforms.SetWorldPosition(ship, transforms.GetWorldPosition(barrenMarker) + new Vector2(51, 0));
            Assert.That(sector.TryDescend(ship, barren), Is.False, "Descent must require proximity to the planet.");
            var shuttles = SEntMan.System<SharedShuttleSystem>();
            Assert.That(shuttles.CanFTLTo(ship, SEntMan.GetComponent<TransformComponent>(barren).MapID, ship), Is.False,
                "The actual FTL route must also enforce the 50-tile orbital limit.");
            transforms.SetWorldPosition(ship, transforms.GetWorldPosition(barrenMarker) + new Vector2(49, 0));
            Assert.That(shuttles.CanFTLTo(ship, SEntMan.GetComponent<TransformComponent>(barren).MapID, ship), Is.True);
            Assert.That(sector.TryDescend(ship, barren), Is.True);
            Assert.That(SEntMan.GetComponent<TransformComponent>(ship).MapUid, Is.EqualTo(barren));
            transforms.SetWorldPosition(ship, new Vector2(10000, 10000));
            var spaceId = SEntMan.GetComponent<TransformComponent>(space).MapID;
            var orbitPosition = transforms.GetWorldPosition(barrenMarker);
            Assert.That(shuttles.GetFTLRangeOrigin(ship, spaceId), Is.EqualTo(orbitPosition));
            Assert.That(shuttles.FTLFree(ship, new EntityCoordinates(space, new Vector2(10000, 10000)), Angle.Zero, null), Is.False,
                "Surface exploration must not grant distant orbital FTL destinations.");
            Assert.That(Enumerable.Range(0, 12).Any(i => shuttles.FTLFree(ship,
                new EntityCoordinates(space, orbitPosition + new Vector2(MathF.Cos(i), MathF.Sin(i)) * 128),
                Angle.Zero, null)), Is.True, "There should be valid destinations in the planet-centred orbital bubble.");
            Assert.That(sector.TryAscend(ship), Is.True);

            var asteroid = sector.CreateAsteroid(new Vector2(2000, 2000));
            Assert.That(shuttles.GetIFFLabel(asteroid), Is.Null);
            Assert.That(SEntMan.GetComponent<MapGridComponent>(asteroid).LocalAABB.Width, Is.GreaterThan(1));
            var wreck = sector.CreateWreck(new Vector2(2500, 2000));
            Assert.That(shuttles.GetIFFLabel(wreck), Is.Null);
            Assert.That(SEntMan.GetComponent<MapGridComponent>(wreck).LocalAABB.Width, Is.GreaterThanOrEqualTo(11));
            var wreckEntities = SEntMan.EntityQuery<MetaDataComponent, TransformComponent>()
                .Where(e => e.Item2.GridUid == wreck).Select(e => e.Item1.EntityPrototype?.ID).ToArray();
            Assert.That(wreckEntities.Count(id => id != null && id.StartsWith("Crate")), Is.GreaterThanOrEqualTo(3));
            Assert.That(SEntMan.EntityQuery<Content.Shared.Storage.Components.EntityStorageComponent, TransformComponent>()
                .Where(e => e.Item2.GridUid == wreck).All(e => !e.Item1.Open), Is.True,
                "Wreck supply crates should begin closed.");
            Assert.That(wreckEntities, Does.Contain("WallSolid"));
            Assert.That(wreckEntities, Does.Contain("Girder"));
            Assert.That(wreckEntities, Does.Contain("GrilleBroken"));
            Assert.That(SEntMan.System<Content.Server.Decals.DecalSystem>()
                .GetDecalsIntersecting(wreck, SEntMan.GetComponent<MapGridComponent>(wreck).LocalAABB).Count,
                Is.GreaterThan(0));
            var deposited = sector.DepositMeteor(surface, new Vector2(8000, 8000));
            Assert.That(deposited, Is.GreaterThan(0));
            Assert.That(Enumerable.Range(-1, 3).Any(x => Enumerable.Range(-1, 3).Any(y =>
                !maps.GetTileRef(surface, SEntMan.GetComponent<MapGridComponent>(surface),
                    new Vector2i(8000 + x, 8000 + y)).Tile.IsEmpty)), Is.True);
            var blocker = SEntMan.SpawnEntity("WallSolid", new EntityCoordinates(surface, new Vector2(9000.5f, 9000.5f)));
            sector.DepositMeteor(surface, new Vector2(9000, 9000));
            Assert.That(SEntMan.EntityExists(blocker), Is.True, "Meteor deposits must preserve existing construction.");
        });
    }
}
