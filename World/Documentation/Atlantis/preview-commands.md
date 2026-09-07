# Atlantis furnished-interior preview (revision 4)

This revision places **96 arrangements / 6569 items** across the four sparse districts of lower Atlantis. Every room, cell, corridor and niche that was still bare now has its own trade or story, and the repeated motifs of revision 3 were broken up: pools now appear in only a third of the rooms and carry different life (jellyfish of a different species in five rooms, fish, lily pads, an octopus, or drowned bones), and the other rooms get mushrooms, moss, crabs, shells, coral, crystals, hay, garbage or bones instead. Multi-part sprites are always placed whole: no ankh halves, the loom is two pieces, carpets narrower than three tiles use field tiles rather than border pieces, and the bath house walkway is paving rather than an empty carpet frame.

The style rules and palette are in [style-guide.md](style-guide.md). Renders of every arrangement are in `interior-preview-v4-*.jpg`.

## Commands

The decoration lives in **`Data/Atlantis/Decorations.json`**: a `placements` array where each entry is `district, group, art, hue, x, y, z, mode`. The generator writes it, the game reads it, and `[AtlantisSave` writes back into it. Nothing here touches MUL files; every piece is a normal saved world item (`AtlantisPreviewItem`, immovable unless unfrozen).

| Command | Behavior |
| --- | --- |
| `[AtlantisDecor` | Open the palette: every art/hue pair used in Atlantis, six by five per page, with a name or `0x` filter. Tap an item, target a spot, and a movable copy is placed there (on top of whatever you target). The palette reopens after each placement. |
| `[AtlantisDecor preview [district]` | Place every arrangement from the JSON that fits |
| `[AtlantisDecor check [district]` | Report what would be placed without creating items |
| `[AtlantisDecor go [district]` | Travel to the first arrangement of a district |
| `[AtlantisDecor status` | Count placed items by district |
| `[AtlantisDecor clear [district]` | Remove placed items (including anything placed from the palette or saved in game) |
| `[AtlantisSave` | Target two corners; everything between them that is a `Static` or a placed item is written into the JSON as one arrangement named after the corner (`saved_X_Y`), and fixed in place |
| `[AtlantisSave <name>` | Same, with your own arrangement name (letters, digits, underscores) |
| `[AtlantisSave unfreeze [district]` | Make the placed items movable so you can drag them around |
| `[AtlantisSave freeze [district]` | Fix them in place again without saving |

Districts are `north`, `central`, `east`, `south`, or `all` (the default; `go` defaults to north).

### Editing loop

1. `[AtlantisDecor preview` once after a restart to spawn the current JSON.
2. Walk a room. `[AtlantisSave unfreeze` lets you drag pieces; `[AtlantisDecor` places new pieces from the palette; `[Delete` removes; `[Props` changes hue or art.
3. `[AtlantisSave` and target the two corners of the room. The whole of any arrangement the rectangle touches is absorbed and replaced, so partial selections never leave a torn group behind. Teleporters, doors, containers and other typed items are ignored and listed. Placement modes are inferred from the world: floor items, flat passable decals as overlays, raised items as `surface` when something under them supports them and `north`/`west` when a wall backs them; a raised item with neither is skipped and reported. Groups over 300 items or 32 tiles are split automatically.
4. The complete manifest is validated before anything is written. On success the previous file is copied to `Data/Atlantis/Backups/`, the JSON is replaced with `"savedInGame": true`, plain Statics you added become owned items, everything saved is fixed in place, and `status`/`clear`/`preview` keep working without a restart.

Once `savedInGame` is true, `build-atlantis-preview.py` refuses to overwrite the JSON unless run with `--force`, so in-game edits are not lost by re-running the generator. Apply `preview` from outside the rooms; a mobile or unexpected item inside an arrangement causes that arrangement to be skipped and reported, and `preview` never re-places an arrangement that is already present (clear first).

## Arrangements

Centres are bounding-box midpoints for orientation; they are not guaranteed clear tiles. District totals: north 2329; central 1053; east 1668; south 1519.

