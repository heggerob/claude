#!/usr/bin/env bash
# Compile-check the Unity scripts outside Unity against a tiny fake UnityEngine API.
# Catches C# syntax/type errors; it does NOT prove the game runs. Needs `mcs` (apt-get install mono-mcs).
set -euo pipefail
cd "$(dirname "$0")/.."
STUB=tools/unity-stub/UnityStub.cs
OUT=$(mktemp -d)
FILES=$(find AirsoftArena/Assets/Scripts -name '*.cs')
NOWARN=-nowarn:169,414,649,219
echo "== Unity 2022 + legacy input"
mcs -target:library $NOWARN -define:ENABLE_LEGACY_INPUT_MANAGER -out:$OUT/a.dll $STUB $FILES
echo "== Unity 6 + Input System"
mcs -target:library $NOWARN -define:ENABLE_INPUT_SYSTEM -define:UNITY_6000_0_OR_NEWER -out:$OUT/b.dll $STUB $FILES
echo "OK"
