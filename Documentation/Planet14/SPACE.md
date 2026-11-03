# Basirath space and planets

Planet 14 rounds now create **Basirath sector space**, a separate vacuum map with
the normal space background. It is also available as an FTL destination. The
starter planet and the configured additional planet recipes receive orbital
markers there.

Use the alternative actions on your shuttle console (right-click menu):

- **Ascend to space** moves the entire undocked ship, including its passengers,
  out of a Planet 14 surface map. It stops ship velocity and places the ship near
  that planet's marker without overlapping another grid.
- **Descend to [planet]** appears when your ship is within **50 tiles of the
  planet marker's centre**. The server checks this distance again when used.
  Landing reserves an unoccupied patch of terrain, away from the starter clearing
  and other ships. It preserves buildings and avoids natural obstacles already
  generated there. Docked ships and ships currently in FTL cannot change altitude.

The normal FTL console also requires the ship's centre to be within 50 tiles of
a planet's orbital marker before FTL to that surface is allowed. Departures from
a surface use that planet's fixed orbital position as the range origin on the
space map, including the displayed range bubble and map centring. Travelling far
across the surface cannot extend the ship's reach in space. Direct FTL between
different planetary surfaces is blocked; travel through sector space first.

The temporary planet artwork is a 64×64 sprite scaled to **10×10 world tiles**.
Markers use transparent backing tiles; their artwork is drawn in the world
background pass after parallax and before all grids. It follows world rotation,
without billboard rotation. They have no
physical collision, so ships can fly over them without hiding ship flooring. Their names and
blue IFF labels use the planet recipe's name. Replace the image in
`Resources/Textures/_Planet14/Space/planet.rsi/` when final planet sprites are ready.

Every entry into space or a registered planet shows a seven-second top-centred
banner with a short fade and slide, the Press Start 2P pixel font, measured line
spacing, wrapping, and a panel sized to its content. Planet banners show the name, biome name and
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
12% chance of a ruined salvage hull, otherwise a 30% chance of a mineable
asteroid. Most chunks are empty. Wrecks generate rooms and corridors with damaged
flooring, plating, lattice, wall sections, girders, grilles, broken grilles,
graffiti, burns, scattered salvage and closed supply crates. Asteroids supply
common ores. Debris is kept away from planet markers; wreck and asteroid grid
outlines and labels are hidden from scanner displays. Both remain visible in the
world and can still be collided with or explored.

Wreck floors carry visible burns, blood spatters and green chemical stains away
from loot piles. These stains are decorative. By default, 30% of wrecks have
one or two hostile space carp nearby; `wreckCarpChance` controls this chance.

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
