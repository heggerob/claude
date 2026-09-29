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
