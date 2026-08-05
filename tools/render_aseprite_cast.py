#!/usr/bin/env python3
"""Render Eggbert's .aseprite character roster to PNG sprites.

The repo's .aseprite files carry a 4-byte size prefix before the standard Aseprite
header, and (writer quirk) a 4-zero-byte gap between the color-profile chunk and the
palette chunk. This parser handles both, plus the modern 16-byte frame header and
WORD cel-type field.

Usage:
    python3 tools/render_aseprite_cast.py [outdir]
        renders every assets/characters/*.aseprite to <outdir>/<snake_case>.png
        (default outdir: assets/generated/characters)

Then run `godot --headless --path . --import` once so Godot writes .import sidecars
(the repo commits .import files for tracked assets).
"""
import os
import struct
import sys
import zlib

from PIL import Image


def render_aseprite(path: str) -> Image.Image:
    data = open(path, "rb").read()
    magic_pos = data.find(b"\xfa\xf1", 44)
    assert magic_pos >= 0, "no frame magic"
    off = magic_pos - 4  # frame size field precedes the frame magic

    def u16():
        nonlocal off
        v = struct.unpack_from("<H", data, off)[0]
        off += 2
        return v

    def u32():
        nonlocal off
        v = struct.unpack_from("<I", data, off)[0]
        off += 4
        return v

    def i16():
        nonlocal off
        v = struct.unpack_from("<h", data, off)[0]
        off += 2
        return v

    def u8():
        nonlocal off
        v = data[off]
        off += 1
        return v

    nframes = struct.unpack_from("<H", data, 6)[0]
    w, h, depth = struct.unpack_from("<HHH", data, 8)
    frames = []
    for _ in range(nframes):
        fsize = u32()
        fend = off + fsize - 4
        assert u16() == 0xF1FA, "bad frame magic"
        oldchunks = u16()
        u16()  # duration
        u16()  # future
        newchunks = u32()
        nchunks = newchunks if newchunks else oldchunks
        cels = []
        for _ in range(nchunks):
            csize = u32()
            ctype = u16()
            if csize == 0:
                off -= 2  # 4-zero-byte writer quirk between chunks
                continue
            cend = off + csize - 6
            if ctype == 0x2005:  # CEL
                lidx = u16()
                cx = i16()
                cy = i16()
                u8()  # opacity
                celtype = u16()  # WORD per spec
                off += 7  # zindex(2) + future(5)
                if celtype == 0:
                    cw, ch = u16(), u16()
                    px = data[off : off + cw * ch * (depth // 8)]
                    off += cw * ch * (depth // 8)
                    cels.append((lidx, cx, cy, cw, ch, px))
                elif celtype == 2:
                    cw, ch = u16(), u16()
                    raw = zlib.decompress(data[off:cend])
                    off = cend
                    cels.append((lidx, cx, cy, cw, ch, raw))
                else:
                    off = cend
            else:
                off = cend
        frames.append(cels)
        off = fend

    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for (lidx, cx, cy, cw, ch, px) in frames[0]:
        if lidx >= 0:
            sub = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
            if depth == 32:
                sub.frombytes(px, "raw", "RGBA")
            img.alpha_composite(sub, (cx, cy))
    return img


def main() -> None:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    src = os.path.join(root, "assets", "characters")
    outdir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, "assets", "generated", "characters")
    os.makedirs(outdir, exist_ok=True)

    ok = 0
    for name in sorted(os.listdir(src)):
        if not (name.endswith(".aseprite") or name.endswith(".ase")):
            continue
        stem = name.rsplit(".", 1)[0]
        try:
            img = render_aseprite(os.path.join(src, name))
            out = os.path.join(outdir, stem.lower().replace(" ", "_") + ".png")
            img.save(out)
            print(f"OK {name} -> {out} {img.size}")
            ok += 1
        except Exception as e:
            print(f"ERR {name}: {type(e).__name__}: {e}")
    print(f"rendered {ok} files")


if __name__ == "__main__":
    main()
