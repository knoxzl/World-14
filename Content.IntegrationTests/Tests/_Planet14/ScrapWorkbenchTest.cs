using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._Planet14.Planets;
using Content.Server._Planet14.Workbench;
using Content.Server.Lathe;
using Content.Server.Materials;
using Content.Shared.Lathe;
using Content.Shared.Audio;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class ScrapWorkbenchTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Fresh = true, Destructive = true };
    private static readonly ProtoId<LatheRecipePrototype> ScrewdriverRecipe = "Planet14BenchScrewdriver";

    [Test]
    public async Task ManualCraftingPausesOutsideThreeTilesAndResumesWithoutPower()
    {
        EntityUid bench = default;
        EntityUid person = default;
        EntityUid mapUid = default;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<Planet14PlanetSystem>().TryCreate("Planet14Starter", 14,
                out var map, out var message), Is.True, message);
            mapUid = SEntMan.System<SharedMapSystem>().GetMapOrInvalid(map);
            bench = SEntMan.SpawnEntity("Planet14ScrapWorkbench", new EntityCoordinates(mapUid, new Vector2(0.5f)));
            person = SEntMan.SpawnEntity("MobHuman", new EntityCoordinates(mapUid, new Vector2(2.5f, 0.5f)));
            Server.PlayerMan.SetAttachedEntity(Pair.Player, person);
            var lathe = SEntMan.GetComponent<LatheComponent>(bench);
            var recipes = SEntMan.System<LatheSystem>().GetAvailableRecipes(bench, lathe);
            var outputs = recipes.Select(r => SProtoMan.Index(r).Result!.Value.Id).ToArray();
            Assert.That(outputs, Is.SupersetOf(new[]
            {
                "CableApcStack1", "CableMVStack1", "CableHVStack1",
                "CircuitImprinterMachineCircuitboard", "MicroManipulatorStockPart", "Beaker",
                "Wrench", "Screwdriver", "Crowbar", "Wirecutter", "Welder",
            }));
            foreach (var recipe in recipes)
                Assert.That(SProtoMan.HasIndex(SProtoMan.Index(recipe).Result!.Value), Is.True);
            var storage = SEntMan.System<MaterialStorageSystem>();
            Assert.That(storage.TryChangeMaterialAmount(bench, "Steel", 5000), Is.True);
            Assert.That(storage.TryChangeMaterialAmount(bench, "Wood", 5000), Is.True);
            var system = SEntMan.System<LatheSystem>();
            Assert.That(system.TryAddToQueue(bench, SProtoMan.Index(ScrewdriverRecipe), 1), Is.True);
            Assert.That(system.TryStartProducing(bench), Is.True, "The bench must run without an APC receiver.");
        });
        await Pair.RunSeconds(5);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<AmbientSoundComponent>(bench).Enabled, Is.True);
            Assert.That(SEntMan.System<ScrapWorkbenchSystem>().IsAttended(
                (bench, SEntMan.GetComponent<ScrapWorkbenchComponent>(bench))), Is.True,
                $"Person: {SEntMan.GetComponent<TransformComponent>(person).Coordinates}; " +
                $"bench: {SEntMan.GetComponent<TransformComponent>(bench).Coordinates}; " +
                $"attached: {Pair.Player?.AttachedEntity}; " +
                $"state: {SEntMan.GetComponent<Content.Shared.Mobs.Components.MobStateComponent>(person).CurrentState}");
            SEntMan.System<SharedTransformSystem>().SetCoordinates(person,
                new EntityCoordinates(mapUid, new Vector2(3.6f, 0.5f)));
        });
        await Pair.RunSeconds(30);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<LatheComponent>(bench).CurrentRecipe, Is.Not.Null,
                "Walking away must pause the craft instead of finishing or losing it.");
            Assert.That(SEntMan.GetComponent<AmbientSoundComponent>(bench).Enabled, Is.False);
            Assert.That(SEntMan.System<ScrapWorkbenchSystem>().IsAttended(
                (bench, SEntMan.GetComponent<ScrapWorkbenchComponent>(bench))), Is.False);
            SEntMan.System<SharedTransformSystem>().SetCoordinates(person,
                new EntityCoordinates(mapUid, new Vector2(2.5f, 0.5f)));
        });
        await Pair.RunSeconds(1);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.GetComponent<AmbientSoundComponent>(bench).Enabled, Is.True));
        await Pair.RunSeconds(21);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<LatheComponent>(bench).CurrentRecipe, Is.Null);
            Assert.That(SEntMan.GetComponent<AmbientSoundComponent>(bench).Enabled, Is.False);
            Assert.That(SEntMan.EntityQuery<MetaDataComponent, TransformComponent>()
                .Count(e => e.Item1.EntityPrototype?.ID == "Screwdriver" && e.Item2.MapUid == mapUid), Is.EqualTo(1));
            Server.PlayerMan.SetAttachedEntity(Pair.Player, null);
            SEntMan.DeleteEntity(mapUid);
        });
    }
}
