#!/usr/bin/env bash
# ============================================================
# build-android.sh
# One-command Android build. Requires Unity Editor + Android module installed.
# Usage:
#   ./build-android.sh
#   ./build-android.sh --output Build/My.apk --no-il2cpp
# ============================================================

set -euo pipefail

# ---- Config (override via env)
UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
OUTPUT_PATH="${ARIA_OUTPUT_PATH:-Builds/Android/ProjectAria.apk}"
KEYSTORE_NAME="${ARIA_KEYSTORE_NAME:-user.keystore}"

# ---- Args
while [[ $# -gt 0 ]]; do
    case $1 in
        --unity) UNITY_PATH="$2"; shift 2 ;;
        --output) OUTPUT_PATH="$2"; shift 2 ;;
        --project) PROJECT_PATH="$2"; shift 2 ;;
        --no-il2cpp) ARIA_NO_IL2CPP=1; shift ;;
        -h|--help)
            echo "Usage: $0 [--unity PATH] [--output PATH] [--project PATH] [--no-il2cpp]"
            exit 0
            ;;
        *) echo "Unknown arg: $1"; exit 1 ;;
    esac
done

# ---- Sanity checks
if [[ ! -x "$UNITY_PATH" && ! -f "$UNITY_PATH" ]]; then
    echo "❌ Unity not found at: $UNITY_PATH"
    echo "Set UNITY_PATH env var, e.g.:"
    echo "  export UNITY_PATH=/path/to/Unity"
    exit 1
fi

if [[ ! -d "$PROJECT_PATH/Assets" ]]; then
    echo "❌ Project not found at: $PROJECT_PATH"
    exit 1
fi

# ---- Pre-build: ensure keystore exists
if [[ ! -f "$PROJECT_PATH/$KEYSTORE_NAME" ]]; then
    echo "🔑 No keystore found. Generating dev keystore..."
    "$UNITY_PATH" -batchmode -nographics -quit \
        -projectPath "$PROJECT_PATH" \
        -executeMethod ProjectAria.Editor.BuildScript.GenerateKeystore
fi

# ---- Build
echo "🏗  Building Android APK..."
echo "  Unity:    $UNITY_PATH"
echo "  Project:  $PROJECT_PATH"
echo "  Output:   $OUTPUT_PATH"

mkdir -p "$(dirname "$OUTPUT_PATH")"

ARIA_OUTPUT_PATH="$OUTPUT_PATH" "$UNITY_PATH" \
    -batchmode \
    -nographics \
    -quit \
    -projectPath "$PROJECT_PATH" \
    -buildTarget Android \
    -executeMethod ProjectAria.Editor.BuildScript.BuildAndroid \
    -logFile -

BUILD_EXIT=$?

if [[ $BUILD_EXIT -eq 0 && -f "$OUTPUT_PATH" ]]; then
    SIZE=$(du -h "$OUTPUT_PATH" | cut -f1)
    echo "✅ Build succeeded! APK: $OUTPUT_PATH ($SIZE)"
else
    echo "❌ Build failed (exit $BUILD_EXIT). Check logs above."
    exit $BUILD_EXIT
fi
