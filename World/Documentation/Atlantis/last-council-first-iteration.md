# Last Council: first playable iteration

Built on `feature/atlantis-last-council`. This implements the city investigation, citizens, watch encounter and a provisional Herald fight beneath the boat. Hydros is not involved.

## Load and place it

The running local server must restart to load the new scripts. Use the existing administrator command `[Restart` in game; it saves before restarting. Reconnect, then run these commands one at a time:

```text
[AtlantisCouncil check
[AtlantisCouncil setup
[AtlantisCouncil go citizens
```

`check` reads the real map and current world items without placing anything. `setup` creates 25 encounter placements: 19 clue/interaction/scenery items and six citizen spawners. The spawners create six named NPCs. New scenery is immovable, nondecaying world items on **Map.Atlantis**, not frozen statics. The controller is stored internally and its progress is included in normal world saves.

Setup searches within each intended room for clear floor, accounts for current furniture, and requires approachable positions. If any required placement is blocked, preflight reports the problem before creating placements. Repeating setup retains existing objects and repairs missing ones. If an existing encounter object was moved to another facet or out of its room, setup asks that it be restored rather than duplicating it.

Existing Atlantis preview decorations are a separate system. This command expands around them and does not apply, clear or regenerate their manifest. No map, statics, art or tiledata files are changed.

Before the full playthrough, `[AtlantisCouncil go arena` lets an administrator inspect the provisional seabed. This visit does not spawn the boss or bypass the investigation. Check whether the overhead water and ship allow you to see your character and ground effects comfortably.

## Meet the citizens

The six citizens are spread across their workplaces. **`[AtlantisCouncil go citizens` visits Mira only**, not a gathering of all six. Use `[AtlantisCouncil status` to list every citizen's actual location and whether it has spawned. After loading the updated scripts, `[AtlantisCouncil go Edrin` (or any of the six names below) visits that NPC directly. The council chamber currently speaks through the seal; separate seated councillor NPCs are not part of this iteration.

Double-click a citizen, use their **Talk** context entry, or say **hello** nearby. Their conversation has buttons for further information, your investigation, buying and selling. The usual vendor context menus and speech also work. Normal Memento merchant settings and economy rules apply; staff purchases may be free under the existing vendor system.

| Citizen | Place | Shop | What they know |
| --- | --- | --- | --- |
| **Mira**, infirmary keeper | Infirmary, near `4436,2642,-127` | Bandages, lesser healing potions | Her music box and the abandoned citizens |
| **Edrin**, archivist | Tidal archive, near `4339,2529,-127` | Blank scrolls, black pearls | The three records and the council's lie |
| **Orin**, armorer | City watch armory, near `4326,2558,-127` | Daggers, bucklers | The standard and the watch's older oath |
| **Neris**, provisioner | Fishmongers' arcade, near `4331,2588,-127` | Bread, bandages | How the three testimonies fit together |
| **Selene**, lamplighter | Avenue of lanterns, near `4418,2564,-127` | Candles, sulfurous ash | How to answer the Herald's decrees |
| **Tovan**, innkeeper | Officers' refectory, near `4412,2548,-127` | Bread, bandages | The last feast and the council chamber |

The NPCs use spectral hues and ordinary clothed human bodies so they can converse and trade normally. They are invulnerable, remain at their stations, and respawn through their owned spawners if deleted. Their merchandise can be bought and matching supplies sold back. No purchase is required to solve a puzzle.

## Play without spoilers

Each character has their own investigation. The three obligations can be completed in any order. Reading clues records them in memory; `[AtlantisJournal` or a citizen's investigation button shows progress and lets you reread the records.

- Search the three archive bookshelves and challenge the record at the lectern.
- Find Mira's music box, speak to her, and guide her remembering echo to the memorial.
- Remember the folded standard in the barracks, then answer the oath stone in the armory.

When all three are complete, interact with the council seal. Calling the witnesses opens the passage and starts the Herald encounter. Friends must complete their own obligations to join through the seal; qualified characters can join the same active fight.

The initial fight uses one shared arena, not party instances. It cycles through three announced decrees with an eight-second response window. Ward names and spoken warnings identify the response. The final third of the boss's health shortens the quiet intervals. This is an initial tuning pass for testing, not a finished difficulty target.

The return conch stays usable during combat and in death. Following pets travel into the arena with their owner; surviving owned pets and summons still inside the arena return with the owner even if they are farther from the conch. Existing pet death rules still apply.

The first victory awards a blessed cosmetic **memorial seal of the Last Council**, once per character while this controller is retained. If the backpack cannot receive it, the pending claim survives saves and can be retried at the return conch or council seal. No custom equipment or boss loot table is included yet.

## Recovery and testing commands

