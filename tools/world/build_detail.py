#!/usr/bin/env python3
"""
Builds the fine coast layer for Odin's Coin's world: wherever the 1 km map (north.bytes, from build_map.py) has
both land and sea in a 25 km block, that block is sampled again at 200 m from zoom-11 AWS Terrain Tiles
(~38 m pixels at 60N, fine enough that fjord water still reads as exactly 0 m and can be told from land;
lower zooms average it away), so fjords, sounds and skerry belts come out. Blocks line up with the 1 km grid.
About 18 000 tiles (1.8 GB) the first time.

Output: OdinsCoin/Assets/Resources/World/coast.bytes:
  "OCWD", version 1, block size (m), cell (m), points per side, origin x, origin z (as the 1 km map), block count,
  then an index of (block i, block j, offset, length) and each block's int16 heights (rows south to north),
  gzip-compressed on their own so the game unpacks only the blocks near the ship.

Usage: tools/world/build_detail.py [--zoom 9]. Needs build_map.py's output first. Tiles are cached in $TMPDIR/oc-tiles.
"""
import argparse, gzip, math, os, struct, sys
from concurrent.futures import ThreadPoolExecutor
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
import build_map as bm

BLOCK_CELLS = 25          # blocks are 25 cells of the 1 km map
CELL = 200.0              # fine grid spacing, metres
OUT = os.path.join(bm.ROOT, "OdinsCoin", "Assets", "Resources", "World", "coast.bytes")


def read_map():
    with open(bm.OUT, "rb") as f:
        data = f.read()
    assert data[:4] == bm.MAGIC
    version, w, h, cell, x0, z0 = struct.unpack("<iiifff", data[4:28])
    heights = np.frombuffer(gzip.decompress(data[44:]), dtype="<i2").reshape(h, w)
    return heights, cell, x0, z0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--zoom", type=int, default=11)
    args = ap.parse_args()
    cache = os.path.join(os.environ.get("TMPDIR", "/tmp"), "oc-tiles")
    os.makedirs(cache, exist_ok=True)
    coarse, cell, x0, z0 = read_map()
    h, w = coarse.shape
    block_m = BLOCK_CELLS * cell
    n = int(round(block_m / CELL)) + 1   # points per side, sharing edges with the neighbours

    blocks = []
    for bj in range(0, (h + BLOCK_CELLS - 1) // BLOCK_CELLS):
        for bi in range(0, (w + BLOCK_CELLS - 1) // BLOCK_CELLS):
            b = coarse[bj * BLOCK_CELLS:(bj + 1) * BLOCK_CELLS + 1, bi * BLOCK_CELLS:(bi + 1) * BLOCK_CELLS + 1]
            if (b > 0).any() and (b <= 0).any():
                blocks.append((bi, bj))
    print(f"{len(blocks)} coastal blocks of {block_m / 1000:.0f} km, {n} x {n} points at {CELL:.0f} m")

    # Where every block's points are, in zoom-level pixel space, and which tiles that needs.
    t = np.arange(n) * CELL
    plans, need = [], set()
    for bi, bj in blocks:
        X, Z = np.meshgrid(x0 + bi * block_m + t, z0 + bj * block_m + t)
        lon, lat = bm.unproject(X, Z)
        fx, fy = bm.tile_xy(np.clip(lon, bm.LON_MIN, bm.LON_MAX), np.clip(lat, bm.LAT_MIN, bm.LAT_MAX), args.zoom)
        px, py = fx * 256.0 - 0.5, fy * 256.0 - 0.5
        tx0, tx1 = int(px.min() // 256), int(px.max() // 256 + 1)
        ty0, ty1 = int(py.min() // 256), int(py.max() // 256 + 1)
        plans.append((bi, bj, px, py, tx0, tx1, ty0, ty1))
        for ty in range(ty0, ty1 + 1):
            for tx in range(tx0, tx1 + 1):
                need.add((tx, ty))
    print(f"fetching {len(need)} tiles at zoom {args.zoom}")
    with ThreadPoolExecutor(16) as pool:
        done = 0
        for _ in pool.map(lambda k: bm.fetch(args.zoom, k[0], k[1], cache), sorted(need)):
            done += 1
            if done % 1000 == 0:
                print(f"  {done}/{len(need)}", flush=True)

    index, blobs, offset = [], [], 0
    for k, (bi, bj, px, py, tx0, tx1, ty0, ty1) in enumerate(plans):
        mosaic = np.zeros(((ty1 - ty0 + 1) * 256, (tx1 - tx0 + 1) * 256), dtype=np.float32)
        for ty in range(ty0, ty1 + 1):
            for tx in range(tx0, tx1 + 1):
                mosaic[(ty - ty0) * 256:(ty - ty0 + 1) * 256, (tx - tx0) * 256:(tx - tx0 + 1) * 256] = bm.fetch(args.zoom, tx, ty, cache)
        mosaic = bm.water(mosaic, 40075000.0 / (256 * 2 ** args.zoom) * math.cos(math.radians(bm.LAT0)))
        lx = np.clip(px - tx0 * 256, 0, mosaic.shape[1] - 1.001)
        ly = np.clip(py - ty0 * 256, 0, mosaic.shape[0] - 1.001)
        ix, iy = lx.astype(np.int64), ly.astype(np.int64)
        ax, ay = lx - ix, ly - iy
        v = (mosaic[iy, ix] * (1 - ax) * (1 - ay) + mosaic[iy, ix + 1] * ax * (1 - ay)
             + mosaic[iy + 1, ix] * (1 - ax) * ay + mosaic[iy + 1, ix + 1] * ax * ay)
        v = np.where(v < -30.0, np.round(v / 5.0) * 5.0, v)
        blob = gzip.compress(np.clip(np.round(v), -32000, 32000).astype("<i2").tobytes(), 9)
        index.append((bi, bj, offset, len(blob)))
        blobs.append(blob)
        offset += len(blob)
        if (k + 1) % 200 == 0:
            print(f"  block {k + 1}/{len(plans)}", flush=True)

    with open(OUT, "wb") as f:
        f.write(b"OCWD" + struct.pack("<iffiffi", 1, block_m, CELL, n, x0, z0, len(index)))
        for bi, bj, off, ln in index:
            f.write(struct.pack("<iiii", bi, bj, off, ln))
        for blob in blobs:
            f.write(blob)
    print(f"wrote {OUT} ({os.path.getsize(OUT) / 1e6:.1f} MB)")


if __name__ == "__main__":
    main()
