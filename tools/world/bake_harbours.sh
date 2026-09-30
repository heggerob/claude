#!/usr/bin/env bash
# Bakes the real places' harbours (water joined to the open sea) into OdinsCoin/Assets/Resources/World/harbours.txt.
set -euo pipefail
cd "$(dirname "$0")/../.."
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/bake.exe \
  tools/unity-stub/UnityStub.cs tools/world/BakeHarbours.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/bake.exe OdinsCoin/Assets/Resources/World/north.bytes OdinsCoin/Assets/Resources/World/harbours.txt
