# Atlantis: The Last Council

Working design, 2026-09-07. Branch: `feature/atlantis-last-council`, created from the fetched `main` / `origin/main` at `c08017c0b429ade46ba7cc0df43bf6dc9df54cf1`. Existing uncommitted work was carried across intact. A [first playable iteration](last-council-first-iteration.md) now implements the investigation, citizens and provisional seabed Herald encounter. This document preserves the broader design proposal; consult the iteration guide for implemented behavior and commands.

## Direction Uri selected

- Use **The Last Council** story for Atlantis.
- Use a **Herald** as the boss. Hydros is handled elsewhere: do not use him, his boss implementation, or make this encounter depend on his storyline.
- The boss fight belongs in a separate area from the furnished city. Its location is still undecided.
- Consider the Atlantis versions of Poseidon's Caverns at `5768,1699`, Dragon's Maw at `4348,4002`, and the sand beneath the boat at `3275,3470,-20`.
- Expand the ruined underwater city: broken stone, marine growth and evidence of the people who lived there. Preserve and build on the existing decoration.
- Interior references include the armory at `4304,2579` and the furnished rooms around `4287,2617`. Tables, chairs, bookcases, statues, wall displays and purposeful room arrangements matter; the rejected shell scatter is not the model.
- New scenery remains world items with `Movable = false`. Keep it removable for review. Freezing it into statics is a later decision for Uri.

Everything below is a proposal or survey finding, not an additional approved design decision.

## Story proposal

The council promised that its final decree would preserve Atlantis. It preserved the city's stone and its rulers' authority by binding the citizens to their last day. The dead still file petitions, stand watch and wait for an evacuation that never came.

The player discovers what the decree actually did and gives the city three things its council withheld: truth about the bargain, care for its abandoned people, and the courage to disobey an unjust order. These actions allow the council's final decree to be challenged.

The **Herald of the Last Decree** is the pact's enforcer: a drowned royal messenger who continues announcing and carrying out its commands. This is a new Memento character. No identity or allegiance to an established Ultima deity is assumed. The story should be presented as Memento's Atlantis fiction, without claiming these events or this city are established Ultima canon.

Working seabed arena name: **The Unanswered Anchorage**. The council promised rescue ships; the citizens' last hope now hangs above their prison. That makes the existing boat meaningful to the story.

## Explore the city to open the encounter

Let players complete the three obligations in any order. Each should teach an action used again during the boss fight. Completion should visibly change its room and the council chamber, rather than merely add three keys to a backpack. Exact interaction positions must be checked against the current decoration manifest and saved edits.

| Obligation | Place and activity | Opposition | Visible result |
| --- | --- | --- | --- |
| **Expose the decree** | Search the archive's desks and shelves for a proclamation, casualty register and evacuation order. Match the evidence to three statements in the proclamation and challenge the false one at the clerk's lectern. Clues remain available if the player gets it wrong. | Bound clerks conceal evidence while ink-dark apparitions interrupt the investigation. | The proclamation changes to show the truth; the first council seal cracks. |
| **Remember the abandoned** | Recover a personal keepsake in a household or infirmary and give its spirit a name. Guide the awakened spirit to a refuge or memorial, clearing threats along the route. A lost escort returns to its last safe point. | Drowned citizens need help; chains and jailers keep them trapped. Make friendly and hostile spirits distinguishable. | The rescued spirit becomes a witness; the memorial lights and the second seal cracks. |
| **Release the watch** | Recover the watch standard and bring it to the armory's oath stand. Defend it while changing the watch's standing order from protecting the council to protecting the citizens. | Oathbound guards and a Watch-Captain attempt to restore the old command. | The watch kneels or withdraws; the final seal cracks. |

At the council table, players call the witnesses and reject the decree. The empty seats answer, the Herald issues a summons, and a passage opens to the separate arena. Reuse the furnished chamber as the story's focal point; avoid replacing it with a second boss arena.

Design the first playable version so one player can perform each interaction sequentially, with room for friends to help fight and protect an escort. This is a prototype convenience, not a decision about final difficulty or party size. No mandatory simultaneous switches or oxygen countdown are proposed.

## Monsters with a reason to be here

