# Stone furnace

Build **stone furnace** from the construction menu using 30 raw iron ore
(`SteelOre`), 15 wood planks, and 5 coal. Construction coal is consumed as part
of the recipe; load additional coal to run the finished furnace.

Click the furnace with coal or a supported raw-material stack to load it.
It accepts up to 50 coal and 100 ore pieces, leaving excess in your hand.
It automatically starts when it has both fuel and ore, without electricity.
Examine it to see stored coal, queued ore, and remaining smelts from burning fuel.

Each batch takes 20 seconds of simulation time and smelts up to 15 pieces of
one ore type, dropping one refined piece per ore on the floor beside the furnace.
Smaller batches run when less ore or fuel is available. One coal smelts five ore
pieces, so a full batch costs three coal. Batch contents and fuel are reserved
when processing starts; loading more ore does not expand a running batch.
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
`batchSize`, `fuelCapacity`, and `oreCapacity` settings. Recipe outputs must be single-piece
entity prototypes.

The furnace uses Knoxzl's 64×64 sprites. The fire animation repeats frames
1, 2, 3, 2, then returns to 1, at 0.2 seconds per frame. It is visible only
while smelting. A warm orange light and the campfire ambience also run only
while the furnace is working.
