# The World of Pagan — Design Draft

> Status: **DRAFT / pre-implementation.** Branch: `feature/pagan-world` (off `main`).
> Scope: a new, bespoke explorable world (facet) plus the questline that connects it to
> the existing Titan-of-Ether ascension arc.
>
> Legend used below: **[canon]** = grounded in Ultima VIII: Pagan · **[code]** = already
> exists in this repo · **[new]** = to be authored.

---

## 1. Pitch

Pagan is the Guardian's conquered world — the place the game has referenced for years but
never let players visit. The existing elemental/Titan content (collect the four elements,
absorb the four Titans, become a **Titan of Ether**) is **Act 1: the ascension**. Pagan is
**Act 2: post-ascension endgame**, entered *as* the Titan of Ether to do what the Titans
can't — break the Guardian.

The four Titans are **not** Pagan's bosses. They are already the ascension bosses in the
base worlds **[code]** and stay exactly as they are. They are the price of admission. Pagan's
bosses are what was always *above* the Titans: the Guardian and the elemental cults' daemons.

This closes the running thread from the "Black Gate"/Guardian tribute NPC — *"I am stuck
here in Sosaria, looking for a way to get to the world of Pagan"* (`EpicGump.cs:268`) **[code]**.

---

## 2. Access & gating

- **Prerequisite:** `PlayerMobile.IsTitanOfEther == true` (`PlayerMobile.cs:3033`) **[code]**.
  This is the StatCap-300 / skill-cap-bonus endgame tier, so Pagan is the hardest content
  in the game by construction.
- **Entry item:** the `ObsidianGate` item handed out on ascension (`ApproachObsidian.cs:49`)
  is currently a **dead-end reward with no destination** **[code]**. Wiring it to open the
  portal to Pagan is the single cheapest, highest-leverage first task.
- **Hub:** the **Ethereal Plane** (`Regions.xml:384`, Lodor facet) is promoted from a
  one-room mount shop into the threshold between the mortal worlds and Pagan. The obsidian
  gate to Pagan lives here, opened by the `ObsidianGate` item / `IsTitanOfEther` check.
  - First-time unlock stays tied to defeating the Black Gate Demon (existing 30-min
    moongate); after that, grant a permanent recall so the hub isn't re-gated each visit.
  - The Ethereal Guide / ethereal-mount vendor stays as flavor; add blackrock-themed mounts
    as Pagan rewards.

---

## 3. Geography (bespoke map — for the artist mock)

A single island-continent sealed by an **impassable storm-sea** (the world is a prison; there
is no sailing out). Proposed facet size **~2560 × 2560 tiles** (square, hand-built; adjustable).
Bespoke terrain — **not** a reuse of existing facets.

### Composition — "the broken wheel"
A wheel around a central wound:

- **Center — the Maelstrom:** a vast circular caldera of black water and ash, lightning
  hammering into it, with a needle of black volcanic glass at its eye crowned by the
  **Obsidian Fortress** (the Guardian's seat). Its spire is visible on the skyline from
  every realm — a constant reminder of the goal.
- **Four elemental realms** fill the quadrants around the center, each touching the outer
  storm-sea, each bleeding into its neighbors at a transitional "seam."
- A perpetual storm rotates around the island with its eye over the Fortress.

```
        N  ~~~~~~~~~~~~ STORM-SEA (impassable hurricane wall) ~~~~~~~~~~~~
        ┌───────────────────────────┬───────────────────────────┐
        │   THE ASHEN REACH  (FIRE) │   ARGENTROCK   (AIR)       │
        │   volcanic badlands,      │   shattered floating peaks,│
        │   lava rivers, ash dunes  │   cloud-wreathed monastery │
        │      ▲ the Foundry        │      ▲ the Wind-Halls       │
   W    ├───────────────┐  ◎◎◎  ┌──┴────────────────────────────┤   E
        │               │ MAELSTROM │                            │
        │  THE MOURN    │ + OBSIDIAN FORTRESS (the Guardian)     │
        │  (EARTH)      └──┬────────┘                            │
        │  dead plains,    │   THE DROWNED REACH  (WATER)        │
        │  barrows,        │   flooded ruins, ice at the deep    │
        │  ▲ Catacombs     │      ▲ Hydros' Ice-Prison    ╔═══╗  │
        └──────────────────┴─────────────────────────────╢ T ╟──┘
        S  ~~~~~ storm-sea ~~~~~ (Tenebrae on a south spur)╚═══╝
```

### The four realms
Each needs its own bespoke palette/tileset.

| Realm | Element | Terrain & landmark | Dungeon mouth | Palette |
|---|---|---|---|---|
| **The Ashen Reach** | Fire | volcanic badlands, lava rivers, ash dunes, one burning mountain | **the Foundry** (forge-mouth in the volcano) | charcoal / ember orange / sulphur |
| **Argentrock** | Air | land torn upward — jagged spires & floating shards linked by failing sky-bridges; monastery on the highest peak | **the Wind-Halls** (bell-tower stair) | pale silver / cold slate / cloud-white |
| **The Drowned Reach** | Water | half-sunken lowland, flooded city breaking the surface, black sea-ice at the deep | **the Sunken Cathedral** (flooded nave into the ice) | teal / drowned grey-green / frost |
| **The Mourn** | Earth | dead grey fog-plains, barrows, sinkholes, the Necropolis | **the Catacombs of Lithos** (cracked mausoleum) | ash grey / bog green / bone white |

**Seams** (where quadrants meet) are short transition ribbons — smoking ridge (fire↔air),
scorched barrow-field (fire↔earth), frozen fen (water↔earth), storm-coast (water↔air).

### Tenebrae — the one city **[canon]**
On a rocky **south spur / tidal island**, joined to the mainland by a stone **causeway** that
floods at high storm. A walled, vertical, rain-black port: tiered streets climbing to the
Tempest's hall, dead docks below. The only lit, living place on the map.

### Map markers for the artist
- **Arrival gate** — an obsidian arch on the south shore by Tenebrae's causeway (exit from
  the Ethereal Plane; the first thing players see).
