# Planet 14

Planet 14 is a persistent survival sandbox in the dying Basirath sector. Players start
on a planet without assigned jobs, faction membership, or objectives. They collect
materials, build ships, explore planets, establish factions, and construct settlements
and stations. The sector survives for fifteen real-life days. Only those who reach
an evacuation pod escape its collapse.

## This first implementation

This change supplies an **automatically started sector clock**, durable UTC origin, scheduled phases,
four-minute pulse events, NT daily chat announcements, configurable generated planets,
a Planet 14 round preset, automatic starter-planet spawning, a Survivor role and kit,
console commands, automated tests, and the setting below.
It does **not** yet supply a complete campaign, character resets,
Black Space traversal, audio, an evacuation ship, pods, capacity calculation, Discord
integration, or destructive collapse. Phase names describe the schedule; they do not
claim that a physical ship has spawned or that a portal currently works.

See [Planet recipes and announcements](PLANETS.md) to add planets, customize terrain
and day/night cycles, or edit the daily broadcasts.

When Planet 14 is enabled, it forces its own preset, starter map, and Survivor role.
Set `planet14.enabled = false` before starting to use ordinary SS14 presets instead.
Existing map persistence is not a complete solution for persistent worlds and offline characters.

### Running the clock

The sector clock is enabled by default. Start the server normally:

```text
dotnet run --project Content.Server --configuration Release --no-build
```

The lobby appears first. When the round starts, the starter planet is generated,
players spawn as Survivors, and the sector clock automatically creates its first save.
Later rounds resume that same cycle. No `planet14_start` command or `--cvar` flag is required.
The shipped server configuration includes these defaults:

```toml
[planet14]
enabled = true
day_length_seconds = 86400
starter_planet = "Planet14Starter"
```

`planet14_status` is available to everyone. `planet14_start` remains a Server-admin
recovery command if initial save creation failed; it never replaces an existing cycle.
In a fresh lobby, status says that the clock is waiting for the round to begin.
Starting writes `planet14/sector-cycle.json` under the server's user-data directory
before activating the clock. Normal round restarts do not reset it. An existing cycle,
including a collapsed cycle, is never automatically replaced.
Set `enabled = false` in the `[planet14]` section before startup to disable the clock.
An explicit false in an older configuration still overrides the new default.

### Survivor spawning and kit

Round-start and late-joining players spawn on the configured starter planet as Survivors,
regardless of their old station job preferences. A clear arrival area prevents terrain
objects from appearing inside freshly spawned players. The Survivor backpack contains
gauze, bruise packs, ointment, a crowbar, wrench, screwdriver, wirecutters, welder,
multitool, mining drill, a nutribrick, and water. Clothing, shoes, a PDA, and headset are
also supplied. Edit `Resources/Prototypes/_Planet14/round.yml` to change the kit.

The starter round uses a minimal station record solely for normal SS14 job/ID bookkeeping.
It does not load the development station's cargo shuttles, wreck fields, or station event
schedulers. Terrain loads as players explore. Test movement after compilation finishes:
running a full build on the server's machine competes for CPU and can stall simulation.

For testing, set `day_length_seconds = 60` **before starting a new cycle**. A complete
cycle then takes fifteen minutes. All intra-day deadlines and pulses scale equally.
Supported day lengths are 60–86400 real seconds. A saved cycle retains its day length
even if configuration changes. Enabling/disabling the system requires a server restart.

To start another development cycle, stop the server and archive its clock file first.
There is deliberately no command that silently overwrites a running or corrupt cycle.
An unreadable/invalid save is logged and blocks starting; repair or archive it while the
server is stopped. New saves are written to a temporary file, then renamed into place.
Back up the clock file with the world's future persistence data. Each server instance
must use its own user-data directory. Keep the host's UTC clock synchronized; this
implementation uses wall-clock time, not an external trusted time source.

### Schedule

Day 1 starts at the saved UTC origin. Day N starts after N−1 elapsed days. Collapse
occurs after fifteen complete elapsed days, at the end of day 15. Sector age in the lore
is a separate calendar measured in years called cycles.

| Deadline | State |
| --- | --- |
| Days 1–3 | Black Space unstable; entry will be fatal. |
| Start of day 4 | Black Space becomes traversable in both directions. |
| Start of day 12 | Return closes, including for people already inside. |
| Start of day 13 | Evacuation capacity review is due. |
| Start of day 14 | Scheduled NT arrival on Trispic; shield charging begins. |
| Arrival + 2 hours | Doors open; three-hour boarding window begins. |
| Arrival + 5 hours | Twenty-minute final rush begins. |
| Arrival + 5 hours 15 minutes | Final five minutes; exit music is due. |
| Arrival + 5 hours 20 minutes | Scheduled departure. |
| Start of day 15 | Final day for those left behind. |
| End of day 15 | Sector consumed. |

`SectorPhaseChangedEvent` publishes the current day, Black Space phase, and scheduled
evacuation phase. On startup its previous state is null. A stall or outage can skip
several phases: subscribers must reconcile to the current state rather than replaying
obsolete phases. The clock does not depend on lobby, game-pause, or round elapsed time.
The server frame hook also runs it when the simulation auto-pauses on an empty server.
`BlackSpacePulseEvent` provides a monotonically numbered live heartbeat, at most once
per one-second update. Missed pulses are coalesced, never replayed; startup and collapsed
sectors emit none. An audible heartbeat and pulse-driven encounters come later.

The arrival schedule is fixed. Early departure when all pods fill will belong to the
future evacuation state machine; it must persist its departure and reservations rather
than infer actual ship state from this schedule.

