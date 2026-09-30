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
