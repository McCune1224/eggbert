# Combat UI & Equipment Design — Research & Proposal

> Research-backed design proposal for the combat scene UI and the pause-menu
> equipment screen, plus a roster of equipment that changes *how* combat works
> (parry feel, bullet speed, survivability). Written for the Eggbert repo.
> Status: **proposal — feeds design issue #7 (equipment stats) and phase 3 item
> depth (#85/#86). Nothing here is implemented yet.**

---

## 1. Current state (verified against the code)

| System | File | What exists |
|---|---|---|
| Combat arena | `combat/arena/CombatArena.cs` | Node2D room; tracks `EnemiesRemaining`; fires `BattleWon`/`BattleLost`; spawns `CombatHUD` |
| Combat HUD | `combat/ui/CombatHUD.cs` | CanvasLayer: player HP bar (top-left), enemy name+HP bars (top-right). That's the whole UI. |
| Parry | `combat/components/ParryComponent.cs` | Proximity ring (J key). `ParryRadius=110`, `ParryDamage=10`, `Cooldown=0.5s`. Reflects `RedBullet`s, damages `RollingEgg`-type enemies in radius. Ring flashes cyan (success) / red (miss). |
| Bullets | `combat/bullets/RedBullet.cs` | Fixed speed per shot, optional homing. `SetDirection(dir, speed)`. 10 dmg on player hit. |
| Enemies | `combat/enemies/CombatOatmeal.cs` (+Cereal, Yogurt, SunnysideLeader) | State machine idle→telegraph→attack→cooldown; 4 bullet flavors (spread/homing/aimed/burst). Telegraph = red color pulse. |
| Equipment | `autoload/Equipment.cs` | 3 slots (Weapon/Armor/Accessory). Wired stats: MaxHP, Defense, ParryRadius, ParryDamage. **Attack is wired into reflected-bullet + parry damage; Speed is computed but unused.** `PreviewDeltas()` only covers HP/ATK/DEF/SPD. |
| Item def | `components/items/Item.cs` + `ItemDatabase.cs` | Flat Resource; fields: ATK/DEF/SPD/MaxHP/ParryRadius/ParryDamage boosts. All items code-defined. |
| Pause menu | `ui/OverworldMenu.tscn` + `.cs` | Persistent CanvasLayer under GameController (present during combat too). Items panel has Key/Consumable/Equipment tabs, ItemList + detail pane + Use/Equip button. Equip/unequip already works, incl. `PreviewDeltas`. |
| Design doc | `DESIGN.md` | Combat = "dodge + counter", minimal overlay HUD. **"No item usage during combat (combat is dodge-only)."** |

**Two gaps found while verifying:**
1. `OverworldMenu` is a persistent CanvasLayer, so **Esc during combat opens the
   Items panel and the Use button can heal mid-battle** — contradicting DESIGN.md.
   Needs an explicit decision (see §4).
2. `Equipment.PreviewDeltas()` and the menu's stats label **don't show parry
   stats at all**, even though they're the combat-relevant ones.

---

## 2. Design principles (from research + repo constraints)

Research sources: Undertale/Deltarune combat system analysis (bullet-hell as the
enemy phase; graze/TP meter; DEFEND), Undertale's equipment vocabulary (INV =
invincibility frames, passive regen, consumable-boosting weapons), Sekiro/Bloodborne
parry taxonomy (**usability / versatility / impact**), and bullet-hell roguelite
parry design (Deflector).

Principles for Eggbert specifically:

1. **Keep combat realtime arena dodge+parry.** DESIGN.md and the North Star
   ("combat is a dodge-and-parry punctuation, not the point") lock this. A full
   Undertale-style turn menu (FIGHT/ACT/ITEM/MERCY + bullet board) is a different
   game; it's documented as an alternative in §6 but *not* recommended.
2. **The HUD must fit 640×360.** Undertale's bottom menu bar eats ~25% of the
   screen; we can't afford that in a realtime arena. Corner panels + in-world
   telegraphs + a single optional meter.
