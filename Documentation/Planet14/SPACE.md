# Basirath space and planets

Planet 14 rounds now create **Basirath sector space**, a separate vacuum map with
the normal space background. It is also available as an FTL destination. The
starter planet and the configured additional planet recipes receive orbital
markers there.

Use the alternative actions on your shuttle console (right-click menu):

- **Ascend to space** moves the entire undocked ship, including its passengers,
  out of a Planet 14 surface map. It stops ship velocity and places the ship near
  that planet's marker without overlapping another grid.
- **Descend to [planet]** appears when your ship is within **20 tiles of the
  planet marker's centre**. The server checks this distance again when used.
  Landing reserves an unoccupied patch of terrain, away from the starter clearing
  and other ships. It preserves buildings and avoids natural obstacles already
  generated there. Docked ships and ships currently in FTL cannot change altitude.

The temporary planet artwork is a 64×64 sprite scaled to **10×10 world tiles**.
Markers have no physical collision, so ships can fly over them. Their names and
blue IFF labels use the planet recipe's name. Replace the image in
`Resources/Textures/_Planet14/Space/planet.rsi/` when final planet sprites are ready.

Every entry into space or a registered planet shows a seven-second top-of-screen
banner with a short fade and slide. Planet banners show the name, biome name and
description. Space warns that it is cold, airless, and requires a spacesuit.

## Configuration

Planet recipes in `Resources/Prototypes/_Planet14/planets.yml` now accept:

```yaml
biomeName: Temperate grasslands
description: A quiet refuge of forests and mineral outcrops.
spacePosition: 600,300
```

The `Basirath` `planet14Sector` prototype in
`Resources/Prototypes/_Planet14/space.yml` lists planets created for each round.
Additional planets created with `planet14_planet` also acquire orbital markers
while a sector is running. Give them distinct `spacePosition` values; colliding
defaults are moved apart automatically.

## Sparse exploration debris

Space is divided into 256×256 exploration chunks. Each chunk is rolled once:
12% chance of a small broken salvage hull, otherwise a 30% chance of a mineable
asteroid. Most chunks are empty. Wrecks contain common construction salvage;
asteroids supply common ores. Debris is kept away from planet markers.

An initial 5×5 area is generated at round setup. More chunks are generated around
players exploring space, up to `maxChunks` (default 256), to bound generation cost.
These are small procedural wrecks, not the larger hand-mapped ruin collection.

## Planetary meteor showers

Each planet rolls a shower every **30–60 minutes of simulation time** by default.
The `meteorMinSeconds` and `meteorMaxSeconds` settings control this interval.
Showers scatter groups 25–65 tiles from current players (up to three groups), plus
two distant groups 500–3000 tiles from the origin, including on uninhabited worlds.

Animated, falling meteors leave small patches of mineable rock and ore. Deposits
can include gold, silver, uranium, plasma and diamond. Placement skips existing
entities and ship grids; this first version replenishes resources without
exploding players' buildings. Distant patches remain available for exploration.
`maxMeteorRocks` bounds the number of unmined meteor rocks (default 4096);
mining them frees room for later deposits.

All space generation and meteor timers are round-local simulation state. This
does not add world persistence across server restarts; the sector clock continues
to use its existing separate real-time save.
