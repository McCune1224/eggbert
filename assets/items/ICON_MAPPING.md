# Item Icon Mapping Reference

## SODA Realistic 32×32 IconSet (for inventory UI)

Source: `assets/items/icons/IconSet.png` — 512×640, 16 cols × 20 rows
Individual slices: `assets/items/icons/icon_NNNN.png`

### RPG Maker VX Ace layout (standard)

| Row | Indices | Category | Notes |
|-----|---------|----------|-------|
| 0   | 000–015 | Swords, weapons | |
| 1   | 016–031 | Axes, spears, bows | |
| 2   | 032–047 | Guns, claws, instruments | |
| 3   | 048–063 | Shields, heavy armor | |
| 4   | 064–079 | **Helmets, headgear** | eggshell_helm candidate |
| 5   | 080–095 | Accessories, rings | |
| 6   | 096–111 | **Potions / drinks** | hardboiled_egg candidate |
| 7   | 112–127 | **Food / herbs** | scrambled_egg candidate |
| 8   | 128–143 | Books, scrolls | |
| 9   | 144–159 | Special / magic items | |
| 10  | 160–175 | **Keys / tools** | rusty_key & cell_key candidates |
| 11  | 176–191 | Gems, valuables | |
| 12  | 192–207 | Materials, ore | |
| 13–19 | 208–319 | Misc items, monsters | |

### Suggested assignments

| Item ID | DisplayName | Icon path | Notes |
|---------|-------------|-----------|-------|
| `rusty_key` | Rusty Key | `res://assets/items/icons/icon_0162.png` | Row 10, key area — verify in editor |
| `cell_key` | Cell Key | `res://assets/items/icons/icon_0165.png` | Row 10, different key type |
| `hardboiled_egg` | Hardboiled Egg | `res://assets/items/icons/icon_0099.png` | Row 6, potion/food — verify |
| `scrambled_egg` | Scrambled Egg | `res://assets/items/icons/icon_0114.png` | Row 7, food — verify |
| `eggshell_helm` | Eggshell Helm | `res://assets/items/icons/icon_0064.png` | Row 4, helmet — verify |

To assign in C# (`ItemDatabase.cs`):
```csharp
Icon = ResourceLoader.Load<Texture2D>("res://assets/items/icons/icon_0162.png")
```

---

## Mixel Item Sprites (for world pickups)

Source: `assets/items/sprites/` — 24 slices from the Mixel items PNG (384×64 = 12×2 grid)

| Index | Grid pos | Likely content |
|-------|----------|---------------|
| 0000  | (0,0)    | |
| 0001  | (1,0)    | |
| 0002  | (2,0)    | |
| 0003  | (3,0)    | |
| 0004  | (4,0)    | |
| 0005  | (5,0)    | |
| 0006  | (6,0)    | |
| 0007  | (7,0)    | |
| 0008  | (8,0)    | |
| 0009  | (9,0)    | |
| 0010  | (10,0)   | |
| 0011  | (11,0)   | |
| 0012–0023 | Row 1 | Repeat of similar items (row 2 = variants) |

Open `assets/items/sprites/item_sprite_NNNN.png` in the Godot editor to preview and assign to pickups.

---

## World pickup → Sprite assignment

| Pickup ItemId | Suggested sprite |
|---------------|-----------------|
| `rusty_key`   | `item_sprite_000X.png` (find key-like sprite) |
| `cell_key`    | `item_sprite_000Y.png` (different key) |
| `hardboiled_egg` | `item_sprite_000Z.png` (egg/food sprite) |
| `scrambled_egg` | `item_sprite_000W.png` (food variant) |

Open the Mixel items sheet at `assets/items/sprites/Topdown RPG 32x32 - Items.PNG` in Godot's asset browser to preview all 24 sprites side by side.