| Role | Working creature | Behavior |
| --- | --- | --- |
| Civic opposition | **Oathbound Watchman** | Corroded arms and armor; guards the old order and reinforces nearby watchmen. |
| Archive opposition | **Drowned Clerk** | A spectral scribe who channels a binding while copies of the false decree animate around the desks. |
| Rescue threat | **Chainkeeper** | Tethers spirits to the decree. Breaking its chain matters more than chasing an endless supply of summons. |
| Environmental threat | **Brine Wraith / reef scavenger** | Haunts ruined passages or feeds among wreckage, giving quieter rooms a different kind of danger. |
| Noncombatant | **Remembering Citizen** | Reveals the city through a name, occupation and unfinished task. Most are victims, not automatic enemies. |
| Boss | **Herald of the Last Decree** | Announces commands, binds witnesses and calls the watch. A readable humanoid silhouette suits the low seabed better than an enormous sea monster. |

These are encounter roles and working names, not commitments to new artwork or specific existing monster classes. Inspect available bodies and AI before choosing implementations. Tune numbers, damage, resistances and rewards after the mechanics work.

## Herald fight proposal

The Herald announces a decree before each major attack. Show its warning through text plus a visible symbol or ground pattern; color alone should not be the cue. Start with three mechanics and combine them near the end, with enough time for a solo tester to respond.

1. **"Let the record stand."** The Herald shields itself behind the false proclamation. Use the truth lectern to reveal its contradiction, interrupt the shield and create a damage window. This repeats the archive interaction in a shorter, combat-readable form.
2. **"None shall depart."** Chains bind one of the rescued witnesses. Release the binding before the Herald completes its sentence. While a witness is bound, it cannot maintain its protective ward. A missed rescue should be recoverable within the fight, not erase the city's completed escort task.
3. **"Hold your station."** The Herald marks a broad lane with an approaching surge. Raise the reclaimed watch standard at a ward to establish a safe rally point, or move out of the announced lane. Later casts rotate the threat; avoid random damage with no readable warning.

When the Herald falls, the witnesses deliver the city's answer and the decree breaks. Provide a clear return passage, a brief aftermath in the council chamber and room for a fitting reward, such as a council seal, corroded herald's insignia or a small decorative memorial. Final rewards remain open.

For the seabed, dress the boundary with fallen civic monuments, broken plinths, anchors, short chain runs, wreck timbers and restrained coral growth. Put the lectern, memorial and standard ward in three clearly different stations. Keep the center and routes between them open; the fight's dressing should connect to the furnished city without obstructing its movement mechanics.

## Arena survey

All coordinates below are on **logical `Map.Atlantis` (map index 6)**. Its terrain and statics use file index 1, shared with Sosaria. Reusing this geometry on Atlantis does not copy Sosaria's regions, world decorations, spawns or mechanics.

See [the comparison image](herald-arena-candidates.jpg) and [raw collision probe output](herald-arena-audit.txt).

| Candidate | Existing geometry and appearance | Implication |
| --- | --- | --- |
| **Poseidon's Caverns: `5768,1699,0`** | Long watery cave with teal formations and scattered marine detail. A conspicuous lava patch occupies part of the surveyed area. The anchor has a zero-height water static at Z 0 over passable land. | Strong aquatic palette and a naturally enclosed shape. The lava needs a deliberate explanation or a different subarea. Water appearance alone does not establish whether a tile is walkable: the anchor passes `CanFit`, but player and pet movement still need validation. |
| **Dragon's Maw: `4348,4002,0`** | Broad, irregular chamber with grey/purple cave flooring, stalagmites, scattered rocks and small floor features. | The most straightforward base for an open fight. Needs more decoration to communicate a drowned city's final refuge. Best fallback if the seabed fails the client check. |
| **Under the boat: `3275,3470,-20`** | Sand at roughly Z -20/-19, sparse wreckage and marine detail. Water lies at Z 0, with ship structures at and above that height. | Strongest connection to being beneath the sea and to the failed evacuation. Normal character-height clearance looks promising. Overhead water limits tall scenery, and client visibility remains the decisive unknown. |

**Recommendation:** prototype the seabed first, while retaining Dragon's Maw as the fallback. This recommendation does not settle the location. Before authored scenery or boss work, inspect the seabed in the supported client at Z -20, with a normal character, pets and sample ground effects. Confirm the water and boat can be hidden or rendered without obscuring targeting and attack warnings. If the result is poor, use the open cave rather than changing shared terrain as part of this work.

