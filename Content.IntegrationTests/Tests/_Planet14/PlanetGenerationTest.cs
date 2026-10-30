using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server._Planet14.Planets;
using Content.Server.Parallax;
using Content.Shared._Planet14.Planets;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class PlanetGenerationTest : GameTest
{
    [Test]
    public async Task StarterMountainsAreSparseAndContainOrePockets()
    {
        await Server.WaitAssertion(() =>
        {
            var planets = SEntMan.System<Planet14PlanetSystem>();
            Assert.That(planets.TryCreate("Planet14Starter", 14, out var map, out var message), Is.True, message);
            var uid = SEntMan.System<SharedMapSystem>().GetMapOrInvalid(map);
            try
            {
                var biome = SEntMan.GetComponent<BiomeComponent>(uid);
                var generation = SEntMan.System<BiomeSystem>();
                var mountains = 0;
                var stone = 0;
                var ores = new HashSet<string>();
                for (var x = -64; x < 64; x++)
                for (var y = -64; y < 64; y++)
                {
                    if (!generation.TryGetEntity(new Vector2i(x, y), biome,
                            (Entity<MapGridComponent>?) null, out var entity) || !entity.StartsWith("WallRock"))
                        continue;

                    Assert.That(SProtoMan.HasIndex<EntityPrototype>(entity), Is.True);
                    mountains++;
                    if (entity == "WallRock")
                        stone++;
                    else
                        ores.Add(entity);
                }

                Assert.That(mountains, Is.InRange(50, 1638), "Outcrops should occupy less than 10% of the sampled planet.");
                Assert.That(stone, Is.GreaterThan(mountains / 2), "Most mountain tiles should be ordinary rock.");
                Assert.That(ores, Is.EquivalentTo(new[] { "WallRockTin", "WallRockCoal", "WallRockQuartz" }));
                TestContext.Out.WriteLine($"Starter mountains: {mountains}/16384 tiles, {stone} plain rock tiles.");
            }
            finally
            {
                SEntMan.DeleteEntity(uid);
            }
        });
    }

    [TestCase("Planet14Starter", 14, 1800)]
    [TestCase("Planet14Barren", 1234, 3600)]
    public async Task RecipeCreatesPlanetWithConfiguredLighting(string recipe, int seed, int duration)
    {
        await Server.WaitAssertion(() =>
        {
            var planets = SEntMan.System<Planet14PlanetSystem>();
            var maps = SEntMan.System<SharedMapSystem>();
            Assert.That(planets.TryCreate(recipe, seed, out var mapId, out var message), Is.True, message);
            var uid = maps.GetMapOrInvalid(mapId);
            try
            {
                Assert.That(SEntMan.HasComponent<MapGridComponent>(uid), Is.True);
                var generatedBiome = SEntMan.GetComponent<BiomeComponent>(uid);
                Assert.That(generatedBiome.Seed, Is.EqualTo(seed));
                var template = SProtoMan.Index(SProtoMan.Index<Planet14PlanetPrototype>(recipe).Biome);
                Assert.That(SEntMan.System<BiomeSystem>().TryGetTile(Vector2i.Zero, template.Layers, seed,
                    (Entity<MapGridComponent>?) null, out var tile), Is.True);
                Assert.That(tile!.Value.IsEmpty, Is.False);
                Assert.That(SEntMan.GetComponent<GravityComponent>(uid).Enabled, Is.True);
                var light = SEntMan.GetComponent<LightCycleComponent>(uid);
                var shadows = SEntMan.GetComponent<SunShadowCycleComponent>(uid);
                Assert.That(light.Duration, Is.EqualTo(TimeSpan.FromSeconds(duration)));
                Assert.That(light.InitialOffset, Is.False);
                Assert.That(shadows.Duration, Is.EqualTo(light.Duration));
                Assert.That(shadows.Offset, Is.EqualTo(light.Offset));
                Assert.That(SharedLightCycleSystem.CalculateLightLevel(light, duration / 2f),
                    Is.GreaterThan(SharedLightCycleSystem.CalculateLightLevel(light, 0f)));
            }
            finally
            {
                SEntMan.DeleteEntity(uid);
            }
        });
    }

    [Test]
    public async Task BadRecipesAndSeedsCreateNoMap()
    {
        await Server.WaitAssertion(() =>
        {
            var planets = SEntMan.System<Planet14PlanetSystem>();
            Assert.That(planets.TryCreate("Planet14DoesNotExist", null, out var invalidMap, out _), Is.False);
            Assert.That(invalidMap, Is.EqualTo(MapId.Nullspace));
            Assert.That(planets.TryCreate("Planet14Starter", -1, out invalidMap, out _), Is.False);
            Assert.That(invalidMap, Is.EqualTo(MapId.Nullspace));
        });
    }

    [Test]
    public async Task EveryConfiguredRecipeReferencesAValidBiome()
    {
        await Server.WaitAssertion(() =>
        {
            foreach (var planet in SProtoMan.EnumeratePrototypes<Planet14PlanetPrototype>())
            {
                Assert.That(SProtoMan.HasIndex(planet.Biome), Is.True, planet.ID);
                Assert.That(planet.DayNight.IsValid, Is.True, planet.ID);
            }
        });
    }

    [Test]
    public async Task DailyBroadcastsFormatForEverySectorDay()
    {
        await Server.WaitAssertion(() =>
        {
            var loc = Server.ResolveDependency<ILocalizationManager>();
            Assert.That(loc.TryGetString("planet14-nt-announcer", out _), Is.True);
            for (var day = 1; day <= 15; day++)
            {
                Assert.That(loc.TryGetString($"planet14-nt-day-{day}", out var text,
                    ("day", day), ("daysUntilArrival", Math.Max(0, 14 - day))), Is.True);
                Assert.That(text, Does.Contain($"DAY {day}"));
            }
            Assert.That(loc.GetString("planet14-nt-day-11", ("daysUntilArrival", 3)), Does.Contain("DAY 12"));
            Assert.That(loc.GetString("planet14-nt-day-12", ("daysUntilArrival", 2)), Does.Contain("CLOSED"));
        });
    }
}
