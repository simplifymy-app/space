#!/bin/sh
set -e

DIRECTORY=$1
RUNTIME=$2

case "$RUNTIME" in
  osx-arm64) ARCH=arm64 ;;
  osx-x64) ARCH=x86_64 ;;
  *) echo "thin.sh: unknown runtime $RUNTIME" >&2; exit 1 ;;
esac

for LIBRARY in "$DIRECTORY"/*.dylib; do
  [ -f "$LIBRARY" ] || continue
  case "$(lipo -archs "$LIBRARY" 2>/dev/null)" in
    *\ *)
      lipo -thin "$ARCH" "$LIBRARY" -output "$LIBRARY.thin"
      mv "$LIBRARY.thin" "$LIBRARY"
      codesign --force --timestamp=none --sign - "$LIBRARY" 2>/dev/null
      ;;
  esac
done
