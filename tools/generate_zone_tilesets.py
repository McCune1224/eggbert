#!/usr/bin/env python3
"""Generate three themed 16x16-cell tileset atlases for the Eggs Isle "First Night"
exile (replaces the reused 'prison tileset' for the new maps):

  dock_tileset.png       — moonlit pier: wood planks, water, sand, props
  gatehouse_tileset.png  — stone booking room: stone floors/walls, desk, rug
  overflow_tileset.png   — industrial wing: concrete, bars, pipes, boiler, rubble

Palette matches the game's night-prison look (moody blues #3a4466..#7083a9 with the
orange #f77622 accent). Each atlas is 12x12 cells (192x192 px), 16 px per cell.
The companion .tres files (dock_tileset.tres etc.) reference the exact cells used.

Usage: python3 tools/generate_zone_tilesets.py   (writes assets/tilemaps/*.png)
Then run `godot --headless --path . --import` to refresh .import sidecars.
"""
import os
import random

from PIL import Image

CELL = 16
ATLAS = 12  # cells per side

# ---- palette (night prison) ----
INK = (26, 32, 56)          # #1a2038 near-black
DEEP = (58, 68, 102)        # #3a4466
NIGHT = (66, 78, 116)       # #424e74
BLUE = (75, 88, 114)        # #4b5872
STEEL = (90, 104, 136)      # #5a6988
LIGHT = (94, 109, 141)      # #5e6d8d
PALE = (103, 120, 153)      # #67789b
SKY = (112, 131, 169)       # #7083a9
MOON = (184, 198, 221)      # #b8c6dd
ORANGE = (247, 118, 34)     # #f77622
LANTERN = (255, 179, 92)    # #ffb35c
WOOD_A = (106, 88, 64)      # planks
WOOD_B = (90, 74, 53)
WOOD_C = (70, 56, 37)
WOOD_D = (46, 36, 25)
SAND = (88, 92, 110)
PEBBLE = (120, 126, 148)
STONE_A = (100, 108, 128)
STONE_B = (88, 96, 116)
STONE_C = (74, 82, 100)
CONC_A = (109, 118, 136)
CONC_B = (94, 102, 120)
CONC_C = (78, 86, 102)
RUST = (180, 85, 46)
SLIME = (92, 122, 96)
METAL = (130, 140, 158)


def new_tile():
    return [[(0, 0, 0, 0)] * CELL for _ in range(CELL)]


def put(buf, x, y, c, a=255):
    if 0 <= x < CELL and 0 <= y < CELL:
        buf[y][x] = (c[0], c[1], c[2], a)


def rect(buf, x0, y0, x1, y1, c, a=255):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(buf, x, y, c, a)


def hline(buf, y, x0, x1, c, a=255):
    for x in range(x0, x1 + 1):
        put(buf, x, y, c, a)


def vline(buf, x, y0, y1, c, a=255):
    for y in range(y0, y1 + 1):
        put(buf, x, y, c, a)


def dither(buf, x0, y0, x1, y1, c, density, rng, a=255):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if rng.random() < density:
                put(buf, x, y, c, a)


def grain(buf, c, density, rng, a=255):
    dither(buf, 0, 0, CELL - 1, CELL - 1, c, density, rng, a)


def wood_plank(buf, rng, worn=False):
    rect(buf, 0, 0, 15, 15, WOOD_B)
    # horizontal boards (4 rows of 4px)
    for row in range(4):
        y0 = row * 4
        c = WOOD_A if (row + (rng.random() < 0.3)) % 2 == 0 else WOOD_B
        rect(buf, 0, y0, 15, y0 + 3, c)
        hline(buf, y0 + 3, 0, 15, WOOD_C)
        # board joints (vertical seams, staggered)
        seam = 3 + (row * 3) % 9
        if row % 2 == 0:
            vline(buf, seam, y0, y0 + 3, WOOD_C)
        else:
            vline(buf, (seam + 5) % 14, y0, y0 + 3, WOOD_C)
        # nail dots
        for nx in (2, 13):
            put(buf, nx, y0 + 1, WOOD_D)
            put(buf, nx, y0 + 2, WOOD_D)
    if worn:
        dither(buf, 0, 0, 15, 15, WOOD_D, 0.12, rng)
        dither(buf, 0, 0, 15, 15, (0, 0, 0, 0), 0.05, rng)  # chips
        for _ in range(2):
            x, y = rng.randrange(1, 14), rng.randrange(1, 14)
            rect(buf, x, y, x + 1, y + 1, WOOD_D)


