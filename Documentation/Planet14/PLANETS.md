# Planet recipes and NT announcements

## Generate a planet

Recipes live in `Resources/Prototypes/_Planet14/planets.yml`. From the server console,
or as a Mapping administrator, run:

```text
planet14_planet Planet14Starter
planet14_planet Planet14Barren 1234
```

The normal Planet 14 round automatically generates and uses `planet14.starter_planet`
(default `Planet14Starter`). These commands are for creating additional planets manually.

Each command creates a **new map** and prints its map ID. Visit it with the existing
mapping/teleport tools. Generation loads terrain around players as they explore; these
are SS14 procedural map planes, not finite spherical worlds. The command does not
overwrite existing maps, assign spawn points, create travel connections, or save worlds
automatically. Planet recipes work without enabling the sector clock.

The seed argument overrides the recipe's seed. Without either, the existing generator
chooses a random seed. Use a fixed seed to reproduce terrain. Seeds range from 0 to
2147483647. Restart the server after editing recipes; existing generated planets retain
their lighting settings. Generate a new planet to test changed recipes.

## Add your own recipe

Copy this into a `.yml` file under `Resources/Prototypes/_Planet14/`, giving it a unique ID:

```yaml
- type: planet14Planet
  id: MyForestPlanet
  name: My forest planet
  biome: Grasslands
  seed: 42
  gravity: true
  ambientLight: "#D8B059"
  dayNight:
    enabled: true
    duration: 2400
    offset: 1200
    minLightLevel: 0.08
    maxLightLevel: 3
    clipLight: 1.25
```

Then run `planet14_planet MyForestPlanet`. Existing biomes include `Grasslands`,
`Continental`, `LowDesert`, `Snow`, `Caves`, `Lava`, `Shadow`, and `Asteroid`.
The current planet setup supplies breathable air using SS14's normal planet defaults.

| Setting | Meaning |
| --- | --- |
| `biome` | A `biomeTemplate` defining ground, rocks, trees, ores, decals, and other generated entities. |
| `seed` | Optional reproducible generation seed. |
| `gravity` | Enables inherent planetary gravity; defaults to true. |
| `ambientLight` | Base outdoor light color, before the lighting curve is applied. |
| `dayNight.enabled` | False gives fixed ambient lighting and no rotating sun shadows. |
| `dayNight.duration` | Seconds for a complete day/night cycle; at least one second. Default 1800. |
| `dayNight.offset` | Phase offset relative to SS14's round clock. Zero is midnight; half the duration is midday. |
| `dayNight.minLightLevel` | Minimum nighttime intensity; defaults to 0.08. |
| `dayNight.maxLightLevel` | Light-curve peak before clipping; defaults to 3. Must be at least the minimum. |
| `dayNight.clipLight` | Maximum applied light intensity; defaults to 1.25. |

`minLevel`, `maxLevel`, and `clipLevel` also accept SS14 colors to tune the existing
dawn/dusk color curve. Sun-shadow duration and offset follow the light cycle automatically.
Night/day timing uses **simulation time**, including normal game pauses. It is independent
of the real-life 15-day sector countdown. Set `enabled: false` and choose `ambientLight`
for a permanently bright or dark world.

## Customize terrain generation

Copy `Planet14StarterTerrain` in the example file and point your recipe's `biome` at
the copied ID. No C# changes are needed. The example demonstrates a grass base, dirt
patches, sparse trees, sparse rocks, and occasional mineable mountains.

The final `BiomeMetaLayer` in `Planet14StarterTerrain` masks the
`Planet14StarterMountains` template into small outcrops. Its `threshold: 0.82`
controls rarity (higher means fewer); `frequency: 0.065` controls footprint size
(higher means smaller). Inside that template, ordinary `WallRock` is the fallback,
with separate small pockets of `WallRockTin` (steel), `WallRockCoal`, and
`WallRockQuartz` (glass). Raise an ore layer's threshold to make that resource rarer.
The mountain mask confines every ore pocket to rock formations and takes precedence
over trees. The round's reserved arrival clearing remains free of mountains.

- `BiomeTileLayer` chooses ground tiles. The last matching tile layer wins, so put
  fallback ground first and terrain patches after it.
- `BiomeEntityLayer` places entities on the listed `allowedTiles`. Change `entities`
  to existing tree, rock, ore-wall, or creature prototype IDs. Creature layers can make
  a planet dangerous immediately; the starter example contains no hostile spawns.
- `BiomeDecalLayer` adds visual vegetation or other decals.
- `BiomeMetaLayer` mixes other biome templates for larger regional variation.
- Raising a noise `threshold` makes matching areas rarer. Lowering `frequency` generally
  makes broader patches. Noise `seed` values separate layers; the planet seed determines
  the world's layout.

Use `Resources/Prototypes/Procedural/biome_templates.yml` and the nearby ore templates
for additional working layer examples. Tile/entity names must refer to valid prototypes.
Map generation validates the planet's biome, seed, and light settings before creating
a map; later generation errors are logged and an incomplete map is queued for removal.

## NT daily broadcasts

The sector clock starts automatically when the first round begins. Nanotrasen Sector Relief
broadcasts the sector day in global announcement chat with the normal announcement cue.
Messages are in `Resources/Locale/en-US/_Planet14/announcements.ftl`; edit them to change
the wording without C# changes. The sender is `planet14-nt-announcer`. Message IDs are
`planet14-nt-day-1` through `planet14-nt-day-15`. The `day` and `daysUntilArrival` variables
are available, with arrival fixed to day 14.

```toml
[planet14]
enabled = true
day_length_seconds = 86400
day_announcements = true
day_announcement_sound = true
```

Day 1 encourages survivors and warns against unstable Black Space. Days 4, 11, and 12
explain stabilization, the last return window, and permanent closure. Days 13–15 cover
evacuation preparations and the final day. Announcements describe the intended sector
schedule; physical evacuation and Black Space travel mechanics are still separate work.

There is one broadcast per sector day, not per login. An empty server still advances,
but holds the current day's broadcast until someone connects, so automatic startup
does not waste the welcome message. After an outage,
only the current day's message is eligible, never a backlog of missed days. A collapsed
sector sends no new daily broadcast.

Markers under `planet14/announcements/<cycle UTC ticks>/` prevent duplicates across
restarts. Back these up with the cycle file. They are written before dispatch: a crash
between marking and sending can lose a message, but cannot repeat it. A storage error
is logged and suppresses that broadcast without stopping the sector clock. Archiving
the cycle and creating a new one uses a new marker namespace automatically.
