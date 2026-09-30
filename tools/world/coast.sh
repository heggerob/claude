#!/usr/bin/env bash
# Renders close-up pictures of the streamed land (real map + detail) to OdinsCoin/docs/coast-*.png.
set -euo pipefail
cd "$(dirname "$0")/../.."
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/coast.exe \
  tools/unity-stub/UnityStub.cs tools/world/CoastPreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
MAP=OdinsCoin/Assets/Resources/World/north.bytes
shot() {
  mono $TMP/coast.exe $MAP "$2" "$3" "$4" 900 $TMP/$1.rgb
  python3 - "$TMP/$1.rgb" "OdinsCoin/docs/coast-$1.png" <<'PY'
import struct, sys
from PIL import Image
d = open(sys.argv[1], "rb").read()
w, h = struct.unpack("<ii", d[:8])
Image.frombytes("RGB", (w, h), d[8:]).save(sys.argv[2])
PY
  echo "wrote OdinsCoin/docs/coast-$1.png"
}
shot bergen 60.39 5.2 40
shot lofoten 68.2 14.2 60
shot oslofjord 59.3 10.5 50

# The in-game sea chart, with the real places.
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/chart.exe \
  tools/unity-stub/UnityStub.cs tools/world/ChartPreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/chart.exe $MAP 1400 $TMP/chart.rgb $TMP/chart.txt
python3 - "$TMP/chart.rgb" "$TMP/chart.txt" OdinsCoin/docs/chart.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw
d = open(sys.argv[1], "rb").read()
w, h = struct.unpack("<ii", d[:8])
img = Image.frombytes("RGB", (w, h), d[8:])
draw = ImageDraw.Draw(img)
ink = (58, 40, 26)
for line in open(sys.argv[2]):
    name, kind, x, y = line.strip().split("|")
    x, y = float(x), float(y)
    r = 4
    if kind == "market": draw.polygon([(x, y - r), (x + r, y), (x, y + r), (x - r, y)], fill=ink)
    elif kind == "monastery": draw.line([(x - r, y), (x + r, y)], fill=ink, width=2); draw.line([(x, y - r), (x, y + r)], fill=ink, width=2)
    else: draw.ellipse([x - r, y - r, x + r, y + r], fill=ink)
    draw.text((x + 6, y - 7), name, fill=ink)
img.save(sys.argv[3])
PY
echo "wrote OdinsCoin/docs/chart.png"

# The real places up close: the land, with the settlement's buildings, jetty, harbour, chests and guards marked.
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/place.exe \
  tools/unity-stub/UnityStub.cs tools/world/PlacePreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
place() {
  mono $TMP/place.exe $MAP "$1" "$3" 700 $TMP/place.txt
  read LAT LON < <(head -1 $TMP/place.txt)
  mono $TMP/coast.exe $MAP "$LAT" "$LON" "$3" 700 $TMP/place.rgb
  python3 - "$TMP/place.rgb" "$TMP/place.txt" "OdinsCoin/docs/place-$2.png" "$1" <<'PY'
import struct, sys, math
from PIL import Image, ImageDraw
d = open(sys.argv[1], "rb").read()
w, h = struct.unpack("<ii", d[:8])
img = Image.frombytes("RGB", (w, h), d[8:])
dr = ImageDraw.Draw(img)
ink = (40, 28, 18)
colours = {"GreatHall": (150, 40, 30), "Longhouse": (120, 80, 45), "Boathouse": (90, 70, 50), "Storehouse": (160, 120, 60),
           "Watchtower": (60, 60, 60), "Palisade": (70, 50, 30), "Jetty": (185, 150, 100), "Church": (210, 205, 190)}
for line in list(open(sys.argv[2]))[1:]:
    p = line.split()
    if p[0] == "plot":
        kind, x, y, hx, hz, yaw = p[1], *map(float, p[2:])
        a = math.radians(yaw)
        # Footprint corners: local x across, z along the yaw (clockwise from north, screen y down).
        pts = []
        for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            lx, lz = sx * hx, sz * hz
            ex = lx * math.cos(a) + lz * math.sin(a)
            ez = -lx * math.sin(a) + lz * math.cos(a)
            pts.append((x + ex, y - ez))
        dr.polygon(pts, fill=colours.get(kind, (100, 100, 100)), outline=ink)
    elif p[0] == "harbour":
        x, y = map(float, p[1:3]); dr.ellipse([x - 5, y - 5, x + 5, y + 5], outline=(20, 40, 120), width=2)
    elif p[0] == "chest":
        x, y = map(float, p[1:3]); dr.rectangle([x - 2, y - 2, x + 2, y + 2], fill=(230, 180, 40), outline=ink)
    elif p[0] == "guard":
        x, y = map(float, p[1:3]); dr.ellipse([x - 2.5, y - 2.5, x + 2.5, y + 2.5], fill=(40, 90, 50), outline=ink)
    elif p[0] == "home":
        x, y, r = map(float, p[1:4]); dr.ellipse([x - r, y - r, x + r, y + r], outline=(150, 30, 20), width=2)
dr.text((10, 10), sys.argv[4], fill=ink)
img.save(sys.argv[3])
PY
  echo "wrote OdinsCoin/docs/place-$2.png"
}
place Kaupang kaupang 3
place Lindisfarne lindisfarne 1.5
place Hedeby hedeby 2
