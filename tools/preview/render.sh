#!/usr/bin/env bash
# Renders a top-down picture of the Odin's Coin world (islands + start) to OdinsCoin/docs/world.png.
# Needs mono (mcs + mono) and python3 with Pillow.
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=${1:-OdinsCoin/docs/world.png}
mkdir -p "$(dirname "$OUT")"
TMP=$(mktemp -d)
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/world.exe \
  tools/unity-stub/UnityStub.cs tools/preview/WorldPreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/world.exe $TMP/world.rgba
python3 - "$TMP/world.rgba" "$OUT" <<'PY'
import struct, sys
from PIL import Image
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
Image.frombytes('RGBA', (w, h), data[8:]).save(sys.argv[2])
print('wrote', sys.argv[2])
PY

# Sounds: every generated effect as a WAV.
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/sounds.exe \
  tools/unity-stub/UnityStub.cs tools/preview/SoundPreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/sounds.exe OdinsCoin/docs/sounds

# The Viking: eight views (looks, angles, a fighting pose) and an .obj of the default Viking.
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/viking.exe \
  tools/unity-stub/UnityStub.cs tools/preview/VikingPreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/viking.exe $TMP/viking.rgba OdinsCoin/docs/viking.obj
python3 - "$TMP/viking.rgba" OdinsCoin/docs/viking.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
img = Image.frombytes('RGBA', (w, h), data[8:]).resize((w // 2, h // 2), Image.LANCZOS)
d = ImageDraw.Draw(img)
labels = ["Front", "Three-quarter", "Side", "Back", "Fighting", "Jarl (horns)", "Saxon guard", "Danish raider"]
cw, ch = w // 2 // 4, h // 2 // 2
for i, t in enumerate(labels):
    d.text((i % 4 * cw + 10, i // 4 * ch + 8), t, fill=(30, 35, 45, 255))
img.save(sys.argv[2])
print('wrote', sys.argv[2])
PY
