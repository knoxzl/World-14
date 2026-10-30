using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._Planet14.Planets;
using Content.Server.GameTicking;
using Content.Shared._Planet14;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Roles.Jobs;
using Content.Shared.Storage;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class SurvivorRoundTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        InLobby = true,
        Fresh = true,
        Destructive = true,
    };

    [TestCase(false)]
    [TestCase(true)]
    public async Task RoundStartAndLateJoinSpawnSurvivorsOnPlanet(bool lateJoin)
    {
        await Server.WaitAssertion(() =>
        {
            var config = Server.ResolveDependency<IConfigurationManager>();
            config.SetCVar(Planet14CVars.Enabled, true);
            Planet14RoundSystem.Configure(config);
            var ticker = SEntMan.System<ServerGameTicker>();
            ticker.SetGamePreset("Planet14");
            ticker.ToggleReady(Pair.Player, !lateJoin);
            ticker.StartRound(true);
            Assert.That(ticker.CurrentPreset!.ID, Is.EqualTo("Planet14"));
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

            if (lateJoin)
                ticker.MakeJoinGame(Pair.Player, EntityUid.Invalid, "Captain");

            Assert.That(Pair.Player.AttachedEntity, Is.Not.Null);
            var mob = Pair.Player.AttachedEntity!.Value;
            var transform = SEntMan.GetComponent<TransformComponent>(mob);
            Assert.That(transform.MapID, Is.EqualTo(ticker.DefaultMap));
            Assert.That(SEntMan.HasComponent<BiomeComponent>(transform.MapUid), Is.True);
            var mind = SEntMan.GetComponent<MindContainerComponent>(mob).Mind;
            Assert.That(SEntMan.System<SharedJobSystem>().MindTryGetJob(mind, out var job), Is.True);
            Assert.That(job!.ID, Is.EqualTo("Survivor"));

            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryGetSlotEntity(mob, "back", out var bag), Is.True);
            var contents = SEntMan.GetComponent<StorageComponent>(bag!.Value).Container.ContainedEntities
                .Select(e => SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype!.ID).ToArray();
            Assert.That(contents, Is.SupersetOf(new[]
            {
                "MedkitFilled", "BoxSurvival", "MiningDrill", "FoodSnackNutribrick",
                "DrinkWaterBottleFull", "Machete", "FlashlightLantern",
            }));
            Assert.That(inventory.TryGetSlotEntity(mob, "belt", out var belt), Is.True);
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(belt!.Value).EntityPrototype!.ID,
                Is.EqualTo("ClothingBeltUtilityFilled"));
            var tools = SEntMan.GetComponent<StorageComponent>(belt.Value).Container.ContainedEntities
                .Select(e => SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype!.ID).ToArray();
            Assert.That(tools, Is.SupersetOf(new[]
            {
                "Crowbar", "Wrench", "Screwdriver", "Wirecutter", "Welder", "NetworkConfigurator",
            }));
            SEntMan.System<SharedTransformSystem>().SetCoordinates(mob,
                new EntityCoordinates(transform.MapUid!.Value, new Vector2(0.5f, 0.5f)));
        });

        await Pair.RunTicksSync(30); // Warm generation around the connected player.
        var timer = Stopwatch.StartNew();
        await Pair.RunTicksSync(60);
        TestContext.Out.WriteLine($"Planet 14 paired simulation: {timer.Elapsed.TotalMilliseconds / 60:F2} ms per tick after warmup.");

        await Pair.RunUntilSynced();
        await SetMoveKey(BoundKeyState.Down);
        await Pair.RunTicksSync(15);
        await SetMoveKey(BoundKeyState.Up);
        await Pair.RunTicksSync(20);
        await Pair.RunUntilSynced();
        var position = Vector2.Zero;
        await Server.WaitAssertion(() =>
        {
            position = SEntMan.GetComponent<TransformComponent>(Pair.Player.AttachedEntity!.Value).LocalPosition;
            Assert.That(position.X, Is.GreaterThan(1f), "The survivor must be able to walk on the planet.");
        });
        await Client.WaitAssertion(() =>
        {
            var predicted = CEntMan.GetComponent<TransformComponent>(Client.Session!.AttachedEntity!.Value).LocalPosition;
            Assert.That(Vector2.Distance(predicted, position), Is.LessThan(0.15f),
                "Client and server positions must converge after movement stops.");
        });
    }

    private async Task SetMoveKey(BoundKeyState state)
    {
        await Client.WaitPost(() =>
        {
            var key = EngineKeyFunctions.MoveRight;
            var input = Client.ResolveDependency<Robust.Client.Input.IInputManager>();
            var mob = Client.Session!.AttachedEntity!.Value;
            var command = new ClientFullInputCmdMessage(CGameTiming.CurTick, CGameTiming.TickFraction,
                input.NetworkBindMap.KeyFunctionID(key))
            {
                State = state,
                Coordinates = CEntMan.GetComponent<TransformComponent>(mob).Coordinates,
                Uid = mob,
            };
            CEntMan.System<Robust.Client.GameObjects.InputSystem>().HandleInputCommand(Client.Session, key, command);
        });
    }
}
