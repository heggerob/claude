#!/usr/bin/env bash
# Renders a top-down picture of the Odin's Coin world (islands + start) to OdinsCoin/docs/world.png.
# Needs mono (mcs + mono) and python3 with Pillow.
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=${1:-OdinsCoin/docs/world.png}
mkdir -p "$(dirname "$OUT")"
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
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

# The hand-drawn surface textures, each tiled 2x2.
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/textures.exe \
  tools/unity-stub/UnityStub.cs tools/preview/TexturePreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/textures.exe $TMP/textures.rgba $TMP/textures.txt
python3 - "$TMP/textures.rgba" "$TMP/textures.txt" OdinsCoin/docs/textures.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
img = Image.frombytes('RGBA', (w, h), data[8:])
labels = [l.strip() for l in open(sys.argv[2]) if l.strip()]
sheet = Image.new('RGBA', (w, h + 24), (246, 241, 230, 255))
sheet.paste(img, (0, 0))
d = ImageDraw.Draw(sheet)
for i, t in enumerate(labels):
    d.text((i * (w // len(labels)) + 6, h + 6), t, fill=(60, 50, 40, 255))
sheet.save(sys.argv[3])
print('wrote', sys.argv[3])
PY

# The storybook heroes, laid out like the concept sheet, plus an .obj of each.
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/heroes.exe \
  tools/unity-stub/UnityStub.cs tools/preview/HeroPreview.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/heroes.exe $TMP/heroes.rgba $TMP/heroes.txt OdinsCoin/docs/heroes $TMP/motion.rgba $TMP/motion.txt $TMP/skins.rgba $TMP/skins.txt $TMP/turn.rgba $TMP/turn.txt $TMP/attacks.rgba $TMP/attacks.txt $TMP/footsteps.txt $TMP/ships.rgba $TMP/ships.txt $TMP/buildings.rgba $TMP/buildings.txt $TMP/scene.rgba $TMP/firstperson.rgba $TMP/firstperson.txt
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
    f = font
    tw = d.textlength(text, font=f)
    # Long labels shrink to fit their cell.
    size = 22
    while tw > cw - 40 and size > 10 and hasattr(font, 'path'):
        size -= 1
        f = ImageFont.truetype(font.path, size)
        tw = d.textlength(text, font=f)
    x = i * cw + (cw - tw) / 2
    y = img.height + 18
    d.line([(i * cw + 30, y - 8), (x - 12, y - 8)], fill=ink, width=1)
    d.line([(x + tw + 12, y - 8), ((i + 1) * cw - 30, y - 8)], fill=ink, width=1)
    d.text((x, y), text, font=f, fill=ink)
sheet.save(sys.argv[3])
print('wrote', sys.argv[3])
PY
python3 - "$TMP/skins.rgba" "$TMP/skins.txt" OdinsCoin/docs/skins.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw, ImageFont
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
img = Image.frombytes('RGBA', (w, h), data[8:]).resize((w // 2, h // 2), Image.LANCZOS)
lines = [l.rstrip('\n') for l in open(sys.argv[2])]
cols = int(lines[0].split('=')[1])
labels = lines[1:]
rows = (len(labels) + cols - 1) // cols
cw, ch = img.width // cols, img.height // rows
pad = 34
sheet = Image.new('RGBA', (img.width, img.height + rows * pad), (246, 241, 230, 255))
d = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf', 13)
except Exception:
    font = ImageFont.load_default()
for r in range(rows):
    sheet.paste(img.crop((0, r * ch, img.width, (r + 1) * ch)), (0, r * (ch + pad)))
    for c in range(cols):
        i = r * cols + c
        if i >= len(labels) or not labels[i]:
            continue
        t = labels[i]
        tw = d.textlength(t, font=font)
        d.text((c * cw + (cw - tw) / 2, r * (ch + pad) + ch + 6), t, font=font, fill=(74, 58, 46, 255))
sheet.save(sys.argv[3])
print('wrote', sys.argv[3])
PY
python3 - "$TMP/turn.rgba" "$TMP/turn.txt" OdinsCoin/docs/turnaround.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw, ImageFont
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
img = Image.frombytes('RGBA', (w, h), data[8:]).resize((w // 2, h // 2), Image.LANCZOS)
lines = [l.rstrip('\n') for l in open(sys.argv[2])]
cols = int(lines[0].split('=')[1])
labels = lines[1:]
rows = (len(labels) + cols - 1) // cols
cw, ch = img.width // cols, img.height // rows
pad = 34
sheet = Image.new('RGBA', (img.width, img.height + rows * pad), (246, 241, 230, 255))
d = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf', 13)
except Exception:
    font = ImageFont.load_default()
for r in range(rows):
    sheet.paste(img.crop((0, r * ch, img.width, (r + 1) * ch)), (0, r * (ch + pad)))
    for c in range(cols):
        i = r * cols + c
        if i >= len(labels) or not labels[i]:
            continue
        t = labels[i]
        tw = d.textlength(t, font=font)
        d.text((c * cw + (cw - tw) / 2, r * (ch + pad) + ch + 6), t, font=font, fill=(74, 58, 46, 255))
sheet.save(sys.argv[3])
print('wrote', sys.argv[3])
PY
# Footprints from above: how the footstep locomotion turns (docs/footsteps.png).
python3 - "$TMP/footsteps.txt" OdinsCoin/docs/footsteps.png <<'PY'
import math, sys
from PIL import Image, ImageDraw, ImageFont
groups = []
for line in open(sys.argv[1]):
    p = line.split()
    if not p: continue
    if p[0] == 'scenario': groups.append({'name': line[9:].strip(), 'body': [], 'feet': []})
    elif p[0] == 'body': groups[-1]['body'].append((float(p[1]), float(p[2])))
    elif p[0] == 'foot': groups[-1]['feet'].append((float(p[1]), float(p[2]), float(p[3]), p[4]))
cw, ch, pad = 520, 560, 40
img = Image.new('RGB', (cw * len(groups), ch + pad), (246, 241, 230))
d = ImageDraw.Draw(img)
try:
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf', 15)
except Exception:
    font = ImageFont.load_default()
for gi, g in enumerate(groups):
    pts = g['body'] + [(f[0], f[1]) for f in g['feet']]
    xs = [p[0] for p in pts]; zs = [p[1] for p in pts]
    span = max(max(xs) - min(xs), max(zs) - min(zs), 2.0)
    sc = (min(cw, ch) - 80) / span
    cx, cz = (max(xs) + min(xs)) / 2, (max(zs) + min(zs)) / 2
    def P(x, z): return (gi * cw + cw / 2 + (x - cx) * sc, ch / 2 - (z - cz) * sc)
    # A light grid, 1 m apart.
    for k in range(-10, 11):
        a = P(cx + k, cz - 10); b = P(cx + k, cz + 10); d.line([a, b], fill=(232, 226, 212))
        a = P(cx - 10, cz + k); b = P(cx + 10, cz + k); d.line([a, b], fill=(232, 226, 212))
    d.line([P(*p) for p in g['body']], fill=(160, 140, 120), width=2)
    for i, (x, z, h, side) in enumerate(g['feet']):
        a = math.radians(h)
        fwd = (math.sin(a), math.cos(a)); right = (math.cos(a), -math.sin(a))
        L, W = 0.13, 0.05
        corners = [(x + fwd[0] * sx * L + right[0] * sy * W, z + fwd[1] * sx * L + right[1] * sy * W) for sx, sy in ((1, 0), (0.4, 1), (-1, 0.8), (-1, -0.8), (0.4, -1))]
        col = (60, 45, 35) if side == 'L' else (150, 60, 40)
        d.polygon([P(*c) for c in corners], fill=col)
    t = g['name']
    d.text((gi * cw + (cw - d.textlength(t, font=font)) / 2, ch + 10), t, font=font, fill=(74, 58, 46))
img.save(sys.argv[2])
print('wrote', sys.argv[2])
PY
for sheet in attacks firstperson; do
python3 - "$TMP/$sheet.rgba" "$TMP/$sheet.txt" OdinsCoin/docs/$sheet.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw, ImageFont
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
img = Image.frombytes('RGBA', (w, h), data[8:]).resize((w // 2, h // 2), Image.LANCZOS)
lines = [l.rstrip('\n') for l in open(sys.argv[2])]
cols = int(lines[0].split('=')[1])
labels = lines[1:]
rows = (len(labels) + cols - 1) // cols
cw, ch = img.width // cols, img.height // rows
pad = 34
sheet = Image.new('RGBA', (img.width, img.height + rows * pad), (246, 241, 230, 255))
d = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf', 13)
except Exception:
    font = ImageFont.load_default()
for r in range(rows):
    sheet.paste(img.crop((0, r * ch, img.width, (r + 1) * ch)), (0, r * (ch + pad)))
    for c in range(cols):
        i = r * cols + c
        if i >= len(labels) or not labels[i]:
            continue
        t = labels[i]
        tw = d.textlength(t, font=font)
        d.text((c * cw + (cw - tw) / 2, r * (ch + pad) + ch + 6), t, font=font, fill=(74, 58, 46, 255))
sheet.save(sys.argv[3])
print('wrote', sys.argv[3])
PY
done
for strip in motion ships buildings; do
python3 - "$TMP/$strip.rgba" "$TMP/$strip.txt" OdinsCoin/docs/$strip.png <<'PY'
import struct, sys
from PIL import Image, ImageDraw, ImageFont
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
img = Image.frombytes('RGBA', (w, h), data[8:]).resize((w // 2, h // 2), Image.LANCZOS)
labels = [l.strip() for l in open(sys.argv[2]) if l.strip()]
sheet = Image.new('RGBA', (img.width, img.height + 60), (246, 241, 230, 255))
sheet.paste(img, (0, 0))
d = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf', 20)
except Exception:
    font = ImageFont.load_default()
cw = img.width // len(labels)
for i, t in enumerate(labels):
    tw = d.textlength(t, font=font)
    d.text((i * cw + (cw - tw) / 2, img.height + 18), t, font=font, fill=(74, 58, 46, 255))
sheet.save(sys.argv[3])
print('wrote', sys.argv[3])
PY
done

# A real place as it might look in the game.
if [ -f "$TMP/scene.rgba" ]; then
python3 - "$TMP/scene.rgba" OdinsCoin/docs/scene-kaupang.png <<'PY'
import struct, sys
from PIL import Image
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
Image.frombytes('RGBA', (w, h), data[8:]).save(sys.argv[2])
print('wrote', sys.argv[2])
PY
python3 - "$TMP/scene.rgba.fp" OdinsCoin/docs/scene-kaupang-fp.png <<'PY'
import struct, sys
from PIL import Image
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
Image.frombytes('RGBA', (w, h), data[8:]).save(sys.argv[2])
print('wrote', sys.argv[2])
PY
python3 - "$TMP/scene.rgba.helm" OdinsCoin/docs/scene-helm.png <<'PY'
import struct, sys
from PIL import Image
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
Image.frombytes('RGBA', (w, h), data[8:]).save(sys.argv[2])
print('wrote', sys.argv[2])
PY
fi

# Side-by-side against the concept sheet: reference on the left, our render on the right.
HERO_LABELS=$TMP/heroes.txt python3 - <<'PY'
import os
from PIL import Image
ref = Image.open('OdinsCoin/docs/reference/characters-concept.png').convert('RGBA')
ours = Image.open('OdinsCoin/docs/heroes.png').convert('RGBA')
os.makedirs('OdinsCoin/docs/compare', exist_ok=True)
crops = {'jarl': ((0, 20, 340, 800), 0), 'raider': ((300, 20, 650, 800), 1), 'navigator': ((580, 20, 890, 800), 2), 'spear-guard': ((860, 20, 1170, 800), 3), 'seer': ((1110, 20, 1480, 800), 4), 'scout': ((1420, 20, 1672, 800), 5)}
import sys, glob
labels = [l for l in open(os.environ['HERO_LABELS']) if l.strip()]
cell = ours.width // len(labels)
for name, (box, index) in crops.items():
    a = ref.crop(box)
    b = ours.crop((index * cell, 0, (index + 1) * cell, ours.height - 70))
    b = b.resize((int(b.width * a.height / b.height), a.height), Image.LANCZOS)
    out = Image.new('RGBA', (a.width + b.width + 20, a.height), (246, 241, 230, 255))
    out.paste(a, (0, 0)); out.paste(b, (a.width + 20, 0))
    out.save('OdinsCoin/docs/compare/%s.png' % name)
print('wrote compare images')
PY