3. **Parry is the game's one offensive verb** → equipment that changes *how parry
   feels* (radius, cooldown, reflect speed, side effects) is more fun than flat
   +ATK. This follows the parry taxonomy: better usability (longer radius /
   shorter cooldown), versatility (reflect more bullet types, AoE), impact
   (damage, shockwaves, healing).
4. **Every equipment effect must be readable in the pause menu.** Plain-language
   effect lines ("Slows enemy bullets 25%"), not just stat numbers.
5. **Overworld-only consumables stay** (DESIGN.md). Equipment is the vehicle for
   combat customization. All effects are *passive*, applied before combat starts —
   no mid-combat fiddling (see §4).

---

## 3. Combat scene UI — recommendations

### 3.1 Do now (Phase 1, cheap, high value)

| # | Change | Why / reference |
|---|---|---|
| C1 | **Parry cooldown indicator.** The ring flash already shows success/miss; add a small "PARRY" pip/badge in the HUD that greys out during cooldown (0.5s default). Better: make the in-world ring *fill back up* — the arc closes as cooldown expires. | Parry is the core verb; its cooldown is currently invisible between presses. Sekiro-style readability. |
| C2 | **Telegraph warning glyph.** During an enemy's Telegraph state, draw a small "!" (or the attack name) above the enemy's head, scaling with remaining windup. | Undertale telegraphs intent with animations; our enemies already pulse red. A glyph makes the timing readable from the corner of the eye. Pure `_Draw` addition to `CombatOatmeal`. |
| C3 | **Battle banners.** "FIGHT!" on arena entry, "VICTORY!" / "YOU WON" (Undertale spelling, for flavor) on win, fading labels via the existing `HudLabel` variation + tween (same pattern as the PARRY! popup in `ParryComponent`). | Undertale's battle bookends; cheap, huge personality. |
| C4 | **Enforce "no items in combat."** In `OverworldMenu.OnUsePressed`, disable consumable Use (and preferably equipment changes) while `GameController.Instance.CurrentLevel is CombatArena`. Show "(unavailable in combat)" on the Use button. | Brings behavior in line with DESIGN.md; decision needed (§4). |
| C5 | **Fix stat preview parity.** Add ParryRadius/ParryDamage (+ new stats from §5) to `Equipment.PreviewDeltas()` and the menu stats label. | Today equipping a parry item shows no preview at all. |

### 3.2 Next (Phase 3 — the single biggest fun-add)

| # | Change | Why / reference |
|---|---|---|
| C6 | **Graze meter (Deltarune-style).** Bullets that pass within a graze radius of the player (without hitting) fill a small meter (top-center or bottom-left). Full meter → a "GRAZE" trigger: screen-clearing parry shockwave that damages all enemies, or a heal. | Turns dodging from passive survival into active risk/reward. This is the genre's proven answer to "how do you make dodging fun." Gives the HUD a second, dynamic element. Equipment can modify graze radius/reward (§5). |
| C7 | **Damage numbers.** Floating "+10" / "-10" text on parry hits and player damage (existing label+tween pattern). | Undertale shows numbers; standard juice. |
| C8 | **Block-charge pips / regen tick** for armor effects (§5) — Bubble Wrap charges shown as shell pips near the HP bar; regen shows a "+2" tick. | Armor effects must be visible to feel real. |

### 3.3 Later (Phase 4 polish)

