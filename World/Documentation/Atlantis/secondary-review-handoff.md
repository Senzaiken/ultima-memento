> **Revision 4, September 7, 2026:** The manifest is now `Data/Atlantis/Decorations.json`; `[AtlantisDecor` with no arguments opens a palette gump of every art/hue pair for placing new pieces, `[AtlantisSave` (two-corner target) writes a room back into the JSON, and `[AtlantisSave unfreeze|freeze` toggles movability for rearranging. after Uri walked revision 3 in the client he asked for the still-empty rooms to be filled, for the repeated jellyfish pools to be varied, and for partial multi-part graphics (ankh halves, border-only carpets) to be fixed. Revision 4 has 96 arrangements / 6,569 items; every room, corridor and niche in the four districts is decorated. Details in preview-commands.md and style-guide.md.

> **Superseded on September 6, 2026 (revision 3).** Uri reviewed revision 2 and rejected it as monotonous: "Everything is just all the same, same rugs, same tables." He asked for a study of other dungeons (Tower of Brass in particular) to understand each one's aesthetic, then an expansion with more styling, more decoration and a feel of life. Revision 3 redesigned all 13 rooms and added 18 arrangements (31 groups, 4,300 items). See [preview-commands.md](preview-commands.md) for the rooms, [style-guide.md](style-guide.md) for the aesthetic and palette, and `interior-preview-v3-*.jpg` for renders. The implementation notes below still describe the command; the room list and item counts are out of date.

# Atlantis decoration: secondary review handoff

Review the current Atlantis decoration expansion against Uri's directions below. Assess both the visual design and the implementation. The main question is whether these additions feel like furnished rooms belonging to the existing Atlantis dungeon, while remaining a reversible preview of immovable world items. Passing tests and a high item count do not establish that the decoration looks good.

This handoff describes the state on September 6, 2026. Paths are relative to the `ultima-memento` repository unless linked otherwise. The workspace used was `C:\Users\Uri\Documents\ultima-memento`.

## What Uri requested and how the direction evolved

1. **Study the actual map and client assets.** Uri asked whether the agent could inspect MUL files, artwork, tiledata, and comparable dungeons to design additions to unfinished Atlantis. The work proceeded through direct asset inspection and offline rendering; screenshots or an MCP connection were not required for that work.
2. **Make a reversible world-item preview.** The original instruction was to spawn decorations “not as statics, but just as items in the world with movable false.” Uri wants to inspect the result and may freeze it later if he likes it. Writing decorations into MUL statics is outside the current task.
3. **Use a ruined underwater city theme.** Uri said: “I like the ruined underwater city thing.” He requested an underwater/coral theme with broken stone. Monsters are explicitly for later.
4. **Study these references:** Poseidon's Caverns (listed in the study as Caverns of Poseidon), Furnace Vorpal Dock, Flooded Temple, Cave of Souls, Dungeon Ankh, Undersea Castle, and Azure Castle. His message spelled the dock “Furance Vorpal Dock.”
5. **Expand existing decoration.** Uri emphasized: “some of atlantis IS also decorated..we want to just expand on that.” Preserve finished rooms and use them as the primary style reference.
6. **Actually implement the commands.** Uri asked for working placement commands, then asked to compile and run the local Memento server. A study or proposed design alone did not satisfy that request.
7. **The first design was rejected.** Uri specifically directed attention to existing Atlantis rooms at **4304,2579** and **4287,2617**. His criticism was: “you didn't add tables, chairs, bookcases, statues, wall decor” and “you just added more shells from the outside to the inside.” This correction takes priority when interpreting the earlier coral-theme request. Interiors need furnishings, arrangement, and a recognizable former use.
8. **Increase density and vary the tables.** His latest design instruction was: “add more decoration in each room. and change up the tables a bit, in the council and officers refectory.” All 13 proposed room layouts were subsequently expanded, and those two table arrangements were changed.

Uri has requested an independent review. The current design has not received final aesthetic approval. The review should produce findings and recommendations; it should not treat this handoff as a request to freeze items or rebuild the live world.

## Implementation and revision history

The initial preview placed **715 items in 108 small groups**, emphasizing shells, coral, and isolated ruined props. Uri rejected that visual direction. Its seven-column manifest remains at [Decorations.txt](../../Data/Atlantis/Decorations.txt) for the original script that may still be loaded by a running server. **It is not the current design.**

The replacement used **13 authored furnished-room arrangements / 1,115 items**. The latest density revision adds **241 net furnishing and detail pieces**, bringing the current manifest to **1,356 items**. The carpet/overlay count did not increase in that revision. The current total includes **680 overlay pieces** and **676 other pieces**, so do not interpret it as 1,356 separate furniture assemblies.

