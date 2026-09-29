#!/usr/bin/env bash
# Builds and runs SpritePreview, writing a PNG contact sheet of all procedural pixel art.
# Needs mono (mcs + mono) and python3 with Pillow.
set -euo pipefail
cd "$(dirname "$0")/../.."
OUT=${1:-tools/preview/sprites.png}
TMP=$(mktemp -d)
mcs -nowarn:169,414,649,219 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/preview.exe \
  tools/unity-stub/UnityStub.cs tools/preview/SpritePreview.cs $(find AirsoftArena/Assets/Scripts -name '*.cs')
mono $TMP/preview.exe $TMP/preview.rgba
python3 - "$TMP/preview.rgba" "$OUT" <<'PY'
import struct, sys
from PIL import Image
data = open(sys.argv[1], 'rb').read()
w, h = struct.unpack('<ii', data[:8])
Image.frombytes('RGBA', (w, h), data[8:]).save(sys.argv[2])
print('wrote', sys.argv[2])
PY