- Screen shake on player damage / parry (there's no shake anywhere yet).
- Parry sparkle particles (FEATURE_IDEAS has "sparkle on parry").
- Hit-stop (brief freeze on reflected-bullet hits) — Undertale's signature punch.
- Enemy HP bars get damage-flash (white flicker on hit).

---

## 4. Pause menu (equipment screen) — recommendations

The Items panel is already functional; these changes make it a *loadout* screen.

### 4.1 Slot overview (the biggest gap today)

There is no way to see the current loadout at a glance. Add an **equipment slot
strip** at the top of the Equipment tab (or its own tab):

```
WEAPON   Baseball Bat      +5 ATK          [Unequip]
ARMOR    Soda Can Armor    +8 DEF          [Unequip]
ACCESS.  Lucky Yolk        +2 SPD          [Unequip]
```

- Each row = slot, equipped item, its effect summary, Unequip button.
- Clicking a row selects that item in the ItemList for detail.
- Implementation: pure addition to `OverworldMenu.cs` + `OverworldMenu.tscn`;
  data from `Equipment.GetEquipped(slot)`.

### 4.2 Full stat comparison

Extend the detail pane so selecting any equipment shows a **current → new**
comparison covering **all** combat-relevant stats, color-coded:

```
ATK        +3 → +5   (+2)
DEF        +0 → +8   (+8)
PARRY RADIUS  110 → 132  (+22)
PARRY COOLDOWN 0.50 → 0.40s
BULLET SLOW   — → 25%
```

- `Equipment.PreviewDeltas()` is the hook; extend it to every stat field on
  `Item` (including new ones from §5). Color via RichTextLabel or label
  modulate (green/red) — the theme has no rich-text styles yet, keep it simple.
- Also show the **derived totals** (e.g. "Parry radius 132" rather than just the
  delta) so players learn what the numbers mean.

### 4.3 Readable effects

Replace the raw `+N` line with a two-part description:
- **Numeric line** (existing, extended): `+5 ATK, +8 DEF`
- **Effect line** (new, plain language): `"Slows enemy bullets 25%"`, `"Parry
  reflects bullets 40% faster"`, `"Blocks the first 3 hits of each battle"`

A small `Item.EffectSummary()` helper (or a switch on fields in the menu) keeps
`Item` flat per repo convention. This is the difference between "equipment is
numbers" and "equipment changes how the game plays."

### 4.4 Small QoL

- **Slot badge** on ItemList rows: `[W] Baseball Bat`, `[A] Soda Can Armor`.
- Sort equipment rows by slot (Weapon → Armor → Accessory), then name.
- **Unequip from the slot strip** (4.1) — no need to hunt the item in the list.
- Icons: `Item.Icon` exists but no item has art; leave placeholder, flag for
  Phase 4 art pass.
- Keep theme conventions: `MenuPanel`, `MenuButton`, `TabButton`, `ItemListRetro`,
  `MenuLabelSmall` — `tests/VerifyUiTheme.cs` checks these.

### 4.5 Decision needed: mid-combat pause menu

Esc currently works inside arenas (menu is a persistent CanvasLayer). Options:

- **A (recommended): Lock build during combat.** Equipment tab read-only + Use
  disabled. Realtime combat has no "turn cost" to pay, so free swapping trivializes
  builds. Undertale allows swaps because it costs a turn.
- **B: Allow everything.** Contradicts DESIGN.md's "dodge-only" line; needs a
  DESIGN.md edit.
- **C: Combat pause = stripped menu** (Resume/Settings only). Most faithful to
  "combat is its own screen."

Pick one and update DESIGN.md accordingly (this is issue #7 territory).

---

## 5. Equipment design — the fun part

### 5.1 New stat fields to add to `Item` (flat scalars, repo convention)

```csharp
[Export] public float BulletSlowFactor;        // 0.25 = enemy bullets 25% slower
[Export] public float ParryCooldownReduction;  // seconds off the 0.5s parry cooldown
[Export] public float ReflectSpeedBoost;       // multiplier on reflected bullet speed
[Export] public float GrazeRadiusBoost;        // px added to graze detection (C6)
[Export] public float HomingResistance;        // 0.5 = homing strength halved
[Export] public int   BlockCharges;            // hits absorbed per combat (Bubble Wrap)
[Export] public int   RegenPerSecond;          // combat HP regen (Stained Apron)
[Export] public float EvadeChance;             // 0.15 = 15% chance to ignore bullet dmg
[Export] public float InvulnerabilityBoost;    // seconds of iframes after a hit
[Export] public float DashCooldownReduction;   // seconds off dash cooldown
```

Pattern to follow: `Equipment` already computes totals via `GetTotalParryRadius()`
etc. Extend with `GetTotalBulletSlow()`, `GetTotalParryCooldownReduction()`, …
pushed to systems through a small static facade:

```csharp
// combat/CombatStats.cs — read-only snapshot, refreshed on equip/unequip
public static class CombatStats
{
    public static float BulletSlowMultiplier = 1f;   // 0.75 = bullets at 75% speed
    public static float HomingResistance = 0f;
    public static float ReflectSpeedMultiplier = 1f;
    public static int   BlockCharges = 0;
    public static float RegenPerSecond = 0f;
    public static float EvadeChance = 0f;
    public static float InvulnerabilityBoost = 0f;
    public static void Refresh() { /* read Equipment.Instance totals */ }
}
```

Hooks are one-liners in existing files:
- `RedBullet._Process`: `speed` multiplied by `CombatStats.BulletSlowMultiplier`;
  homing lerp strength × `(1 - CombatStats.HomingResistance)`; reflect speed set
  via `SetDirection` × `ReflectSpeedMultiplier`.
- `ParryComponent.UpdateStats()`: add cooldown param.
- `HealthComponent.TakeDamage()`: evade roll; iframe duration; `BlockCharges`
  decrement (skip damage while > 0).
- `CombatArena._Process()`: apply `RegenPerSecond` to player.
- `Player` (dash): cooldown − `DashCooldownReduction`.
- Wire the currently-dead **`SpeedBoost`** into `Player` move speed — this also
  settles part of design issue #7.

### 5.2 Roster (tiered, egg-themed, all passive)

Legend: existing stat • new field (5.1) • needs small new behavior

**Weapons — change how parry works**

| Item | Slot | Effect | Notes |
|---|---|---|---|
| Butter Knife (exists) | Weapon | +3 ATK | Starter |
| Baseball Bat (exists) | Weapon | +5 ATK | |
| Whisk | Weapon | Reflected bullets fly **40% faster** (ReflectSpeedBoost), +2 ATK | "Whisk it good." Fast reflect = more DPS, harder to read |
| Spatula | Weapon | +22 ParryRadius, −1 ParryDamage | "Flip more, hit softer." Usability trade-off |
| Slotted Spoon | Weapon | −0.2s parry cooldown (ParryCooldownReduction) | Rapid parries; pairs with dense patterns |
| Cast Iron Frying Pan | Weapon | +4 ParryDamage; reflected bullets **explode** (30px AoE) on impact | Needs small explosion behavior on `RedBullet` — highest-impact weapon |
| Ladle | Weapon | Parry restores **3 HP per reflected bullet** | Undertale Burnt-Pan lineage (healing weapon) |
| Chopsticks (long shot) | Weapon | Parry *catches* a bullet; next parry fires all held bullets | Phase 5; needs held-bullet state |
| Egg Timer | Weapon | Enemy bullets **25% slower** (BulletSlowFactor) | Time-manipulation fantasy on a weapon |

**Armor — survivability with trade-offs**

| Item | Slot | Effect | Notes |
|---|---|---|---|
| Egg Shell (exists) | Armor | +5 DEF | |
| Soda Can Armor (exists) | Armor | +8 DEF | |
| Eggshell Helm (exists) | Armor | +4 DEF, +10 MaxHP | |
| Pot Lid | Armor | +6 DEF, +15 ParryRadius | Defense + parry usability |
| Bubble Wrap | Armor | **Blocks first 3 hits** of each combat (BlockCharges) | HUD pips (C8); Undertale's INV philosophy |
| Stained Apron | Armor | +6 DEF, **regen 2 HP/s in combat** (RegenPerSecond) | Undertale Stained Apron homage |
| Tin Foil Hat | Armor | +3 DEF, **homing bullets 50% weaker** (HomingResistance) | "Blocks the mind-reading rays." |
| Silicone Baking Mat | Armor | +4 DEF, −0.15s dash cooldown | Mobility armor |
| Cracked Carton | Armor | +12 DEF, **−15% move speed** (wires SpeedBoost) | First heavy-armor trade-off |
| Hardboiled Shell | Armor | +20 DEF, −20% move speed | Endgame tank |

**Accessories — weird, build-defining**

| Item | Slot | Effect | Notes |
|---|---|---|---|
| Lucky Yolk (exists) | Accessory | +2 SPD | |
| Dice (exists) | Accessory | +3 ATK, +3 DEF | |
| Butter | Accessory | Enemy bullets **20% slower** | "Everything's better with butter." |
| Molasses | Accessory | Bullets 35% slower, **player −10% speed** | Trade-off version |
| Hourglass | Accessory | Dash leaves a **2s bullet-time zone** (bullets inside at 50%) | Needs zone behavior; strong |
| Graze Charm | Accessory | +40 graze radius (GrazeRadiusBoost) | Builds the C6 meter faster |
| Lucky Horseshoe | Accessory | 15% evade (EvadeChance) | Slot-machine joy |
| Rubber Band | Accessory | Reflected bullets **bounce once off walls** | Needs bounce flag; fun in corridors |
| Stopwatch | Accessory | Enemy telegraphs last **30% longer** (windup ×1.3) | "The whole kitchen slows down" — big parry enabler |
| Wedding Ring | Accessory | +50% parry damage | Late-game impact |
| Sunglasses | Accessory | −0.1s parry cooldown, +0.5s iframes (InvulnerabilityBoost) | "Too cool to get hit twice." |

Balance guardrail: keep the **build identity** readable — every trade-off item
(Spatula, Molasses, Cracked Carton) trades one visible stat for another so the
menu comparison (4.2) always shows *why* you'd equip it.

### 5.3 Which items land where (phases)

- **Phase 1 (#86 baseline equipment):** Whisk, Spatula, Slotted Spoon, Pot Lid,
  Bubble Wrap, Butter. All use existing or trivial-new fields; exercises the
  preview/comparison UI.
- **Phase 3 (#85/#86 item depth):** Frying Pan, Ladle, Stained Apron, Tin Foil
  Hat, Molasses, Graze Charm, Stopwatch, Hourglass. These need the C6 graze
  system and small behaviors.
- **Phase 5 / long shots:** Chopsticks, Rubber Band, Egg Timer, Sunglasses.

---

## 6. The road not taken: Undertale-style turn menu

For the record, the full FIGHT/ACT/ITEM/MERCY + bullet-board redesign:
- Requires a turn loop (menu phase ↔ dodge phase), action economy, item usage in
  combat, ACT/mercy content per enemy, and a bottom menu bar on a 360px screen.
- Contradicts DESIGN.md ("dodge + counter", "combat is dodge-only punctuation").
- **Recommendation: don't.** If the game ever wants menu-y combat, the smallest
  version that keeps the identity is a **pre-battle encounter menu** (FIGHT /
  ITEM / SPARE choices before the arena starts, Undertale-style flavor line in
  the HUD banner). That's a Phase 5 discussion, not a Phase 1-3 one.

---

## 7. Implementation order (mapped to MASTER_ROADMAP)

| Phase | Work | Issues |
|---|---|---|
| 1 | C1–C5 (parry pip, telegraph glyph, banners, no-items-in-combat, preview parity); wire SpeedBoost; baseline items (5.3 Phase 1) | #7 (partial), #86, #88 |
| 2 | Settle #7 using §4.5 + §5.1 as the proposal; write decisions into DESIGN.md | #7 |
| 3 | C6–C8 (graze meter, damage numbers, block pips); full item roster; slot-overview + comparison UI (4.1–4.4) | #6, #85, #86 |
| 4 | Juice: shake, hit-stop, sparkles, banner art, item icons | new issues |

**Gate before any commit:** `dotnet build` (0 warnings) + `tests/VerifyAllLevels.cs`
headless; new UI additions should extend `tests/VerifyUiTheme.cs` where they touch
theme variations. `ItemDatabase.cs` is a hot file — **append** items only.
