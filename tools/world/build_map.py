#!/usr/bin/env python3
"""
Builds Odin's Coin's real-world height map from AWS Terrain Tiles (Mapzen, Terrarium encoding: land and sea
floor, in metres, built from SRTM, GMTED, ETOPO1 and others).

The game's world is flat, 1 unit = 1 metre. Latitude/longitude are projected with a sinusoidal projection
centred on LON0/LAT0 (x east, z north), which keeps distances along each parallel and along the meridians true,
so sailing distances match the real ones. The same maths is in OdinsCoin/Assets/Scripts/World/WorldMap.cs;
change both together.

Output: OdinsCoin/Assets/Resources/World/north.bytes, a small header then the heights (int16 metres, rows
south to north) gzip-compressed, and OdinsCoin/docs/worldmap.png, a picture of it.

Usage: tools/world/build_map.py [--cell 1000] [--zoom 7]. Tiles are cached in $TMPDIR/oc-tiles.
"""
import argparse, gzip, io, math, os, struct, sys, urllib.request
import numpy as np
from PIL import Image

LON0, LAT0 = 10.0, 60.0            # projection centre (the Norwegian coast)
LON_MIN, LON_MAX = -25.0, 33.0     # Iceland to Lake Ladoga
LAT_MIN, LAT_MAX = 50.0, 71.5      # the English Channel to the North Cape
R = 6371000.0
TILE_URL = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "OdinsCoin", "Assets", "Resources", "World", "north.bytes")
PICTURE = os.path.join(ROOT, "OdinsCoin", "docs", "worldmap.png")
MAGIC, VERSION = b"OCWM", 1


def project(lon, lat):
    """Longitude/latitude (degrees) to game metres (x east, z north)."""
    x = R * np.radians(lon - LON0) * np.cos(np.radians(lat))
    z = R * np.radians(lat - LAT0)
    return x, z


def unproject(x, z):
    lat = LAT0 + np.degrees(z / R)
    c = np.maximum(np.cos(np.radians(lat)), 1e-6)
    lon = LON0 + np.degrees(x / (R * c))
    return lon, lat


def tile_xy(lon, lat, zoom):
    """Fractional Web Mercator tile coordinates."""
    n = 2 ** zoom
    x = (lon + 180.0) / 360.0 * n
    lr = np.radians(lat)
    y = (1.0 - np.log(np.tan(lr) + 1.0 / np.cos(lr)) / math.pi) / 2.0 * n
    return x, y