| Command | Effect |
| --- | --- |
| `[AtlantisJournal` | Player command: progress and collected records |
| `[AtlantisCouncil status` | Counts encounter objects/NPCs/investigations, reports whether the Herald is active, and lists each citizen's live location or missing status |
| `[AtlantisCouncil go <name>` | Visit Mira, Edrin, Orin, Neris, Selene or Tovan at their actual position |
| `[AtlantisCouncil go archive` | Visit the archive lectern |
| `[AtlantisCouncil go citizens` | Visit Mira in the infirmary |
| `[AtlantisCouncil go watch` | Visit the oath stone |
| `[AtlantisCouncil go council` | Visit the council seal |
| `[AtlantisCouncil go arena` | Inspect the seabed near the return conch |
| `[AtlantisCouncil resetfight` | Remove the active boss, return registered participants and their arena pets, retain city progress |
| `[AtlantisCouncil resetme` | Reset the invoking administrator's investigation; retain previous victory/reward eligibility |
| `[AtlantisCouncil setup` | Repair missing encounter objects without resetting progress or duplicating NPCs |
| `[AtlantisCouncil remove` | Remove this controller and its owned scenery, spawners and creatures; **discard its investigation records**; preserve unrelated decor and earned souvenirs |

All `AtlantisCouncil` operations require Administrator access. Nothing installs automatically on startup. Once installed, citizen spawners and the controller resume after loading a save.

Mira's echo waits when its guide is more than twelve tiles away or dead. Returning to Mira can start a fresh echo if the old one is distant; no duplicate follows the same character. Echoes expire after twenty minutes. An interrupted escort can be started again without finding the music box again.

The watch challenge resets when its challenger dies, leaves the armory area, or takes longer than five minutes. It can then be started again. A boss fight with no connected, living participant in the arena resets after sixty seconds. Restart recovery cancels active fights, watch challenges and temporary escorts while retaining completed obligations, collected clues and pending rewards. The short post-victory cooldown is one minute.

Removal is intended for reviewing the entire prototype. Use `resetfight` or `resetme` when testing progression rather than removing the controller each time.

## Walkthrough / spoilers

<details>
<summary>Show the complete puzzle and boss solution</summary>

1. In the archive, read the public proclamation, harbor register and sealed evacuation order. At the lectern choose **"The council withdrew the rescue ships."** Every record is required; a wrong answer preserves the clues.
2. Find the waterlogged music box in the sunken household near `4448,2669,-127`. Return to Mira in the infirmary and choose **"I remember your music box. Come bear witness."** Walk her echo to the memorial near `4419,2616,-127`, then double-click the memorial with the echo nearby. Follow the actual corridors around the rooms.
3. Remember the folded standard in the barracks near `4368,2567,-127`. At the oath stone near `4332,2567,-127`, choose **"Protect the people; raise their oath."** Defeat both sentries and the captain while staying near the armory.
4. At the council seal (approximately `4363,2621,-127` with the current furnished manifest), call the witnesses. The seal transports you beneath the boat and summons the Herald.
5. **LET THE RECORD STAND:** use the TRUTH book ward near `3266,3467,-19` to break the damage shield.
6. **NONE SHALL DEPART:** use the LOVE binding anchor near `3282,3464,-19`. Ignoring it causes cold damage and restores some boss health.
7. **HOLD YOUR STATION:** leave the marked surge lane, or activate the COURAGE rally stone near `3278,3480,-20` and stay within four tiles of it. The lane is five tiles wide, centered on the marked X coordinate.
8. Defeat the Herald, receive the memorial seal and use the return conch near `3263,3458,-20`.

Coordinates can shift slightly within each room if live decorations occupy the preferred tile. The administrator visit commands use the installed object's actual position.

</details>

## Implementation and validation

Source lives in `World/Source/Scripts/Engines and Systems/Quests/Atlantis/`:

- `AtlantisCouncil.cs`: commands, persistent per-character progress, challenge/arena lifecycle, region and owned cleanup.
- `CouncilLayout.cs`: room bounds, art/hues, placement preflight and the provisional seabed layout.
- `CouncilObjects.cs`: interactive furnishings, journal/dialogs, clue text and reward.
- `CouncilActors.cs`: citizen spawners, vendors, escort echo, watch enemies and Herald mechanics.

Run `World/Source/Tools/test-atlantis-council.ps1` from PowerShell to compile all server scripts and execute the isolated integration checks. It does not start the server or load/save the live world. It creates an in-memory world using the real MUL map and the current furnished Atlantis manifest.

Full server-script compilation succeeded, and all 71 integration assertions passed. Validation includes clear placements amid existing decor, idempotent setup and respawning, merchant stock/buyback types, personal clues and answer requirements, stale/forged dialog replies, the walking escort route, the watch challenge, all three boss mechanics, pet travel, ghost return, reward deduplication, serialization, restart recovery and preservation of existing decoration during removal.

The art choices were checked against the actual tiledata and client artwork, including the book stand, music box, folded cloth standard and binding anchor. In-client rendering, the feel of movement under the overhead water, dialogue layout, actual buy/sell transactions and combat balance still need Uri's playtest. The engine tests are not a substitute for that visual and gameplay check.

The original [design brief](last-council-design.md) remains the longer-term outline. Ambient hostile clerks/reef creatures, richer witness scenes in the arena, additional council aftermath and final reward/difficulty tuning remain later work.
