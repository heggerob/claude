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

# The storybook heroes, laid out like the concept sheet, plus an .obj of each.
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/heroes.exe \
  tools/unity-stub/UnityStub.cs tools/preview/HeroPreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/heroes.exe $TMP/heroes.rgba $TMP/heroes.txt OdinsCoin/docs/heroes
python3 - "$TMP/heroes.rgba" "$TMP/heroes.txt" OdinsCoin/docs/heroes.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw, ImageFont
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
img = Image.frombytes('RGBA', (w, h), data[8:]).resize((w // 2, h // 2), Image.LANCZOS)
labels = [l.strip() for l in open(sys.argv[2]) if l.strip()]
pad = 70
sheet = Image.new('RGBA', (img.width, img.height + pad), (246, 241, 230, 255))
sheet.paste(img, (0, 0))
d = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf', 22)
except Exception:
    font = ImageFont.load_default()
cw = img.width // len(labels)
ink = (74, 58, 46, 255)
for i, t in enumerate(labels):
    text = t.upper()
    tw = d.textlength(text, font=font)
    x = i * cw + (cw - tw) / 2
    y = img.height + 18
    d.line([(i * cw + 30, y - 8), (x - 12, y - 8)], fill=ink, width=1)
    d.line([(x + tw + 12, y - 8), ((i + 1) * cw - 30, y - 8)], fill=ink, width=1)
    d.text((x, y), text, font=font, fill=ink)
sheet.save(sys.argv[3])
print('wrote', sys.argv[3])
PY

# Side-by-side against the concept sheet: reference on the left, our render on the right.
python3 - <<'PY'
import os
from PIL import Image
ref = Image.open('OdinsCoin/docs/reference/characters-concept.png').convert('RGBA')
ours = Image.open('OdinsCoin/docs/heroes.png').convert('RGBA')
os.makedirs('OdinsCoin/docs/compare', exist_ok=True)
crops = {'jarl': ((0, 20, 340, 800), 0), 'raider': ((300, 20, 650, 800), 1)}
cell = ours.width // 7
for name, (box, index) in crops.items():
    a = ref.crop(box)
    b = ours.crop((index * cell, 0, (index + 1) * cell, ours.height - 70))
    b = b.resize((int(b.width * a.height / b.height), a.height), Image.LANCZOS)
    out = Image.new('RGBA', (a.width + b.width + 20, a.height), (246, 241, 230, 255))
    out.paste(a, (0, 0)); out.paste(b, (a.width + 20, 0))
    out.save('OdinsCoin/docs/compare/%s.png' % name)
print('wrote compare images')
PY
