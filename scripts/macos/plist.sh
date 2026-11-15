#!/bin/sh
set -e
cd "$(dirname "$0")/../.."

VERSION=$1
OUTPUT=$2
SHORT=${VERSION%%-*}

cp scripts/macos/Info.plist "$OUTPUT"
/usr/libexec/PlistBuddy \
  -c "Set :CFBundleShortVersionString $SHORT" \
  -c "Set :CFBundleVersion $SHORT" \
  "$OUTPUT" >/dev/null
