# Atlantis decoration style guide

This guide records how Atlantis is decorated today, what the comparison dungeons do differently, and the palette the furnished-interior preview draws from. Coordinates are on the logical `Map.Atlantis` facet (file index 1). The lower floor is Z −127.

## What Atlantis already looks like

Atlantis is a drowned stone city, not a cave. Its existing decorated rooms (west and south-central interior, the wreck at 4452–4482 × 2518–2526, the cells at 4300–4345 × 2672–2699) establish these rules:

| Element | How Atlantis does it |
| --- | --- |
| Walls and floor | Teal stone walls (`0xC7`–`0xE6`) on grey stone floor tiles (`0x519`–`0x524`). Every hue below was chosen to sit against these. |
| Light | Blue crystal fixtures on walls (`0x6385` north, `0x6380` west, hue 2319) and blue salt-crystal clusters (`0x61DA`–`0x61E1`) standing on the floor. No torches or braziers. |
| Water inside rooms | Irregular puddles built from the ground blobs `0x5C46`–`0x5C4D` and `0x5C7C`–`0x5C83`, light (hue 0) for small pools and deep blue (hue 2879) with full tiles `0x5C16`/`0x5C17`/`0x5D1F`/`0x5D29` for large ones. Pools always contain a jellyfish (`0x6210`–`0x6215`), bubbles (`0x79A7`), rushes or a snake plant (hue 2977) and often a fish (`0x540D`). |
| Growth | Bright green grasses and vines, hue 2977 (`0xCB0`, `0xCAD`, `0xCEE`, `0xC5E` over shelves). Coral "rocks" (`0x6247`, `0x624B`, `0x62BD`) and shells only outside rooms. |
| Furniture | Blue stone desks and chairs (`0x1201`–`0x121B`, hue 2722, with an olive variant hue 2001), dark wood banquet tables (`0xB77`–`0xB7B`, hue 2739) with brass settings (hue 2766), sandstone benches (hue 2748). |
| Cases and walls | Shelves and bookcases hue 2764 draped in vines, paintings hue 2764/2767, curtains `0x980` hue 2767, lattice windows `0x155`/`0x156` hue 2748, weapon and armour displays hue 2831. |
| Ornament | Pale marble statues and busts (hue 2826) on stone pedestals (`0x750`, hue 2747), clay urns hue 2767, stone urns hue 0, the green sea-serpent statue (`0x4D0A`, hue 2897). |
| Ruin | Broken pillars `0x45B4`/`0x45B5` (hue 2748), rocks and ruined walls hue 2827, floor holes `0x1B71`, torn cave-floor carpet edges (`0x540`–`0x553`, hue 2746). |
| Macabre | Skeletons and bones hue 2765, dark red cages `0x570F`/`0x2019` (hue 2859), chains, an iron maiden and rack in the cells. |
| Nautical | The wreck: ship planks `0x7B9C`–`0x7B9F`, deck boards `0x4AB` and rails `0x880`/`0x886` (hue 2747), cannons hue 2767, barrels, sea chests `0x4FF5`, a boat `0x2299`. |

The two rooms Uri pointed to (4304,2579 armoury and 4287,2617 banquet hall) are the densest examples: full weapon walls, a long damaged table with settings, torn carpets, two pools beside the table, statues on pedestals in the corners.

## What the comparison dungeons add

Renders of each are in `reference-*.jpg`.