| District | Arrangement | Centre (X, Y) at Z −127 | Items | What it is |
| --- | --- | --- | ---: | --- |
| North | Tidal archive | 4335, 2526 | 144 | Bookcases and shelves under vines, marble reading tables on an ornate rug and a slate mosaic, a scribe slumped at a writing table, ladder, globe, mushrooms in the damp corner |
| North | City watch armory | 4331, 2563 | 114 | Weapon and armour walls, training dummies, archery target, forge and anvil with a log workbench, crate stacks, blue braziers on a mosaic runner, shells and coral by the ruined pillar |
| North | Navigators chartroom | 4379, 2547 | 80 | Wall maps, writing table with charts, telescope, globe, sextant, ship model, hanging lantern, small fish and kelp by the door |
| North | Officers refectory | 4411, 2547 | 157 | Seven-tile log table with wooden benches on a deep-blue carpet, marble serving counter, kegs and cauldron, windows and curtains, gaming table, hammock and cot in the south bay, a collapsed corner |
| North | Watch barracks | 4363, 2563 | 124 | Five cots with foot lockers (two still occupied), hammocks, a dice table, weapon stand, mosaic medallion, hay, mushrooms in the leaking corner |
| North | Bath house | 4395, 2562 | 276 | Paved walkway around three deep pools joined to a fountain, two jellyfish species, fish, turtle, mermaid and sea-horse statues, marble benches, basins and troughs |
| North | Hall of the leviathan west | 4434, 2539 | 87 | Colonnade on a two-row carpet, banners and tapestries, giant statues, a leviathan skull with rib bones, crystal and shells |
| North | Hall of the leviathan east | 4458, 2539 | 89 | Colonnade continued, sea-serpent statue, skull piles, a second leviathan skull, mushrooms and growth |
| North | Harbour storehouse | 4419, 2575 | 123 | Crate and cargo stacks on a wooden deck, barrels and sacks, hanging nets, anchor, buoy, mast, a clerk's log table, a crushed worker, squid and crabs |
| North | Refectory kitchen | 4382, 2535 | 66 | Hanging cauldrons, log and wood tables of food, water tub, sacks and jars, cook's skeleton, fish heads and mushrooms |
| North | Weavers workshop | 4351, 2539 | 45 | Loom, two spinning wheels, dye tub, bolts and folded cloth on a wood table, baskets of wool, yarn, scissors, the weaver at her loom |
| North | Chandlery | 4331, 2539 | 57 | Candle and oil shop: shelves of candles, wax pots and beeswax, oil flasks, unlit and lit lanterns, lantern posts, a cauldron for wax |
| North | Granary | 4347, 2546 | 56 | Hay piles and sheaves behind rails, sacks of flour, spilled flour, baskets, barrels, pitchfork and scythe, a farmer's skeleton |
| North | Brewery | 4443, 2579 | 59 | Two rows of barrels and kegs, grape tubs, mash cauldron, a log table of wine bottles and glasses, broken barrels, a drunkard's skeleton |
| North | Trophy hall | 4447, 2551 | 84 | Mounted sea creatures and a crab along the north wall, ship's wheel and net, a shark and harpoon on a log table, leviathan skull and coral on pedestals, sea-horse statues, turtle |
| North | Counting house | 4455, 2571 | 48 | Writing tables with abacus, scales and ledgers, a marble table of silver, gold and copper ingots, strongboxes and metal chests, royal vault, scattered coins |
| North | Collapsed passage | 4379, 2563 | 34 | Broken pillars, rubble and ruined wall stubs, sinkholes and cracks, fallen statue fragments, crystals, a crushed skeleton |
| North | Rope walk | 4379, 2579 | 56 | Posts with rope coils between them, racks of canvas, a mast, oars, a workbench of tools, anchor, buoy, cargo |
| North | Avenue of lanterns west | 4423, 2563 | 50 | Paved avenue with lamp posts, columns and planters, statues, benches, a sinkhole and a turtle |
| North | Avenue of lanterns east | 4449, 2563 | 51 | Avenue continued with rubble, coral and crystal |
| North | Drain channel west | 4330, 2579 | 61 | A dark water channel between iron grates, fish, chains and a hanging skeleton on the wall, mushrooms, garbage, skulls |
| North | Drain channel east | 4351, 2579 | 57 | Channel continued, rusty chest, broken barrel |
| North | Gallery of busts | 4427, 2552 | 64 | Portraits on the west wall facing busts and statuettes on pedestals, marble benches, candelabra, an olive runner |
| North | Armorial hall | 4459, 2552 | 70 | A wall of shields, armour stands, sword displays and a sword in a stone on a slate runner, braziers |
| North | Minstrels gallery | 4366, 2539 | 29 | Music stands with sheet music, harp, lute, drum and flute, wooden benches, a minstrel's skeleton |
| North | Pantry passage | 4394, 2543 | 35 | Shelves of cheese, ham and sausage, crates and sacks of food, baskets of bread, water barrels |
| North | Vestry | 4359, 2531 | 28 | Ornate chests, a marble table of folded vestments with a silver trident, covered chair, candelabra |
| North | Root cellar | 4322, 2546 | 26 | Sacks, baskets of apples and grapes, turnip crate, garlic, water barrels, mushrooms |
| North | Smokehouse | 4331, 2546 | 23 | Shelves of hams, fish and sausage, a small brazier and cauldron, water barrel, baskets of fish |
| North | Cooperage | 4435, 2571 | 21 | Barrel staves, unfinished and broken barrels, woodworker's bench with saw and hammer, boards, wood curls |
| North | Pitch store | 4443, 2571 | 19 | Pitch cauldron, tar barrels, buckets and brushes, ship planks, rope |
| North | Sentry corridor | 4347, 2563 | 29 | Three cells: a sentry post with brazier and bench, a bannered passage, and a tapestried niche with a statue |
| North | Bath vestibule | 4394, 2579 | 18 | Basin, water tub, marble benches, towels, urn, suds |
| North | Oubliette | 4459, 2579 | 20 | Two cages with prisoners, chains, grate, skulls, a small brazier |
| North | Stair niche | 4362, 2546 | 16 | Tapestry, statue, marble bench, urn, crystal |
| North | East gate niche | 4467, 2547 | 13 | Statue between two small braziers, coral, shells |
| Central | Scriptorium | 4343, 2603 | 69 | Dusty bookcases, two writing tables, easel, covered chair, torn Atlantis carpet |
| Central | Council of tides | 4368, 2615 | 287 | Slate mosaic with a carved altar disc at its centre, pale throne, marble side tables, olive stone chairs and marble benches in a ring, giant statues and braziers, one deep pool with a jellyfish |
| Central | Shrine of the deep | 4363, 2627 | 150 | Neptune altar on a stone dais, silver trident, Neptune, mermaid and sea-horse statues, charcoal mosaic, floor runes, offerings of pearls and coins, canopic jars, lily pool |
| Central | Gatehouse watch | 4350, 2619 | 36 | Guards' dice table, weapon rack, two cages with prisoners' skeletons, grates, chains |
| Central | Fishmongers arcade | 4336, 2587 | 103 | Four log-table stalls with fish, a shark and nets, signs and lamp posts, baskets and barrels, octopus and squid, a fish pool |
| Central | Votive path | 4357, 2594 | 47 | A line of votive candles and offerings (coins, gems, pearls, dried flowers) beside the existing central pool, a small altar, floor runes, statues |
| Central | Poolside garden | 4374, 2598 | 49 | Paving, planters, marble benches, lamp posts, coral and shells, a turtle, on the east side of the existing pool |
| Central | Forecourt of the tide bell | 4368, 2587 | 53 | Paved forecourt north of the pool with a flat altar bearing the tide bell, braziers, columns, spear-bearing statues, banners, floor runes |
| Central | Pottery | 4323, 2599 | 52 | Shelves of vases and urns, a kiln, water tub and stool, rows of urns, vases and jars on the floor, a potter's skeleton |
| Central | Wash house | 4331, 2623 | 44 | Water tubs and troughs, boiling cauldron, marble table of folded cloth, buckets, baskets, drying sheets on the walls, suds |
| Central | Lecture hall | 4339, 2595 | 72 | Rows of marble benches with books and slates facing a tome on a pedestal, hourglass, globe, two students who never left |
| Central | Timekeepers alcove | 4343, 2627 | 25 | Marble table of hourglasses, grandfather clock, broken clock, benches, a fallen timekeeper |
| Central | Scroll store | 4351, 2611 | 31 | Shelves and crates of scrolls, scrolls scattered on the floor, broken armoire, ladder, damaged books |
| Central | Well of tides | 4324, 2611 | 20 | A stone well with buckets and rope, troughs, planters, benches, lamp post |
| Central | Lantern corner | 4338, 2611 | 15 | Lamp posts, a spear statue, urns, bench, coral |
| East | Keepers library | 4411, 2595 | 110 | A wall of tall bookcases with a ladder and a collapsed armoire, two marble tables (one on mosaic, one on torn carpet), busts, a reading skeleton |
| East | Curators treasury | 4455, 2595 | 79 | Treasure piles, coins and gems, crown and orb on a marble table, ornate chests and a royal vault, display pedestals, gem veins, a thief's skeleton with lockpicks |
| East | Sculpture gallery | 4407, 2613 | 88 | Paved centre with lion, horse, gargoyle and giant statues, a green sea serpent, lion faces and tapestries on the walls, a toppled statue, crystals |
| East | Residential music room | 4443, 2603 | 80 | Harp, lute, drum and flute on an olive carpet, game table, armoire and drawers, potted plants, sheet music |
| East | Atrium | 4443, 2611 | 68 | Deep pool with a purple jellyfish between columns, lamp posts, marble benches, planters, turtle |
| East | Noble bedchamber | 4443, 2619 | 81 | Ruined four-post bed with its occupant, dresser with mirror, wash basin, curtains, writing table, harp, broken bed, servant's cot, moss and mushrooms |
| East | Agora north | 4389, 2602 | 270 | Paved plaza around a fountain, lily pool and fish pool, lamp posts, columns, giant statues, planters, coral, turtle |
| East | Agora south | 4389, 2621 | 220 | Throne on a stone dais with stairs, braziers and candelabra, slate mosaic with medallion, marble benches, columns, leviathan bones, an octopus pool and a bone-strewn pool |
| East | Mosaic gallery west | 4441, 2587 | 68 | Four small mosaics in four hues between columns, statues, benches, tapestries on the west wall |
| East | Mosaic gallery east | 4462, 2586 | 57 | Torn Atlantis carpet and a blue mosaic between columns, banners, giant and lion statues, a collapsed end with rubble and kelp |
| East | Stables | 4467, 2611 | 63 | Four railed stalls with hay and sheaves, troughs and buckets, saddle and bridle, barding, sea-horse statues, a groom in the hay |
| East | Memorial wall | 4419, 2614 | 51 | Tattered banners along the west wall, rows of gravestones with candles and dried flowers, a small altar with a skull candle, memorial statues |
| East | Nursery | 4415, 2627 | 54 | Rocking horse, music box, doll, two cots and a ruined bed, pillows, a table with cake and muffins, colourful paintings |
| East | Private bath | 4427, 2623 | 51 | Bathtub, basins and water tub, mirror, curtains, towels, urns, a lily pool |
| East | Priests cell | 4459, 2623 | 28 | Small altar with candle and rune, tome on a pedestal, cot, chest, tapestry, a kneeling skeleton |
| East | Rubble fall | 4403, 2627 | 31 | A collapsed passage of rocks, ruined walls, sinkholes, statue fragments, coral and crystals, a crushed skeleton and a rusty chest |
| East | Antechamber | 4395, 2586 | 53 | Banners, shields and weapons on the north wall, spear-bearing guard statues flanking the door, braziers, a slate runner, petition table, marble benches |
| East | Cartwrights yard | 4419, 2587 | 44 | Boards and logs, wagon wheels, woodworker's benches, a log table of saws and hammers, wood curls, barrels and crates |
| East | Bindery | 4427, 2599 | 38 | Shelves of books, writing table with quill, a wood table of thread and scissors, damaged books, a binder's skeleton |
| East | Illuminators studio | 4435, 2594 | 20 | Colourful paintings on the north wall, two easels, table of paints and brushes, stool |
| East | Vault niche | 4467, 2594 | 16 | Lit sconces, statue, small braziers, gem vein, scattered coins, rusty chest, east of the treasury door |
| East | Lamp niche | 4458, 2603 | 13 | Tapestry, lamp post, marble bench, urn and vase |
| East | Shrine niche | 4459, 2610 | 17 | Small altar with offering, banner and tapestry, candelabra, statue, rune, dried flowers |
| East | Linen store | 4443, 2628 | 24 | Shelves of folded cloth, baskets, chests, yarn, stool |
| East | Servants quarters | 4450, 2628 | 18 | Two cots (one occupied), foot locker, stool, candle, pillow |
| East | Tack room | 4467, 2630 | 26 | Saddles, barding, bridle, whip and horseshoes, pegboard and brush shelf, hay, chest |
| South | Mariners mess | 4413, 2647 | 250 | Broken banquet table on torn carpet (as at 4287,2617), a bar with kegs and skull mugs, hammocks, hanging nets, ship's wheel and mounted sea serpent, fallen mast, dice table, octopus pool |
| South | Sunken household | 4447, 2667 | 68 | Kitchen with cauldron and log table, spinning wheel and loom, ruined bed and cot, olive carpet, hay and mushrooms |
| South | Crypt of the tide lords | 4415, 2673 | 146 | Altar disc, tomb statues, two rows of sarcophagi and tombs on a charcoal mosaic, gravestones, stone coffins, skull piles, a flooded corner full of bones |
| South | Chapel annex | 4403, 2678 | 57 | Tapestries, statues, benches, a stone table with a crystal ball, a lily pool |
| South | Shipwrights dock | 4395, 2659 | 280 | A deep-water basin with a purple jellyfish and a fish basin, rail-edged wooden dock, beached boat, masts, anchors, buoy, cargo, a shipwright's table with plans, hammock, leviathan skull |
| South | Infirmary | 4443, 2641 | 114 | Four cots with basins (two occupied), bandages, a marble table of herbs and flasks, troughs, alchemy stand, olive carpet, moss and mushrooms |
| South | Tide mages laboratory | 4463, 2647 | 80 | Pentagram with runes and candles, alchemy table, distillery, cauldron, magic mirror, potions, gem veins, crystals and fungus |
| South | Colonnade of the drowned | 4447, 2659 | 128 | Columns and lamp posts on a three-row carpet, statues, leviathan bones, rubble, coral and shells |
| South | Embalmers workshop | 4463, 2677 | 51 | A stone slab with a body, canopic jars, marble table of mortar, scales and alchemy stand, clean bandages, cauldron, an open sarcophagus |
| South | Ossuary | 4427, 2673 | 84 | Skull piles, skulls and bones in rows along a charcoal runner, coffins, rib cage and spine, skull candles, chains |
| South | Net menders | 4419, 2658 | 45 | Nets spread on the floor, hanging nets, fishing poles and weights, spools, stools, crab pots with crabs, buoys |
| South | Salt store | 4459, 2667 | 21 | Sea-salt crates and salt piles, sacks, salt crystals, a barrel and a crate of scales |
| South | Reagent store | 4467, 2663 | 49 | Shelves of potions, dried herbs, garlic, nightshade, mandrake, ginseng and bloodmoss, giant mushrooms and a fungus patch, cauldron |
| South | Well court | 4446, 2676 | 22 | A stone well with buckets, troughs, planters, benches, lamp post |
| South | Harbour steps | 4385, 2642 | 21 | Nets and lamp on the wall, mast, plank, anchor rope, buoy, cargo and crates, a crab pot, fishing pole |
| South | Wreck scatter | 4385, 2675 | 23 | Ship planks, fallen mast, cannon, broken barrels, rusty chest, bones, coral, sinkholes |
| South | Chapel porch | 4394, 2678 | 23 | Tapestry, marble benches, statue, candelabra, votive candles, dried flowers, urns |
| South | Market stalls | 4442, 2651 | 45 | Three log-table stalls divided by walls: cloth, pottery and fruit, with baskets, urns and a lamp post |
| South | Ossuary nook | 4434, 2667 | 12 | Tapestry, stone bench, urn, skull pile and skull candle |