The 13-room selection, room names, exact coordinates, furniture palette, and specific table designs are the implementing agent's choices. Uri requested the theme, references, more furnishings, and table variation; he did not prescribe a U-shaped table or a particular item total. This is an expansion of selected sparse areas, not a claim that every unfinished Atlantis room has been completed.

| District | Room | Approximate center X,Y at Z=-127 | Current items |
| --- | --- | --- | ---: |
| North | Tidal archive | 4334,2527 | 129 |
| North | City watch armory | 4331,2562 | 118 |
| North | Upper reading room | 4378,2547 | 41 |
| North | Officers' refectory | 4410,2545 | 198 |
| Central | Scribes' study | 4344,2603 | 61 |
| Central | Council of tides | 4367,2617 | 189 |
| East | Keepers' library | 4410,2595 | 90 |
| East | Curator's chamber | 4455,2595 | 58 |
| East | Sculpture gallery | 4407,2611 | 63 |
| East | Residential study | 4440,2620 | 61 |
| South | Mariners' mess | 4416,2646 | 195 |
| South | Sunken household | 4450,2667 | 69 |
| South | Memorial chapel | 4410,2678 | 84 |

Centers identify arrangements, not guaranteed clear teleport tiles. Some arrangements span connected bays. District totals are north 486, central 250, east 272, south 348.

The **council** now uses a U-shaped blue stone table, maps, books, chairs around the outside, and a speaker's seat at the open side. The **officers' refectory** uses two narrow wooden dining tables, place settings, a serving counter, and a smaller gaming table. The mariners' mess retains a broken banquet table. Other additions include reading tables, benches, bookcases, crates and chests, armor displays, paintings, pedestal statues, urns, and damaged domestic furnishings. Four small coral accents accompany masonry ruins; no shells are added by the current manifest.

## Files and evidence to review

| File | Purpose |
| --- | --- |
| [AtlantisDecor.cs](../../Source/Scripts/System/Commands/AtlantisDecor.cs) | Administrator command, manifest validation, placement, cleanup, and persistent preview item class |
| [Decorations-v2.txt](../../Data/Atlantis/Decorations-v2.txt) | Current eight-column placement manifest; the density revision retains this filename and format |
| [build-atlantis-preview.py](../../Source/Tools/build-atlantis-preview.py) | Authored layouts and furnishing helpers; `enrich_rooms` holds the latest additions |
| [AtlantisPreviewTests.cs](../../Source/Tools/AtlantisPreviewTests.cs) | Isolated regression harness using the actual server map reader |
| [test-atlantis-preview.ps1](../../Source/Tools/test-atlantis-preview.ps1) | Compiles all scripts and runs the harness |
| [preview-commands.md](preview-commands.md) | Usage, room locations, and implementation notes |
| [reference-study.md](reference-study.md) | Earlier inspection of Atlantis and the seven requested references |
| [interior-assets-v2.json](interior-assets-v2.json) | Current 126 art/hue pairs, counts, metadata, and existing Atlantis examples where found |
| [Six current room renders](interior-preview-v2.jpg) | Includes archive, armory, refectory, council, library, and residential study |
| [Other seven room renders](interior-preview-more-rooms.jpg) | Remaining proposed arrangements |
| [Council and refectory close-up](council-and-refectory.jpg) | Latest table designs |
| [Dungeon references](dungeon-references.jpg) | Samples from the seven requested comparison locations |

The earlier `preview-districts.jpg` and initial `art-palette.*` concern the earlier design/material survey. Use the current interior renders and inventory to evaluate the latest implementation.

## Map inspection and placement mechanics

Atlantis is **logical map index 6, backed by file index 1**, and shares that underlying map data with Sosaria. Review placement on `Map.Atlantis`, not simply map/file number 1. The lower stone floor is **Z=-127**, an upper layer is at **Z=-87**, and water is above. The relevant registered region spans X 4125-4543 and Y 2424-2760.

Inspection used server files in `World/Data/Files`, especially `map1.mul`, `staidx1.mul`, `statics1.mul`, and `tiledata.mul`. The artwork and hues came from `C:\Users\Uri\Downloads\Ultima-Memento\Client\Data Files`. Some inspection/render helpers remain in ignored `.claude/tmp/` files, notably `uolib.py`, `inspect_atlantis.py`, `render_atlantis_v2.py`, and `render_atlantis_v2_other.py`. Those helpers may be absent in another checkout; the saved documentation images and manifests are the review artifacts.

Tile names alone are unreliable for this custom client: some marine formations are named “rock,” and the inspected “debris” artwork represents timber. Furniture assemblies were inspected visually, including their orientation and height. Torn carpet edges reuse artwork found in existing Atlantis carpets.

The command supports `preview`, `check`, `clear`, `status`, and `go`, with optional `north`, `central`, `east`, `south`, or `all` district selection. It creates nothing at startup. It rereads the current manifest when checking or placing.

