# Stone furnace

Build **stone furnace** from the construction menu using 30 raw iron ore
(`SteelOre`), 15 wood planks, and 5 coal. Construction coal is consumed as part
of the recipe; load additional coal to run the finished furnace.

Click the furnace with coal or a supported raw-material stack to load it.
It accepts up to 50 coal and 100 ore pieces, leaving excess in your hand.
It automatically starts when it has both fuel and ore, without electricity.
Examine it to see stored coal, queued ore, and remaining smelts from burning fuel.

Each ore piece takes 20 seconds of simulation time and produces one refined
piece on the floor beside the furnace. One coal smelts five ore pieces.
It stops when it runs out of fuel or ore, retaining partial progress and unused
fuel capacity. Pausing the map pauses smelting. Destroying the furnace loses
its internal supplies.

| Raw material | Output |
| --- | --- |
| Iron ore | Steel sheet |
| Quartz | Glass sheet |
| Gold ore | Gold ingot |
| Silver ore | Silver ingot |
| Plasma ore | Plasma sheet |
| Uranium ore | Uranium sheet |

Edit `Resources/Prototypes/_Planet14/stone_furnace.yml` to change the construction
cost or the `StoneFurnace` component's `recipes`, `smeltSeconds`, `itemsPerCoal`,
`fuelCapacity`, and `oreCapacity` settings. Recipe outputs must be single-piece
entity prototypes.

The furnace uses Knoxzl's 64×64 sprites. The fire animation repeats frames
1, 2, 3, 2, then returns to 1, at 0.2 seconds per frame. It is visible only
while smelting.