def water(buf, depth, rng, foam=False):
    c = (DEEP, NIGHT, BLUE)[depth]
    rect(buf, 0, 0, 15, 15, c)
    for row in range(5):
        y = 2 + row * 3
        hline(buf, y, 0, 15, (c[0] + 18, c[1] + 18, c[2] + 26))
        if rng.random() < 0.5:
            hline(buf, y + 1, rng.randrange(0, 8), rng.randrange(9, 15), (c[0] + 10, c[1] + 10, c[2] + 14))
    if foam:
        for x in range(0, 16, 2):
            if rng.random() < 0.6:
                put(buf, x, 1, MOON, 160)
                put(buf, x, 2, MOON, 120)


def sand_tile(buf, rng):
    rect(buf, 0, 0, 15, 15, SAND)
    grain(buf, PEBBLE, 0.15, rng)
    grain(buf, (70, 74, 92), 0.08, rng)


def pebble_tile(buf, rng):
    rect(buf, 0, 0, 15, 15, SAND)
    for _ in range(6):
        x, y = rng.randrange(1, 14), rng.randrange(1, 14)
        c = PEBBLE if rng.random() < 0.5 else (105, 111, 132)
        rect(buf, x, y, x + 2, y + 2, c)
        put(buf, x + 1, y + 1, (70, 74, 92))
    grain(buf, (70, 74, 92), 0.1, rng)


def stone_floor(buf, rng, cracked=False):
    rect(buf, 0, 0, 15, 15, STONE_A)
    # mortar grid
    hline(buf, 7, 0, 15, STONE_C)
    hline(buf, 15, 0, 15, STONE_C)
    vline(buf, 7, 0, 7, STONE_C)
    vline(buf, 15, 0, 15, STONE_C)
    # per-block shading
    for (x0, y0, x1, y1) in [(1, 1, 6, 6), (9, 1, 14, 6), (1, 9, 6, 14), (9, 9, 14, 14)]:
        c = STONE_A if rng.random() < 0.5 else STONE_B
        rect(buf, x0, y0, x1, y1, c)
        put(buf, x0, y0, STONE_B if c == STONE_A else STONE_C)
    if cracked:
        vline(buf, 11, 3, 15, (40, 46, 60))
        hline(buf, 11, 8, 12, (40, 46, 60))
        dither(buf, 0, 0, 15, 15, STONE_C, 0.06, rng)


def stone_wall(buf, rng, dark=False, mossy=False):
    base = STONE_B if not dark else STONE_C
    rect(buf, 0, 0, 15, 15, base)
    # big blocks
    for (x0, y0, x1, y1) in [(0, 0, 7, 5), (8, 0, 15, 5), (4, 6, 11, 11), (0, 6, 3, 11), (12, 6, 15, 11), (0, 12, 7, 15), (8, 12, 15, 15)]:
        c = base if rng.random() < 0.6 else (base[0] + 8, base[1] + 8, base[2] + 10)
        rect(buf, x0, y0, x1, y1, c)
    hline(buf, 5, 0, 15, (50, 56, 70))
    hline(buf, 11, 0, 15, (50, 56, 70))
    vline(buf, 7, 0, 5, (50, 56, 70))
    vline(buf, 3, 6, 11, (50, 56, 70))
    vline(buf, 11, 6, 11, (50, 56, 70))
    vline(buf, 7, 12, 15, (50, 56, 70))
    # top highlight
    hline(buf, 0, 0, 15, (120, 130, 152))
    if mossy:
        for _ in range(5):
            x, y = rng.randrange(0, 14), rng.randrange(0, 15)
            put(buf, x, y, SLIME)
            put(buf, x + 1, y, SLIME)
            put(buf, x, y + 1, SLIME, 180)


def concrete_floor(buf, rng, cracked=False, wet=False):
    base = CONC_A if not wet else (CONC_A[0] - 14, CONC_A[1] - 10, CONC_A[2] - 6)
    rect(buf, 0, 0, 15, 15, base)
    # control joints
    hline(buf, 7, 0, 15, CONC_C)
    vline(buf, 7, 0, 7, CONC_C)
    hline(buf, 15, 0, 15, CONC_C)
    vline(buf, 15, 0, 15, CONC_C)
    grain(buf, CONC_B, 0.18, rng)
    if cracked:
        for _ in range(2):
            x, y = rng.randrange(3, 13), rng.randrange(3, 13)
            vline(buf, x, y, min(y + 4, 15), (44, 50, 62))
            hline(buf, y, x - 1, x, (44, 50, 62))
    if wet:
        for _ in range(4):
            x, y = rng.randrange(0, 12), rng.randrange(0, 12)
            rect(buf, x, y, x + 3, y + 2, (CONC_A[0] + 16, CONC_A[1] + 18, CONC_A[2] + 26), 120)


