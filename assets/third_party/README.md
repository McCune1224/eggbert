# Third-Party Placeholder Asset Library

Vetted intake of external placeholder assets for Eggbert. Each subdirectory preserves upstream filenames, directory structure, and license/readme. None of these packs change existing level scenes, gameplay code, or authored character art; they are source material for a later content pass.

Source: GitHub issue #91 — "Asset intake: vetted placeholder asset library (CC0/explicit-license packs)".

## Packs present

| Directory | Pack | License | Source |
|---|---|---|---|
| `kenney_ui/` | Kenney UI Pack (2.0) | CC0 1.0 (see `kenney_ui/License.txt`) | https://kenney.nl/assets/ui-pack |
| `kenney_game_icons/` | Kenney Game Icons | CC0 1.0 (see `kenney_game_icons/license.txt`) | https://kenney.nl/assets/game-icons |
| `kenney_interface_sounds/` | Kenney Interface Sounds (1.0) | CC0 1.0 (see `kenney_interface_sounds/License.txt`) | https://kenney.nl/assets/interface-sounds |
| `kenney_digital_audio/` | Kenney Digital Audio | CC0 1.0 (see `kenney_digital_audio/License.txt`) | https://kenney.nl/assets/digital-audio |
| `fonts/silkscreen/` | Silkscreen (Regular & Bold) | SIL Open Font License 1.1 (see `fonts/silkscreen/LICENSE-OFL.txt`) | https://fonts.google.com/specimen/Silkscreen |

## Not imported

**0x72 16×16 RPG Character Sprite Sheet** (https://0x72.itch.io/16x16-rpg-character-sprite-sheet) is intentionally **not** part of this intake. The source URL returned HTTP 404 at intake time and the itch.io profile page is bot-blocked, so the package and its stated CC0 terms could not be verified against upstream.

Per the intake plan's contingency for that pack, character placeholder art continues to come from the existing Mixel main-character sheets at `assets/temp_mixel_tileset/Top-Down RPG 32x32 by Mixel v1.7/MainCharacter v.1.0/`. The Mixel pack's local `LICENSE.txt` is the authoritative terms for that use. No retry of the 0x72 download is staged in this pass.

## Existing backbone (unchanged)

The Mixel 32×32 pack at `assets/temp_mixel_tileset/Top-Down RPG 32x32 by Mixel v1.7/` remains the authoritative 32×32 environment backbone (ground/water/nature/ruins/UI/items/main character). The reusable ground atlas `assets/tilemaps/mixel_ground_tileset.tres` is untouched — no hand-written nested `.tres` TileSet resources were added.

## Not promoted

`assets/temp_hyptosis_tiles/` is **not** promoted into this vetted library. Its CC BY 3.0 terms require attribution that the repository currently has no corresponding license record for beside those temporary sheets. `assets/audio/music/indie_meditations/license_cc_by_4.0.txt` remains valid for that music and is not relabeled as CC0.

## Intended use (later content pass, not this intake)

- Kenney UI Pack textures → temporary pause/menu panels, buttons, tabs, sliders, bars.
- Kenney Game Icons → temporary inventory/status/map icons. These stay separate from `assets/items/ICON_MAPPING.md`.
- Kenney Interface Sounds / Digital Audio → optional SFX pool for later `AudioManager.PlaySfx()` assignments; not wired into the dialog voice system.
- Silkscreen → temporary generic placeholder UI text, chosen over the Mixel Adventurer font for its explicit separate OFL terms. `assets/fonts/yoster.ttf` and the Mixel Adventurer font are unchanged.