## Implementation and verification

The command reads [Decorations.json](../../Data/Atlantis/Decorations.json) (format 2; room keys keep the `_v2_` prefix the command requires). The generator [build-atlantis-preview.py](../../Source/Tools/build-atlantis-preview.py) holds the palette, the reusable assemblies and every layout, and validates clear floors, wall backing, raised supports, overlay artwork (height ≤ 1, passable), room separation, district bounds and walking connectivity against the MUL files before writing the manifest. When a route is cut it now names the furniture responsible.

Placement modes are `floor`, `overlay`, `surface`, `north` and `west`. The manifest cap in `AtlantisDecor.cs` is 8,000 items. The JSON reader is a small parser inside the script (no library), tolerant of trailing commas so hand edits survive. Items lit in the client: crystal fixtures and sconces, salt crystals, braziers, candelabra, candles, lanterns, gem veins and floor runes.

```powershell
python World/Source/Tools/build-atlantis-preview.py
powershell -NoProfile -File World/Source/Tools/test-atlantis-preview.ps1
```

The harness compiles every server script into an isolated directory and runs 25 checks against the real map reader; its expectations are derived from the manifest. On September 7, 2026 all 34 checks passed (including the in-game save round trip and freeze/unfreeze) with 6569 placements and no blocked groups. It never loads or writes shard saves. [interior-assets-v4.json](interior-assets-v4.json) lists every art/hue pair in use.

A live client walkthrough of revision 4, the palette gump and the in-game save has not been done. After a server restart, run `[AtlantisDecor clear` and then `[AtlantisDecor preview` to replace revision 3.