def concrete_wall(buf, rng, dark=False):
    base = CONC_B if not dark else CONC_C
    rect(buf, 0, 0, 15, 15, base)
    grain(buf, CONC_C, 0.2, rng)
    hline(buf, 0, 0, 15, (128, 138, 158))
    hline(buf, 15, 0, 15, (50, 56, 68))
    for _ in range(3):
        x = rng.randrange(2, 13)
        vline(buf, x, 1, 3, (64, 70, 84))
    # stains
    for _ in range(3):
        x, y = rng.randrange(1, 14), rng.randrange(1, 14)
        put(buf, x, y, (60, 66, 78), 120)


def metal_bars(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    for bx in (2, 6, 10, 14):
        rect(buf, bx - 1, 0, bx + 1, 15, METAL)
        vline(buf, bx, 1, 14, (150, 160, 180))
        vline(buf, bx - 1, 1, 14, (90, 98, 116))
        put(buf, bx, 3, (70, 76, 92))
        put(buf, bx, 8, (70, 76, 92))
        put(buf, bx, 13, (70, 76, 92))


def pipe_h(buf, rng, rust=False):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 0, 4, 15, 9, METAL)
    hline(buf, 4, 0, 15, (150, 160, 180))
    hline(buf, 9, 0, 15, (90, 98, 116))
    vline(buf, 5, 2, 12, (110, 120, 138))
    vline(buf, 11, 2, 12, (110, 120, 138))
    if rust:
        for _ in range(3):
            put(buf, rng.randrange(2, 14), rng.randrange(5, 8), RUST, 200)


def pipe_v(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 4, 0, 9, 15, METAL)
    vline(buf, 4, 0, 15, (150, 160, 180))
    vline(buf, 9, 0, 15, (90, 98, 116))
    hline(buf, 5, 2, 12, (110, 120, 138))
    hline(buf, 11, 2, 12, (110, 120, 138))


def boiler_metal(buf, rng):
    rect(buf, 0, 0, 15, 15, (86, 92, 106))
    for ry in range(3):
        rect(buf, 0, ry * 5, 15, ry * 5 + 4, (100, 108, 124) if ry % 2 == 0 else (88, 96, 112))
        hline(buf, ry * 5 + 4, 0, 15, (64, 70, 84))
    # rivets
    for rx in (3, 8, 13):
        for ry in (2, 7, 12):
            put(buf, rx, ry, (150, 160, 180))
            put(buf, rx + 1, ry, (120, 130, 150))
    # rust patch
    for _ in range(4):
        put(buf, rng.randrange(1, 15), rng.randrange(1, 15), RUST, 180)


def rubble(buf, rng):
    rect(buf, 0, 0, 15, 15, (60, 66, 80))
    for _ in range(5):
        x, y = rng.randrange(0, 12), rng.randrange(0, 12)
        s = rng.randrange(2, 5)
        c = (STONE_B, STONE_C, CONC_C)[rng.randrange(3)]
        rect(buf, x, y, min(x + s, 15), min(y + s, 15), c)
        put(buf, x, y, (120, 130, 150))
    grain(buf, (44, 50, 62), 0.2, rng)


def pier_post(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 6, 0, 9, 15, WOOD_C)
    rect(buf, 5, 0, 10, 1, WOOD_B)
    vline(buf, 6, 1, 14, WOOD_D)
    vline(buf, 9, 1, 14, WOOD_B)
    put(buf, 7, 2, WOOD_D)
    put(buf, 8, 2, WOOD_D)
    put(buf, 7, 8, WOOD_D)
    put(buf, 8, 8, WOOD_D)


def crate(buf, rng):
    rect(buf, 0, 0, 15, 15, WOOD_C)
    rect(buf, 1, 1, 14, 14, WOOD_B)
    rect(buf, 2, 2, 13, 13, WOOD_A)
    hline(buf, 6, 1, 14, WOOD_C)
    hline(buf, 9, 1, 14, WOOD_C)
    vline(buf, 4, 1, 14, WOOD_D)
    vline(buf, 11, 1, 14, WOOD_D)
    put(buf, 7, 7, WOOD_D)


