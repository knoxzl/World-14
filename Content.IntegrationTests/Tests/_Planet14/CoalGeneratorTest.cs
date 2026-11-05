using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._Planet14.Planets;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Power.Generator;
using Content.Shared.Audio;
using Content.Shared.Power.Generator;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class CoalGeneratorTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Fresh = true, Destructive = true };

    [Test]
    public async Task CoalPowersGeneratorAndHumStopsWhenEmpty()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<Planet14PlanetSystem>().TryCreate("Planet14Starter", 14,
                out var map, out var message), Is.True, message);
            var surface = SEntMan.System<SharedMapSystem>().GetMapOrInvalid(map);
            var tiles = Server.ResolveDependency<ITileDefinitionManager>();
            SEntMan.System<SharedMapSystem>().SetTile(surface, SEntMan.GetComponent<MapGridComponent>(surface),
                Vector2i.Zero, new Tile(tiles["FloorSteel"].TileId));
            var uid = SEntMan.SpawnEntity("Planet14CoalGenerator", new EntityCoordinates(surface, new Vector2(0.5f)));
            SEntMan.System<SharedTransformSystem>().AnchorEntity(uid);
            Assert.That(SEntMan.GetComponent<TransformComponent>(uid).Anchored, Is.True);
            var generators = SEntMan.System<GeneratorSystem>();
            var materials = SEntMan.System<MaterialStorageSystem>();
            Assert.That(materials.TryChangeMaterialAmount(uid, "Coal", 100), Is.True);
            Assert.That(generators.GetFuel(uid), Is.EqualTo(1).Within(0.001));
            generators.SetFuelGeneratorOn(uid, true);
            generators.Update(30);
            Assert.That(SEntMan.GetComponent<PowerSupplierComponent>(uid).MaxSupply, Is.EqualTo(10000));
            Assert.That(SEntMan.GetComponent<PowerSupplierComponent>(uid).Voltage, Is.EqualTo(Content.Shared.Power.Voltage.Medium));
            Assert.That(SEntMan.GetComponent<AmbientSoundComponent>(uid).Enabled, Is.True);
            Assert.That(generators.GetFuel(uid), Is.EqualTo(0.5).Within(0.001));
            generators.Update(31);
            generators.Update(1);
            Assert.That(SEntMan.GetComponent<FuelGeneratorComponent>(uid).On, Is.False);
            Assert.That(SEntMan.GetComponent<PowerSupplierComponent>(uid).Enabled, Is.False);
            Assert.That(SEntMan.GetComponent<AmbientSoundComponent>(uid).Enabled, Is.False);
        });
    }
}
