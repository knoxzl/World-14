using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._Planet14.Planets;
using Content.Server._Planet14.StoneFurnace;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class StoneFurnaceTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Fresh = true, Destructive = true };

    [Test]
    public async Task FurnaceConsumesFuelAndSmeltsSlowlyWithoutPower()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<Planet14PlanetSystem>().TryCreate("Planet14Starter", 14,
                out var map, out var message), Is.True, message);
            var mapUid = SEntMan.System<SharedMapSystem>().GetMapOrInvalid(map);
            var coords = new EntityCoordinates(mapUid, Vector2.Zero);
            var uid = SEntMan.SpawnEntity("Planet14StoneFurnace", coords);
            var component = SEntMan.GetComponent<StoneFurnaceComponent>(uid);
            var system = SEntMan.System<StoneFurnaceSystem>();
            var stacks = SEntMan.System<SharedStackSystem>();
            var ore = SEntMan.SpawnEntity("SteelOre", coords);
            var oreStack = SEntMan.GetComponent<StackComponent>(ore);
            stacks.SetCount((ore, oreStack), 6);
            Assert.That(system.TryInsert((uid, component), (ore, oreStack), out var inserted), Is.True);
            Assert.That(inserted, Is.EqualTo(6));

            // No fuel means no progress or output, however long we wait.
            system.Update(60);
            Assert.That(component.Progress, Is.Zero);
            Assert.That(component.Ores.Values.Sum(), Is.EqualTo(6));
            var coal = SEntMan.SpawnEntity("Coal1", coords);
            Assert.That(system.TryInsert((uid, component), (coal, SEntMan.GetComponent<StackComponent>(coal)), out _), Is.True);
            system.Update(19);
            Assert.That(component.Ores.Values.Sum(), Is.EqualTo(6));
            system.Update(1);
            Assert.That(component.Ores.Values.Sum(), Is.EqualTo(5));
            Assert.That(component.Coal, Is.Zero);
            Assert.That(component.RemainingHeat, Is.EqualTo(4));
            for (var i = 0; i < 4; i++)
                system.Update(20);
            system.Update(60);
            Assert.That(component.Ores.Values.Sum(), Is.EqualTo(1), "One coal must smelt exactly five pieces.");
            Assert.That(component.RemainingHeat, Is.Zero);

            var output = SEntMan.EntityQuery<StackComponent, MetaDataComponent, TransformComponent>()
                .Where(e => e.Item2.EntityPrototype?.ID == "SheetSteel1" && e.Item3.MapUid == mapUid)
                .Sum(e => e.Item1.Count);
            Assert.That(output, Is.EqualTo(5));

            var wood = SEntMan.SpawnEntity("MaterialWoodPlank1", coords);
            Assert.That(system.TryInsert((uid, component), (wood, SEntMan.GetComponent<StackComponent>(wood)), out _), Is.False);
            var quartz = SEntMan.SpawnEntity("SpaceQuartz1", coords);
            Assert.That(system.TryInsert((uid, component), (quartz, SEntMan.GetComponent<StackComponent>(quartz)), out _), Is.True);
            SEntMan.DeleteEntity(mapUid);
        });
    }
}
