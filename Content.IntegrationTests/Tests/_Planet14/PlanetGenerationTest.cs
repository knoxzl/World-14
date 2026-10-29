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

namespace Content.IntegrationTests.Tests._Planet14;

public sealed class PlanetGenerationTest : GameTest
{
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