- **Maelstrom + Obsidian Fortress** at center; ring-causeway gated until the four Foci are held.
- **Four dungeon mouths** (one per realm).
- **A ring-road** around the caldera + four spoke-roads from Tenebrae to each realm.

### Art direction (whole-world)
- Perpetual storm sky, no sun; bruised purples and ash greys, lit only by lightning and each
  realm's elemental glow.
- Recurring motif: **black volcanic glass (obsidian / blackrock)** veins everything, so the
  four colored realms read as one cohesive world.
- The Fortress spire is visible on every realm's skyline.

---

## 4. Tenebrae — the hub

- **Vendors (Pagan-flavored):** a Sorcerer (fire reagents/scrolls), a Necromancer (death
  goods), a Theurgist (air/holy), a Tempest-priest (water), a blackrock/obsidian smith, and a
  blackrock-mount dealer (extends the existing `EtherealDealer`).
- **The Tempest** — Tenebrae's ruler and primary quest-giver; the hub of the faction fork.
- **Player activities here:** bank/repair/restock at the edge of the endgame; pick up realm
  quests; hear **rumors** (reuse `TavernPatrons.GetRareLocation` **[code]**) pointing to the
  current realm bosses.
- **Tone:** no working moongates (Guardian's interference); the Guardian taunts via overhead
  messages (his U8 signature — cheap to implement).

---

## 5. The four realms — detail

Each realm = a `DungeonRegion` (`Regions.xml`) + entrance teleporter + cult faction + boss
(non-Titan) + one **elemental Focus** key item + themed `PaganArtifact` rewards **[code]**.

### 5.1 The Ashen Reach (Fire) — the Sorcerers
- **Faction:** Sorcerers / daemon-binders **[canon]**.
- **Dungeon:** the Foundry / lava caverns.
- **Boss:** **Khumash-Gor**, the bound daemon — *already referenced* as "Scimitar of
  Khumash-Gor" in `PaganArtifact.cs` **[code/canon]**.
- **Focus:** the Cinder Focus.

### 5.2 The Mourn (Earth) — the Necromancers
- **Faction:** Necromancers / the Bone Grinders **[canon]**.
- **Dungeon:** the Catacombs of Lithos / the Necropolis, the Worm.
- **Boss:** **Korghin** the Lich (or the Worm of Lithos) — "Korghin's Fang" referenced in
  `PaganArtifact.cs` **[code/canon]**.
- **Focus:** the Grave Focus.

### 5.3 Argentrock (Air) — the Theurgists
- **Faction:** Theurgists / monks of Stratos **[canon]**.
- **Dungeon:** the Wind-Halls / the Stone. (Note: **"Argentrock" already exists as a region
  name** in this game **[code]** and is U8's air-monk isle — reuse the name.)
- **Boss:** the Stormbound Abbot (a wind-wraith) **[new]**.
- **Focus:** the Zephyr Focus.

### 5.4 The Drowned Reach (Water) — the Drowned / Tempests
- **Faction:** the Drowned **[canon-adjacent]**.
- **Dungeon:** the Sunken Cathedral / Hydros' ice-prison.
- **Boss:** a Drowned Leviathan / the Frozen Herald **[new]**.
- **Focus:** the Tide Focus.

---

## 6. Endgame — the Obsidian Fortress & the Guardian

- Opens once all **four Foci** are held (reuse the pedestal + blackrock collection pattern
  of `PaganBase` / `ObeliskTip` **[code]**).
- Final boss: **the Guardian** — promoted from tribute NPC to apex raid boss. Payoff to the
  whole game's running thread.
- Rewards: apex title, a unique blackrock mount, the "you broke the Guardian" achievement.

---

## 7. What the player can do (activity list)

1. **The Foci arc** — a harder, post-ascension mirror of the elements quest, gated on
   `IsTitanOfEther`: collect the four Foci to open the Fortress.
2. **Faction allegiance** — a morality fork like the Runes-of-Virtue cleanse/corrupt split
   (`RuneBox` pattern **[code]**): serve the Tempest or defect to a cult; allegiance changes
   rewards, a learnable spell line, and title.
3. **Learn a Pagan discipline** — Sorcery / Necromancy / Theurgy / Tempestry, wired into
   existing schools (Sorcery↔Elementalism, Necromancy↔Death Knight, Theurgy↔Holy Man,
   Tempestry↔new storm line) via Pagan-only tomes.
4. **Harvest Pagan-only resources** — blackrock, obsidian, and an oxium-style reagent for
   new recipes.
5. **Boss-hunt** for `PaganArtifact` gift-gear (system exists **[code]**).
6. **Buy blackrock/ethereal mounts** (extend `EtherealDealer`).
7. **Hidden traps & chests** in every dungeon (standard `DungeonRegion` spawn pattern **[code]**).
8. **The Guardian raid** as the capstone.

---

## 8. Boss roster (none are the four Titans)

| Boss | Realm | Source |
|---|---|---|
| Khumash-Gor (fire daemon) | Ashen Reach | name in `PaganArtifact` **[code/canon]** |
| Korghin / the Worm of Lithos | the Mourn | name in `PaganArtifact` **[code/canon]** |
| the Stormbound Abbot | Argentrock | **[new]** |
| the Drowned Leviathan / Frozen Herald | Drowned Reach | **[new]** |
| **the Guardian** | Obsidian Fortress | **[canon]** capstone |
| (per-cult mini-bosses) | each realm | **[new]** |

---

## 9. Technical build plan

### New (bespoke)
- **Facet registration** — add a `RegisterMap( <idx>, <mapID>, <fileIndex>, 2560, 2560, ...,
  "Pagan", ... )` entry in `MapDefinitions.cs` (note: visible maps need mapID 0–3 and a real
  `fileIndex`; a brand-new map needs new client `.mul` terrain — this is the bespoke-art cost).
- **`Land.Pagan`** — add to the `Land` enum + `Lands.LandName` / `LandNameShort` in `Map.cs`.
- **Regions** — new `<Facet name="Pagan">` block in `Regions.xml`: Tenebrae (town region),
  four `DungeonRegion`s (one per realm), the Maelstrom, seams.
- **New mobs** — Stormbound Abbot, Drowned Leviathan, cult mini-bosses; a confrontable
  **Guardian**; flesh out Khumash-Gor / Korghin as mobs.
- **New items** — four **Focus** keys; Pagan reagents/resources; blackrock mounts.

### Reuses existing systems
- **Entry gate** — wire the orphaned `ObsidianGate` (`ApproachObsidian.cs:49`).
- **Collection quest** — `PaganBase` pedestal + `ObeliskTip` blackrock pattern.
- **Boss spawns** — `dangers.map` special-villain lines (cf. `TitanPyros` etc. at `dangers.map:47-50`).
- **Loot** — `PaganArtifact` gift-gear system.
- **Faction fork** — Runes-of-Virtue cleanse/corrupt pattern (`RuneBox`).
- **Rumors** — `TavernPatrons.GetRareLocation`.
- **Heat/region hooks** — `Map.cs` region-name checks (cf. Mind Flayer City heat zone).

---

## 10. Open decisions (need sign-off)
1. **Map size** — 2560×2560 proposed. Bigger (more zone room) or smaller (faster to build)?
2. **New `.mul` terrain** vs. a hybrid (bespoke regions painted over an unused facet)? The
   user requested a *new* map area, so: commit to new terrain art.
3. **Faction fork depth** — full cleanse/corrupt-style branching, or a lighter "favored cult"
   flag?
4. **Disciplines** — new spell schools, or Pagan re-skins of existing schools?
5. **Number of realms** — keep four (one per element) — confirmed.

---

## 11. Phasing
- **Phase 0 (this branch):** this design doc + the cheap structural wins — `ObsidianGate`
  destination wiring, `Land.Pagan`, facet stub, Ethereal-Plane hub upgrade.
- **Phase 1:** Tenebrae (regions, vendors, the Tempest, rumors).
- **Phase 2:** the four realms — regions, dungeon entrances, faction mobs, Foci, the Foci
  collection arc.
- **Phase 3:** the Obsidian Fortress + the Guardian raid; apex rewards.
- **Phase 4:** disciplines, resources/crafting, blackrock mounts, polish (Guardian taunts,
  storm ambiance, achievements).