### Verification

Initialize engine dependencies with `git submodule update --init --recursive`, then run:

```text
dotnet test Content.Tests/Content.Tests.csproj --filter FullyQualifiedName~Content.Tests.Server._Planet14
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~PlanetGenerationTest
```

On a development server, check `help planet14_status` and `help planet14_start`.
With the clock disabled, starting must fail without creating a save. Start a server on a
fresh development user-data directory with a 60-second day: status should wait in the lobby,
then automatically begin at day 1 when the round starts. A second clock start must fail,
and day 4 must begin three minutes after the first round begins.
Leave it empty: the server log must still report day/phase transitions while auto-paused.
Restart using the same directory and a different configured day length: the saved
origin and original day length must still apply. Corrupt a **development copy** of
the save while stopped: startup should log the error and refuse a new start without
modifying that file. Use a separate user-data directory for each independent test.

## Agreed gameplay rules for subsequent changes

- New and reset characters receive basic clothing, a gathering tool, and a little food.
- The starter planet is forgiving early and becomes progressively more hostile.
- Leaving independently requires building a small ship from gathered/refined materials.
- Characters remain present and vulnerable while offline.
- Ordinary death restores a person to the starter planet in the present, without their
  equipment or memories. Their old possessions remain in the world. No former loyalties,
  base knowledge, or grudges survive the reset; staff enforce character amnesia.
- After return closes on day 12, death inside Black Space is permanent for that cycle.
- Raiding, betrayal, and combat require an in-character reason. Unmotivated killing
  for amusement is against the rules. Factions are created by players.
- On day 13, privately message the owner through a Discord bot. Suggest pods for 25% of
  unique accounts with at least two hours played during days 10–12, rounded up, minimum
  one. The owner may override with a positive integer before arrival. Lock capacity
  at arrival; use the suggestion if no override arrives. Discord failure cannot block
  the default. Owner identity and delivery destination need deployment configuration.
- The indestructible evacuation ship lands on infested Trispic, in the sector's centre.
  It deploys four turrets and charges a shield for two hours, with doors closed. The
  shield then activates and boarding follows the schedule above. Departure is immediate
  if every pod fills. Entering a pod guarantees survival, reserves its seat through
  disconnects, and ends that character's participation. Secured occupants cannot be harmed.
- Evacuated and permanently dead players may spectate but cannot rejoin that cycle.
- Day 15 is for prayer, reflection, last meetings, or unfinished business. The end of
  that day consumes the sector. A future cycle reset must also reset persisted worlds.

The first playable target is a small complete loop: starter planet, Trispic facility
planet, compact Mayland expedition area, gathering/refining, basic ship and base
construction, death resets, pulse encounters, evacuation, and collapse. More planets,
alien technologies, and broader station-building progression follow that milestone.

## Basirath sector history

Basirath is classified by the Syndicate as **Class A Instability** and **Class B
Biohazard**. Ordinary travel into or out of the sector is prohibited. Ships attempting
to leave are destroyed for fear that something aboard might escape containment.

The Gasian Facility on planet **Trispic** was once a celebrated research and teaching
institution, home to brilliant scientists, students, and teachers. Its reputation was
so strong that Nanotrasen negotiated with the Syndicate to send some of its students.

Researchers seeking a frontier beyond Bluespace and Redspace discovered another
dimensional space overlapping their own. They called it **Black Space**. Basirath's
residents named the dying world beyond it **Mayland**. After two years of development,
the facility promised officials that a semi-stable portal would be safe.

On **29 October 2126, Cycle 12**, the portal opened successfully. Convicts were sent
through during its first year of operation. For months there seemed to be nothing
but barren space and old asteroids. The opening reached only the overlap between
realities and was too small to send a shuttle through safely.

Near the end of that first year, scanners found something constructed. It was alien,
almost perfect, yet riddled with mistakes. Five days of tracking revealed that it was
approaching the portal. Estimated arrival: roughly one and a half years. Months of
signals received no reply.

Syndicate officials ordered the portal closed. The researchers lied and continued
their work. As the object approached, they established **Frontier**, a station on the
other side, to await it.

The arriving ship dwarfed anything they had seen. Its hull was lined with a black,
semi-reflective surface. It seemed dead. Expeditions found only the bodies of otherworldly
beings. For roughly half a year they studied remains their instruments could not
properly examine.

On **26 October 2129, Cycle 15**, scanners detected life aboard the ship. An expedition
was dispatched. Twelve hours later its members were dead. The creature was still
moving, rapidly approaching the portal. Researchers cut the power, but the portal
remained open, sustaining itself. Personnel evacuated and a task force assembled.

On **29 October 2129**, the creature emerged: an amoeba-like shape, fully black with a
faint green reflection. It paused, apparently processing its surroundings, then attacked.
Twenty-nine people died. By **30 October**, it had been killed.

The Syndicate imposed biological containment. Nanotrasen withdrew its educational
agreement and blamed the Syndicate. Two days later, a horde rushed through the portal.
Readings climbed beyond expectations. The breach was decaying, yet something sustained
it, and it was growing. Every four minutes came a deep, audible heartbeat.

The remaining researchers predicted that the breach would consume Basirath within
fifteen days. The Syndicate abandoned the sector to its quarantine. Nanotrasen announced
an evacuation in two weeks, using Bluespace to bypass the blockade. The Syndicate
considered the attempt absurd but preferred to watch NT fail rather than stop it.

There would be limited seats. Publicly, the evacuation was a humanitarian operation.
Privately, NT wanted intelligence and survivors it could exploit as slaves. For the
people trapped in Basirath, it was still their only promised way out.
