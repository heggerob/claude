#!/usr/bin/env bash
# Runs the plain-C# logic tests for Odin's Coin outside Unity. Needs mono (mcs + mono).
set -euo pipefail
cd "$(dirname "$0")/../.."
TMP=$(mktemp -d)
mcs -nowarn:169,414,649,219,618 -define:ENABLE_LEGACY_INPUT_MANAGER -out:$TMP/tests.exe \
  tools/unity-stub/UnityStub.cs tools/tests/LogicTests.cs $(find OdinsCoin/Assets/Scripts -name '*.cs')
mono $TMP/tests.exe
