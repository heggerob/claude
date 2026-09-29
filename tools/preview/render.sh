#!/usr/bin/env bash
# Builds and runs SpritePreview, writing a PNG contact sheet of all procedural pixel art.
# Needs mono (mcs + mono) and python3 with Pillow.
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=${1:-tools/preview/sprites.png}
TMP=$(mktemp -d)
mcs -nowarn:169,414,649,219 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/preview.exe \
  tools/unity-stub/UnityStub.cs tools/preview/SpritePreview.cs tools/preview/MapPreview.cs $(find AirsoftArena/Assets/Scripts -name '*.cs')
mono $TMP/preview.exe $TMP/preview.rgba
python3 - "$TMP" "$OUT" <<'PY'
import glob, os, struct, sys
from PIL import Image
tmp, out = sys.argv[1], sys.argv[2]
def convert(src, dst):
    data = open(src, 'rb').read()
    w, h = struct.unpack('<ii', data[:8])
    Image.frombytes('RGBA', (w, h), data[8:]).save(dst)
    print('wrote', dst)
convert(os.path.join(tmp, 'preview.rgba'), out)
for f in sorted(glob.glob(os.path.join(tmp, 'map_*.rgba'))):
    convert(f, os.path.join(os.path.dirname(out), os.path.basename(f)[:-5] + '.png'))
PY