| Dungeon | Aesthetic | Borrowed for Atlantis |
| --- | --- | --- |
| Tower of Brass (main) | Earthen floors, wooden platforms and stairs, stacked crates and barrels on raised decks, hay, skull piles, a table piled with goods, ponds and snow inside rooms, torches everywhere. | Stacked cargo on a wooden deck (harbour storehouse, shipwrights' dock), goods heaped on tables, mixed elevation via a stone dais. |
| Tower of Brass (annex) | Rows of sarcophagi in long halls, a throne on a red carpet flanked by columns, tomb niches. | The crypt's two rows of sarcophagi and tombs; the throne dais in the southern agora. |
| Undersea Castle | Sandstone castle standing in water; deep red carpets, blue mosaic floors, tall curtains, candelabra, potted trees, marble tables and benches, water channels, skulls. | Ornate carpet family `0xAEE`–`0xAFA`, mosaic family `0x56B8`–`0x56E2`, tall curtains `0x12DB`/`0x160E`, marble tables `0x1DC1`–`0x1DC6` and benches `0x1DD0`/`0x1DD1`, potted plants in hue 2977. |
| Azure Castle | Blue architecture, marble furniture, webs, runes on the floor, blood, bones. | Marble tables, floor runes, mosaic carpet; webs and blood were left out as they read dry, not drowned. |
| Caverns of Poseidon | Wooden huts standing in water with beds and rugs, docks with lanterns, shipwrecks, campfires on islets, a dark-tiled temple with weapons on walls, rows of tombs. | Hammocks and cots, lantern posts, nets and anchors, the dock in a flooded hall, log tables. |
| Dungeon Ankh | Brick rooms with ruined beds, broken dressers and armoires, barrels, tables of goods, banners and paintings, cave sections with crates. | Ruined bed sets `0x1898`–`0x189B` and `0xC26`–`0xC28`, broken dressers `0xC22`/`0xC23`, broken armoire `0xC13`, covered chairs `0xC17`. |

## Palette used by the preview

Hues are named in `build-atlantis-preview.py`. Every hue is one already present in Atlantis's statics.

| Name | Hue | Used for |
| --- | --- | --- |
| STONE | 2722 | Blue stone tables, chairs, thrones, slate mosaic floors, pale banners |
| OLIVE | 2001 | Second stone-chair hue, olive carpets |
| WOOD | 2739 | Wood and log tables, wooden chairs and benches, hammocks, spinning wheel, easel |
| MARBLE | 2826 | Statues, busts, marble tables and benches, ornate chests, vases, Neptune and mermaid statues |
| TEAL | 2748 | Columns, dais blocks, stairs, fountain, altar disc, sarcophagi, tombs, gravestones, lion faces |
| PED | 2747 | Pedestals, deck boards, rails, log posts |
| SHELF | 2764 | Bookcases, shelves, dressers, paintings, tapestries, wall maps, foot lockers |
| ITEM | 2767 | Urns, candelabra, small props, jars, baskets, sacks, kegs, lamp posts |
| SETTING | 2766 | Brass plates, goblets and candelabra on tables |
| BONE | 2765 | Skeletons, bones, skulls, cots, ruined beds, chains |
| ARMOR | 2831 | Weapon racks, shields, armour stands, forge, cauldrons, distillery |
| CRATE | 2859 | Crates, cages, iron grates |
| SEA | 2879 | Deep pools, deep-blue ornate carpets and mosaic |
| CRYS | 2319 | Crystal fixtures and sconces, blue braziers, floor runes, small glowing crystals |
| PLANT | 2977 | Grasses, vines, kelp, potted plants |
| MOSS / RUG | 2005 / 2746 | Dark and yellow-green cave-floor patches used as algae and torn carpet |
| DARK | 2944 | Charcoal mosaic in the crypt and shrine, pentagram |
| ROCK | 2827 | Rubble, ruined wall stubs |
| VERDIGRIS | 2897 | Green sea-serpent statues, corroded armour |
| 0 | 0 | Marine life, coral, shells, treasure, nautical gear, light pools |

## Assemblies

* **Pools** copy two existing Atlantis pools tile for tile (`POOL_SMALL` from 4288,2602 and `POOL_LARGE` from 4362,2593) and add the same life those pools contain.
* **Tables** are one tile wide with matching end pieces: stone, marble, wood and log. Chairs and benches use the hues above; officers get log tables with wooden benches, scholars get marble, the council gets stone and marble, the household a log table with stools.
* **Floors** rotate between torn Atlantis carpet, ornate carpet, mosaic, paving of stone steps (`0x917`/`0x918`), and bare stone with moss patches, so no two neighbouring rooms share a floor treatment.
* **Life** in every room, but never the same life twice in a row. Pools appear in about a third of the rooms; only five carry a jellyfish and each is a different species (bath house 0x6213 and 0x6215, atrium 0x6211, council 0x6212, dock 0x6210). Other pools hold fish, lily pads, an octopus or drowned bones. Rooms without water get mushrooms, moss, crabs, shells, coral, crystals, hay, garbage or bones, chosen to suit the room. Skeletons are posed with the room's activity (asleep in cots, slumped at desks, crushed under crates, at the loom).
* **Whole sprites only.** Multi-part art is placed as a set (loom 0x105F/0x1060, cot, hammock, sarcophagus, tomb, fountain, altar disc, ruined beds, leviathan bones, water trough pairs) and single-orientation variants (broken dressers, lion faces, mounted trophies) are never paired as if they were halves. The two ankh pieces 0x1D97/0x1D98 read as fragments in any arrangement and are not used. Carpets narrower than three tiles use field tiles only, so a runner is never a border with nothing inside; open floors use paving rings, not carpet frames.
* **Light** alternates crystal fixtures and sconces on walls, blue braziers at doorways and dais corners, standing candelabra beside altars and tables, lantern posts on decks.
