# MASTER_ROADMAP.md — Eggbert

**The plan.** What we're building, in what order, and how we know each phase is done.

Sibling docs: `ROADMAP.md` (feature-status checklist) · `FEATURE_IDEAS.md` (idea bucket) · `DESIGN.md` (settled design decisions) · `STORY.md` (narrative draft). GitHub issues are the source of truth for individual work items.

---

## North Star

Eggbert is a story-driven top-down RPG — EarthBound's warmth, Undertale's dialog reactivity — about a wrongly arrested egg-costumed factory worker escaping Eggs Isle prison island. The game's meat is characters, dialog, and puzzles; combat is a dodge-and-parry punctuation, not the point.

**The goal is a complete, polished, playable game — not a tech demo.** The story chain below *is* the game's content. Every zone must reach a state where a player can walk Factory → Eggs Isle → finale → one of three endings without a blocker. Placeholder art is acceptable along the way; a broken scene, a missing transition, or an unresolved design question is not.

---

## State of the project (honest)

What exists is a working engine with floating content:

| Layer | Status |
|---|---|
| Core systems (dialog, combat, puzzles, inventory, equipment, save, audio, cutscenes) | ✅ Working, PoC-grade |
| Factory opening tutorial (5 rooms) | ✅ Shipped — docs/factory-opening.md |
| Story chain wiring (Prison → … → finale) | 🟡 Partially wired |
| Zones | 🟡 All exist as placeholder maps **except Beach (no map at all — cutscene resource only)** |
| Design | 🟡 Tone + protagonist locked; consumables / equipment stats / difficulty open |
| Quality | 🔴 No full playthrough yet; placeholder art everywhere; no QA pass |

The gap between "systems work" and "game exists" is **content and decisions**. This roadmap sequences both.

---

## The three goals

1. **G1 — End-to-end playable story ("the demo")**: one run, New Game → one of three endings, no blockers. Placeholder art is fine. *Current priority — every priority-high demo issue feeds it.*
2. **G2 — Design lockdown**: every open design question settled and written into DESIGN.md / STORY.md. Unlocks content depth.
3. **G3 — Production quality**: placeholder-free art, world feel, QoL, secrets, polish → a release candidate people actually enjoy.

G2 runs in parallel with G1: placeholder content doesn't need the design answers, but deep content does.

---

## Phases

### Phase 1 — Make the story playable end-to-end (G1) · NOW

**Goal:** A single playable run through the full story chain, placeholder art acceptable.

Story chain (flag-driven): Factory → Eggs Isle intake → Prison → Kitchen → Courtyard → Warden's Quarters → Rec Room → Secret Tunnels → Sunnyside Shrine → Solitary → **Beach finale** → Endings (spare/defeat flags determine Good/Mid/Bad).

1. **Stabilize scenes** — fix remaining load failures: #92 (prison), #95 (Great Beyond hierarchy), #96 (OverworldEntrance UID), #127 (dead EggsileSewers→area1 transition), #128 (Kitchen tile atlas errors). *Gate: `tests/VerifyAllLevels.cs` passes every zone.*
2. **Build the missing zone content, in story order** — each ships with its NPCs, dialog, puzzles, and boss/encounter:
   - Prison intake: Frank + cell exploration — #75
   - Kitchen: Grandpa Smith, Chef, Oatmeal boss — #76
   - Courtyard: Egguardo quiz + warden key — #77
   - Warden's Quarters: Yogurt boss + Bacon backstory — #78
   - Rec Room: Waffles spare/fight choice — #79
   - Secret Tunnels: Cereal boss + Sunnyside lore — #80
   - Sunnyside Shrine: cult revelation cutscene — #81
   - Solitary: escape puzzle — #82
   - **Beach: build the zone from scratch** (currently only a cutscene resource) — zone map + transitions = #126, Leader boss + Great Toast God = #83
3. **Wire the story end-to-end** — flags, transitions, cutscenes across all zones — #87
4. **Items** — finish seeded items + missing-item fixes (#89); baseline consumables (#85) and equipment (#86)
5. **Combat balance pass** — damage numbers, parry feel, boss HP — #88
6. **Full playthrough test** — New Game → all three endings (via spare/defeat choices); log and fix every blocker — #90

**Exit criteria:**
- `tests/VerifyAllLevels.cs` loads AND instantiates every level, resolves all transitions + warp entries, zero failures
- One run reaches all three endings with no blockers and no fatal console errors
- All Phase 1 issues closed

### Phase 2 — Design lockdown (G2) · parallel with Phase 1

Settle and document. Each blocks specific content depth:

| Question | Blocks | Status |
|---|---|---|
| Consumables: names, effects, heal values — #6 | Inventory depth, merchant/barter design | Open |
| Equipment stats: what Attack/Speed actually do — #7 (reopened 2026-08-05; closed 07-14 with no decision) | Builds, enemy tuning | Open |
| Difficulty: HP scaling? easy mode? | Final balance pass | Open |
| Full narrative: plot details beyond the beats — #9 | Final dialog, ending polish | Tone + protagonist locked in STORY.md |

**Exit criteria:** DESIGN.md and STORY.md contain no open questions; design issues closed.

### Phase 3 — Content depth & world feel (G3, first half)

Turn placeholder zones into lived-in places:
- **NPC behaviors**: quest-giver (#56), gossip chains (#36), conditional dialog (#35)
- **Secrets & exploration**: hidden paths, breakable walls, post-dialog rewards — #53
- **Inventory QoL**: keyring UI (#58), interaction-prompt toggle (#66), save icon (#70)
- **World feel** (from FEATURE_IDEAS.md): ambient particles per zone, footstep/door sounds, flickering lights, hanging signs, echoey reverb zones
- **Item depth**: full consumable roster (#85), equipment expansion (#86)

**Exit criteria:** each zone has 2–3 side-content beats beyond the main story; no empty-feeling corridors.

### Phase 4 — Polish & release candidate (G3, second half)

- Replace all placeholder art (backdrops, interaction/encounter markers, sprites) with real assets
- Juice: screen shake, particles (dust, parry sparkle), zone-transition stingers
- Audio pass: dialog voice, missing SFX, ambient loops everywhere
- Difficulty options, if locked in Phase 2
- Full QA playthrough; performance check at 640×360 scale

**Exit criteria:** release candidate — no placeholders, no known blockers, one full QA pass green.

### Phase 5 — Post-release backlog

Everything in FEATURE_IDEAS.md not already pulled in (fishing, outfits, photo mode, phone calls, rhythm-game boss…). Pull from the bucket when it serves the game.

---

## Cross-cutting rules

- **Gate before every commit**: `dotnet build` (0 warnings) + `tests/VerifyAllLevels.cs` headless run
- **Issue per non-trivial task**, commit with `Closes #N` on main (see AGENTS.md)
- **C# only** for game code; GDScript stays in `addons/` only
- **No test project, no CI** — the scene-load verifier is the sanity net

---

## Issue map

| Phase | Issues |
|---|---|
| 1 — Stabilize | #92 ✅ #95 ✅ #96 ✅ (all closed 08-05, verified) |
| 1 — Content | #75 #76 #77 #78 #79 #80 #81 #82 #83 #126 |
| 1 — Wire / balance / test | #87 #88 #89 #90 |
| 2 — Design | #6 #7 #9 (+ difficulty) |
| 3 — Depth & QoL | #35 #36 #53 #56 #58 #66 #70 #85 #86 |
| 4 — Polish | new issues as found |
| 5 — Backlog | FEATURE_IDEAS.md |

Milestones mirror these phases 1:1 (Phase 1–Phase 5). Every open issue must carry a phase milestone.
