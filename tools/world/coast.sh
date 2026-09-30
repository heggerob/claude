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