### What the geometry check establishes

A standalone probe referenced the compiled `World.exe`, configured the existing map definitions and read `World/Data/Files`. It used empty in-memory item/mobile dictionaries and never started the server, loaded saves, saved a world or changed the MUL files.

For each anchor it sampled a centered 41 x 41 square (1,681 cells), with `Map.CanFit(x, y, z, height, true, false, true)`. Poseidon and Dragon's Maw were checked at Z 0. The seabed followed `GetAverageZ` and admitted only Z -20/-19. Components connect orthogonal neighboring fit cells with a height difference no greater than two; this is a geometric adjacency calculation, not AI pathfinding.

| Candidate | Cells fitting height 16 | Largest adjacent group | Height 20 fit cells | Height 25 fit cells |
| --- | ---: | ---: | ---: | ---: |
| Poseidon | 1,158 | 1,149 | 1,158 | 1,158 |
| Dragon's Maw | 1,322 | 1,322 | 1,322 | 1,322 |
| Under the boat | 1,412 | 1,412 | 899 | 0 |

All three exact anchors passed height 16. The seabed anchor passed height 20 but failed height 25. These are floor/body clearance results, not guarantees of player movement, spell line of sight, client rendering, animation visibility or suitability for a specific decorative sprite. Counts cover the sample square, not a selected combat boundary, and exclude live world items and mobiles.

The image uses the actual map/statics and client art/hues. Its crops are Poseidon `(5738,1672)-(5799,1718)`, Dragon's Maw `(4318,3976)-(4380,4030)`, and the boat `(3248,3448)-(3300,3494)`, with exclusive upper bounds. Vertical ranges are shown on the panels, also with exclusive upper bounds. The lower-left boat panel deliberately omits Z 0 and higher to expose the sand; the lower-right includes the overhead structures. These are offline renders, not client screenshots. The zigzag sand seams are a limitation of the flat land-art renderer, not evidence of a damaged map. The renderer does not reproduce client roof hiding, terrain texturing, lighting or animation.

## Implementation outline once the arena is selected

1. **Arena preview:** create reversible, owner-tagged world items on Atlantis only. Include a dry-run placement check, apply/status/clear tools and a GM visit command. Make repeated application idempotent and preserve unrelated objects. Use the existing [style guide](style-guide.md) for art and hues. Do not overwrite the city manifest or reuse its fixed Z -127 placement assumptions for an arena at Z 0/-20.
2. **Encounter controller:** track the three obligations, witnesses, council challenge, arena availability, active fight and aftermath. Keep the arena bounds, entrance, return point and interaction positions together in a configurable location profile so changing arenas does not rewrite the story.
3. **Playable sequence:** first build one complete path from a city obligation through a simple Herald encounter and return. Then add the other obligations and the three boss mechanics, using lessons from client testing.
4. **Recovery and tuning:** handle failed escorts, encounter abandonment, player death, disconnects, server restart, pets, late arrivals and repeated boss triggers. Establish whether progression is shared or party-specific before persistence and rewards are finalized. Reset only encounter-owned entities, and award completion once per eligible run.

All three proposed arenas lie outside the current Atlantis Hall of Ktulu region (`4125,2424`, size `419 x 337`). Give the chosen arena its own Atlantis region and explicit entry/return behavior; do not rely on the similarly located Sosaria dungeon region. Configure Z bounds so a seabed fight cannot accidentally include the boat deck. Encounter spawns, hazards and cleanup must remain restricted to Atlantis and the selected encounter boundaries.

Validate movement and spell targeting across the actual dressed arena, pet access and return, readable warnings under overhead art, and recovery after a wipe or restart. Do not freeze scenery or edit shared map/statics files during preview work.

## Open decisions

- Final arena, after the seabed client check.
- Final Herald title, appearance and dialogue.
- Shared-world versus party progression, difficulty and intended group size.
- Reset/cooldown policy, rewards and how much of the city's aftermath persists.

The branch and this brief provide the design context. See the [first-iteration guide](last-council-first-iteration.md) for the implemented commands, current scope and playtest instructions.