def barrel(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 2, 1, 13, 14, WOOD_B)
    rect(buf, 2, 1, 3, 14, WOOD_D)
    rect(buf, 12, 1, 13, 14, WOOD_D)
    hline(buf, 4, 2, 13, WOOD_C)
    hline(buf, 8, 2, 13, WOOD_C)
    hline(buf, 12, 2, 13, WOOD_C)
    rect(buf, 6, 6, 9, 9, (120, 110, 92))
    put(buf, 7, 7, WOOD_D)


def rope_coil(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    for cy in range(4):
        y = 2 + cy * 3
        hline(buf, y, 3, 12, (150, 130, 96))
        hline(buf, y + 1, 4, 11, (170, 150, 112))
        for x in range(4, 12, 2):
            put(buf, x, y + 2, (120, 100, 70))


def lantern_base(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 7, 1, 8, 14, (60, 66, 78))
    rect(buf, 6, 4, 9, 7, (90, 98, 116))
    rect(buf, 6, 8, 9, 9, (120, 100, 70))
    put(buf, 7, 5, ORANGE, 220)
    put(buf, 8, 5, LANTERN, 200)


def desk(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 0, 5, 15, 13, WOOD_A)
    hline(buf, 5, 0, 15, WOOD_C)
    hline(buf, 13, 0, 15, WOOD_D)
    vline(buf, 0, 5, 13, WOOD_D)
    vline(buf, 15, 5, 13, WOOD_D)
    rect(buf, 2, 6, 4, 9, (150, 150, 160))  # paper
    hline(buf, 7, 2, 4, (90, 90, 100))
    hline(buf, 9, 2, 4, (90, 90, 100))
    put(buf, 10, 7, ORANGE, 230)  # ink pot
    put(buf, 11, 7, ORANGE, 200)


def ledger(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 1, 3, 14, 12, (170, 166, 150))
    hline(buf, 3, 1, 14, (120, 116, 100))
    hline(buf, 12, 1, 14, (120, 116, 100))
    for _ in range(5):
        hline(buf, 5 + rng.randrange(0, 3), 3, 12, (110, 106, 90))
    rect(buf, 13, 4, 14, 6, (96, 92, 80))
    put(buf, 4, 5, ORANGE, 200)
    put(buf, 5, 5, ORANGE, 180)


def rug_corner(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 0, 0, 15, 15, (110, 62, 62))
    rect(buf, 2, 2, 13, 13, (130, 74, 74))
    rect(buf, 4, 4, 11, 11, (110, 62, 62))
    hline(buf, 3, 2, 13, (150, 96, 96))
    vline(buf, 2, 2, 13, (150, 96, 96))
    put(buf, 7, 7, (150, 96, 96))


def candle(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 7, 5, 8, 12, (230, 220, 190))
    put(buf, 7, 4, LANTERN, 220)
    put(buf, 8, 4, LANTERN, 200)
    put(buf, 7, 3, ORANGE, 140)
    put(buf, 6, 5, (180, 120, 70), 160)


def barred_window(buf, rng):
    rect(buf, 0, 0, 15, 15, STONE_C)
    rect(buf, 2, 1, 13, 14, (40, 50, 76))
    rect(buf, 3, 2, 12, 13, (60, 80, 110))
    vline(buf, 5, 2, 13, METAL)
    vline(buf, 8, 2, 13, METAL)
    vline(buf, 11, 2, 13, METAL)
    hline(buf, 7, 3, 12, METAL)
    put(buf, 4, 3, MOON, 120)
    put(buf, 7, 5, MOON, 100)


def torch_bracket(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    rect(buf, 1, 3, 2, 12, (70, 76, 90))
    rect(buf, 3, 5, 4, 10, (70, 76, 90))
    put(buf, 5, 5, ORANGE, 230)
    put(buf, 5, 6, LANTERN, 200)
    put(buf, 6, 4, ORANGE, 150)


def vent_grate(buf, rng):
    rect(buf, 0, 0, 15, 15, CONC_C)
    rect(buf, 2, 2, 13, 13, (58, 64, 78))
    hline(buf, 5, 2, 13, METAL)
    hline(buf, 8, 2, 13, METAL)
    hline(buf, 11, 2, 13, METAL)
    vline(buf, 2, 2, 13, (80, 86, 100))
    vline(buf, 13, 2, 13, (80, 86, 100))
    put(buf, 4, 3, (110, 120, 140))
    put(buf, 7, 6, (110, 120, 140))


def warning_stripes(buf, rng):
    rect(buf, 0, 0, 15, 15, INK)
    for x in range(0, 16, 4):
        rect(buf, x, 0, x + 1, 15, (200, 170, 60))
        rect(buf, x + 2, 0, x + 3, 15, (50, 46, 30))


def slime_stain(buf, rng):
    rect(buf, 0, 0, 15, 15, (0, 0, 0, 0))
    for _ in range(8):
        x, y = rng.randrange(1, 13), rng.randrange(1, 13)
        for dx in range(3):
            for dy in range(2):
                put(buf, x + dx, y + dy, SLIME, rng.randrange(150, 220))
    put(buf, 4, 6, (120, 160, 120), 200)
    put(buf, 10, 4, (120, 160, 120), 180)


def moon_glint(buf, rng):
    rect(buf, 0, 0, 15, 15, (0, 0, 0, 0))
    for _ in range(3):
        x, y = rng.randrange(2, 13), rng.randrange(2, 13)
        put(buf, x, y, MOON, 90)
        put(buf, x + 1, y, MOON, 60)


def build_atlas(cells, path):
    img = Image.new("RGBA", (ATLAS * CELL, ATLAS * CELL), (0, 0, 0, 0))
    for (cx, cy), painter in cells.items():
        rng = random.Random(hash((path, cx, cy)) & 0xFFFF)
        buf = new_tile()
        painter(buf, rng)
        for y in range(CELL):
            for x in range(CELL):
                img.putpixel((cx * CELL + x, cy * CELL + y), buf[y][x])
    img.save(path)
    print(f"wrote {path} ({img.size[0]}x{img.size[1]})")


def main():
    outdir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets", "tilemaps")
    os.makedirs(outdir, exist_ok=True)

    dock = {
        (0, 0): lambda b, r: water(b, 0, r),      # deep water
        (1, 0): lambda b, r: water(b, 1, r),      # mid water
        (2, 0): lambda b, r: water(b, 2, r),      # shallow water
        (3, 0): lambda b, r: water(b, 1, r, foam=True),  # foam edge
        (0, 1): lambda b, r: wood_plank(b, r),    # plank A
        (1, 1): lambda b, r: wood_plank(b, r),    # plank B (rng varies)
        (2, 1): lambda b, r: wood_plank(b, r, worn=True),  # worn plank
        (3, 1): rope_coil,
        (0, 2): sand_tile,
        (1, 2): pebble_tile,
        (2, 2): lambda b, r: sand_tile(b, r) or moon_glint(b, r),
        (3, 2): lantern_base,
        (0, 3): pier_post,
        (1, 3): crate,
        (2, 3): barrel,
        (3, 3): moon_glint,
    }
    gatehouse = {
        (0, 0): lambda b, r: stone_floor(b, r),
        (1, 0): lambda b, r: stone_floor(b, r),
        (2, 0): lambda b, r: stone_floor(b, r, cracked=True),
        (3, 0): lambda b, r: stone_floor(b, r),
        (0, 1): lambda b, r: stone_wall(b, r),
        (1, 1): lambda b, r: stone_wall(b, r),
        (2, 1): lambda b, r: stone_wall(b, r, dark=True),
        (3, 1): lambda b, r: stone_wall(b, r, mossy=True),
        (0, 2): desk,
        (1, 2): ledger,
        (2, 2): rug_corner,
        (3, 2): candle,
        (0, 3): barred_window,
        (1, 3): torch_bracket,
        (2, 3): lambda b, r: stone_floor(b, r),
        (3, 3): lambda b, r: stone_floor(b, r, cracked=True),
    }
    overflow = {
        (0, 0): lambda b, r: concrete_floor(b, r),
        (1, 0): lambda b, r: concrete_floor(b, r),
        (2, 0): lambda b, r: concrete_floor(b, r, cracked=True),
        (3, 0): lambda b, r: concrete_floor(b, r),
        (0, 1): lambda b, r: concrete_wall(b, r),
        (1, 1): lambda b, r: concrete_wall(b, r),
        (2, 1): lambda b, r: concrete_wall(b, r, dark=True),
        (3, 1): lambda b, r: pipe_h(b, r),
        (0, 2): pipe_v,
        (1, 2): metal_bars,
        (2, 2): boiler_metal,
        (3, 2): rubble,
        (0, 3): lambda b, r: concrete_floor(b, r, wet=True),
        (1, 3): slime_stain,
        (2, 3): vent_grate,
        (3, 3): warning_stripes,
    }

    build_atlas(dock, os.path.join(outdir, "dock_tileset.png"))
    build_atlas(gatehouse, os.path.join(outdir, "gatehouse_tileset.png"))
    build_atlas(overflow, os.path.join(outdir, "overflow_tileset.png"))
    print("done")


if __name__ == "__main__":
    main()