def fetch(zoom, x, y, cache):
    path = os.path.join(cache, f"{zoom}-{x}-{y}.png")
    if not os.path.exists(path):
        url = TILE_URL.format(z=zoom, x=x, y=y)
        for attempt in range(4):
            try:
                with urllib.request.urlopen(url, timeout=60) as r:
                    data = r.read()
                break
            except Exception as e:  # network hiccups: retry
                if attempt == 3:
                    raise
        with open(path, "wb") as f:
            f.write(data)
    rgb = np.asarray(Image.open(path).convert("RGB"), dtype=np.float64)
    return rgb[..., 0] * 256.0 + rgb[..., 1] + rgb[..., 2] / 256.0 - 32768.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--cell", type=float, default=1000.0, help="grid spacing in metres")
    ap.add_argument("--zoom", type=int, default=7)
    args = ap.parse_args()
    cache = os.path.join(os.environ.get("TMPDIR", "/tmp"), "oc-tiles")
    os.makedirs(cache, exist_ok=True)

    # The grid: the projected bounding box of the lon/lat region.
    lons = np.array([LON_MIN, LON_MAX, LON_MIN, LON_MAX, LON0, LON0])
    lats = np.array([LAT_MIN, LAT_MIN, LAT_MAX, LAT_MAX, LAT_MIN, LAT_MAX])
    xs, zs = project(lons, lats)
    x0, x1, z0, z1 = xs.min(), xs.max(), zs.min(), zs.max()
    w = int(math.ceil((x1 - x0) / args.cell)) + 1
    h = int(math.ceil((z1 - z0) / args.cell)) + 1
    print(f"grid {w} x {h} cells of {args.cell:.0f} m ({w * args.cell / 1000:.0f} x {h * args.cell / 1000:.0f} km)")

    # The tiles covering it.
    tx0, ty1 = tile_xy(LON_MIN, LAT_MIN, args.zoom)
    tx1, ty0 = tile_xy(LON_MAX, LAT_MAX, args.zoom)
    tx0, tx1, ty0, ty1 = int(tx0), int(tx1), int(ty0), int(ty1)
    nx, ny = tx1 - tx0 + 1, ty1 - ty0 + 1
    print(f"fetching {nx * ny} tiles at zoom {args.zoom}")
    mosaic = np.zeros((ny * 256, nx * 256), dtype=np.float32)
    for j in range(ny):
        for i in range(nx):
            mosaic[j * 256:(j + 1) * 256, i * 256:(i + 1) * 256] = fetch(args.zoom, tx0 + i, ty0 + j, cache)
        print(f"  row {j + 1}/{ny}", flush=True)

    # Sample the mosaic at every grid cell (bilinear), row 0 = south.
    gx = x0 + np.arange(w) * args.cell
    gz = z0 + np.arange(h) * args.cell
    X, Z = np.meshgrid(gx, gz)
    lon, lat = unproject(X, Z)
    inside = (lon >= LON_MIN) & (lon <= LON_MAX) & (lat >= LAT_MIN) & (lat <= LAT_MAX)
    fx, fy = tile_xy(np.clip(lon, LON_MIN, LON_MAX), np.clip(lat, LAT_MIN, LAT_MAX), args.zoom)
    px = np.clip((fx - tx0) * 256.0 - 0.5, 0, nx * 256 - 1.001)
    py = np.clip((fy - ty0) * 256.0 - 0.5, 0, ny * 256 - 1.001)
    ix, iy = px.astype(np.int64), py.astype(np.int64)
    ax, ay = px - ix, py - iy
    v = (mosaic[iy, ix] * (1 - ax) * (1 - ay) + mosaic[iy, ix + 1] * ax * (1 - ay)
         + mosaic[iy + 1, ix] * (1 - ax) * ay + mosaic[iy + 1, ix + 1] * ax * ay)
    # Outside the region: open ocean.
    v = np.where(inside, v, -2000.0)
    # Deep water to the nearest 5 m (it only matters to the sea's colour), which halves the file.
    v = np.where(v < -30.0, np.round(v / 5.0) * 5.0, v)
    heights = np.clip(np.round(v), -32000, 32000).astype("<i2")

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    header = MAGIC + struct.pack("<iiifffffff", VERSION, w, h, args.cell, x0, z0, LON0, LAT0, R, 0.0)
    with open(OUT, "wb") as f:
        f.write(header)
        f.write(gzip.compress(heights.tobytes(), 9))
    print(f"wrote {OUT} ({os.path.getsize(OUT) / 1e6:.1f} MB)")

    # A picture: sea in blues by depth, land in greens to browns to white by height (north up).
    hgt = heights.astype(np.float32)[::-1]
    img = np.zeros(hgt.shape + (3,), dtype=np.float32)
    sea = hgt <= 0
    d = np.clip(-hgt / 3000.0, 0, 1)[..., None]
    img[sea] = ((1 - d) * np.array([150, 190, 215]) + d * np.array([30, 60, 110]))[sea]
    e = np.clip(hgt / 2000.0, 0, 1)[..., None]
    land = (1 - e) * np.array([110, 150, 90]) + e * np.array([150, 120, 90])
    land = np.where(e > 0.6, (e - 0.6) / 0.4 * np.array([245, 245, 240]) + (1 - (e - 0.6) / 0.4) * land, land)
    img[~sea] = land[~sea]
    pic = Image.fromarray(img.astype(np.uint8))
    pic.thumbnail((2000, 2000))
    pic.save(PICTURE)
    print(f"wrote {PICTURE}")


if __name__ == "__main__":
    main()