Every spawned decoration is an `AtlantisPreviewItem`, derived from the server's `Static` **world-item class**. That class name does not mean a frozen MUL static. Items have `Movable=false`, do not decay, and serialize ownership by district/group. Crates, books, and other props are decorative artwork, not functional containers or loot. The subclass remains compatible with a later Freeze operation, but freezing is not implemented or performed by this preview command.

Placement modes are `floor`, `overlay`, `surface`, `north`, and `west`. Validation checks stone-floor anchors, backing walls, raised support, manifest bounds, and existing decoration in each arrangement's bounding rectangle plus a one-tile margin. A conflicting static, world item, or mobile causes the whole arrangement to be skipped. Unexpected placement exceptions roll back the items created by that invocation. Existing owned groups prevent duplicate application, including partly removed or moved groups. Cleanup selects only the preview subclass, including owned items moved elsewhere.

An original shell-preview group blocks application of the furnished revision until explicitly cleared. Later furnished revisions retain their group keys: **`preview` alone does not update already spawned rooms**. Refresh with `clear` followed by `preview`.

## What has and has not been verified

The latest full script compilation succeeded, with eight pre-existing warnings in unrelated files. All **25 regression checks passed**, including all 1,356 placements against the real MUL reader, dry runs, wrong-facet rejection, legacy handling, static/item/mobile conflicts, missing backing walls, raised support, duplicate prevention, travel, serialization, and selective cleanup. The harness uses an empty in-memory world and never loads or writes shard saves.

The generator also passed its floor, wall, support, separation, and walking-connectivity checks. All 13 arrangements were rendered and visually inspected with actual client art and hues. These renders hide upper layers and omit live items, animation, and actual client lighting.

**A live walkthrough of the latest revision has not been verified.** The local server was previously compiled and launched, but at handoff creation it was still the process started September 5 at 21:41:56 (PID 44156). Do not assume that process has loaded the later command or that the current manifest has been spawned into the live world. Confirm runtime state before making claims about deployment.

For reproduction from the repository root:

```powershell
python World/Source/Tools/build-atlantis-preview.py
powershell -NoProfile -File World/Source/Tools/test-atlantis-preview.ps1
```

The first command regenerates the current manifest; the second writes isolated build/test outputs. Neither should alter MULs or saves. The live preview workflow is documented separately. If the original command is still loaded, the in-game `[Restart` command saves and restarts the server; after reconnecting, `[AtlantisDecor clear` and `[AtlantisDecor preview` refresh the owned additions. A loaded furnished-room command can reread this latest data-only revision without another restart.

## Questions for the independent reviewer

- **Visual fit:** Compare directly with 4304,2579 and 4287,2617. Do the palettes, furnishing density, orientations, and room arrangements belong to Atlantis? Does each room have a readable purpose? Identify repetitive, sparse, cluttered, or awkward areas with coordinates.
- **Latest request:** Is every proposed room meaningfully richer? Are the council and officers' tables sufficiently different, properly assembled, and well furnished? Assess actual furniture/detail additions rather than counting carpet tiles.
- **Ruined underwater character:** Does the balance of surviving furniture, damage, statues, and limited marine growth work? Are the newer intact tables too pristine? Do any props look misplaced or unrelated to the room?
- **Artwork and movement:** Inspect table joins, chairs, raised objects, torn rugs, wall attachments, and sprite overlap. The connectivity check uses grid anchors; it does not prove comfortable character-width routes, reachability of every chair, or visual clearance for oversized sprites.
- **Validation limits:** Check planned-item overlaps across placement modes and arrangements, as well as collisions with current live items. Support validation uses matching coordinates and tiledata height; independently assess whether that is sufficient for the artwork used. Do not assume the current tests cover every malformed-manifest or stacked-item case.
- **Reversibility and persistence:** Verify facet isolation, no automatic spawning/MUL writes, ownership serialization, cleanup of moved items, partial-group behavior, and failure rollback. Existing finished decoration must remain intact.
- **Runtime and performance:** Confirm which command is loaded, whether rooms were skipped because of live conflicts, and how the item count and crystal lights behave in the client. Clearing cannot undo decorations already frozen into shared map files.

Return a prioritized review with file/line or room/coordinate references, separate visual findings from implementation defects, and distinguish demonstrated problems from checks still requiring a live client. Suggest concrete improvements without treating the current room selection or table shapes as requirements imposed by Uri.

The workspace also contains unrelated modifications to saves/accounts, `.gitignore`, and `DynamicMapDefinitions.cs`. Do not attribute those changes to this decoration revision or overwrite them during review. Much of the new Atlantis work is currently untracked in Git; reviewing only `git diff` will miss it.
