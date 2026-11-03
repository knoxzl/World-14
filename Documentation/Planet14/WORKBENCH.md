# Scrap workbench

Build **scrap workbench** in the G menu with **25 wood planks and 10 steel sheets**.
Load steel, glass, and wood like a normal lathe, open its crafting menu, and queue
recipes. It needs no electricity. A connected, living player must remain within
three tiles of its centre for work to progress. Anyone nearby can supervise it;
leaving pauses the current item and returning resumes it without consuming more
materials. Ghosts, dead bodies, and disconnected characters do not supervise work.
The fabricator sound loops during attended crafting, stops while work is paused,
and resumes when someone returns. It also stops when the crafting queue finishes.

Basic tools take 25–45 seconds each. LV, MV and HV cable take 10 seconds per piece.
The bench also makes a circuit imprinter board (120 seconds), manipulators
(40 seconds), and beakers (30 seconds). These are separate, more expensive recipes
than the powered machines use; ordinary SS14 recipes remain unchanged.

To build the first **circuit imprinter**:

1. Make one circuit imprinter machine board, two manipulators, and two beakers.
2. Use the normal G-menu machine frame recipe with five steel sheets.
3. Anchor the frame with a wrench and add one LV cable.
4. Insert the board, both manipulators and both beakers, then finish with a screwdriver.
5. Supply normal electrical power to the finished circuit imprinter.

The bench can make the cable, wrench, screwdriver, and crowbar needed for assembly
and maintenance. Its board recipe costs 30 steel sheets, 20 glass sheets and five
wood planks. The starter planet's iron and quartz can be refined in the stone furnace
to supply this path; no pre-existing circuit printer or plastic is required.

Recipes, timings and construction costs are in
`Resources/Prototypes/_Planet14/scrap_workbench.yml`. The attendance distance is the
`ScrapWorkbench.attendanceRange` setting. The sprite is Knoxzl's original 32×32 PNG.
