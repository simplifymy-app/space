#!/bin/sh
set -e
cd "$(dirname "$0")/../.."

CONFIGURATION=${CONFIGURATION:-Release}
RUNTIME=${RUNTIME:-osx-arm64}
VERSION=${VERSION:-$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' Directory.Build.props | head -1)}
DOTNET=${DOTNET:-dotnet}
OUTPUT=build
BUNDLE="$OUTPUT/Simplify my Space.app"

SIGN_IDENTITY=${SIGN_IDENTITY:-Simplify my Space Dev}

find_identity() {
  security find-identity -v -p codesigning 2>/dev/null \
    | grep -F "$1" | grep -v CSSMERR | head -1 | awk '{print $2}'
}

if [ -z "$SIGNATURE" ]; then
  SIGNATURE=$(find_identity "$SIGN_IDENTITY")
fi

if [ -z "$SIGNATURE" ]; then
  SIGNATURE=$(find_identity "Apple Development")
  [ -n "$SIGNATURE" ] && echo "note: '$SIGN_IDENTITY' not found, using an Apple Development certificate." >&2
fi

if [ -z "$SIGNATURE" ]; then
  SIGNATURE=-
  echo "warning: no signing certificate found, falling back to ad-hoc signing." >&2
  echo "warning: run make certificate once so macOS keeps permissions between builds." >&2
fi

if [ "$CONFIGURATION" = "Debug" ]; then
  PUBLISH_OPTIONS="-p:PublishSingleFile=false -p:PublishTrimmed=false -p:EnableCompressionInSingleFile=false"
else
  PUBLISH_OPTIONS=""
fi

rm -rf "$OUTPUT/publish-bundle" "$BUNDLE"
"$DOTNET" publish src/App -c "$CONFIGURATION" -r "$RUNTIME" --self-contained true -o "$OUTPUT/publish-bundle" $PUBLISH_OPTIONS
rm -f "$OUTPUT"/publish-bundle/*.pdb
sh scripts/macos/thin.sh "$OUTPUT/publish-bundle" "$RUNTIME"

mkdir -p "$BUNDLE/Contents/MacOS" "$BUNDLE/Contents/Resources"
sh scripts/macos/plist.sh "$VERSION" "$BUNDLE/Contents/Info.plist"
cp assets/icon/AppIcon.icns "$BUNDLE/Contents/Resources/AppIcon.icns"
cp -R "$OUTPUT/publish-bundle/." "$BUNDLE/Contents/MacOS/"
rm -rf "$OUTPUT/publish-bundle"
chmod +x "$BUNDLE/Contents/MacOS/SimplifyMySpace"

find "$BUNDLE/Contents/MacOS" -type f ! -name SimplifyMySpace \
  -exec codesign --force --timestamp=none --sign "$SIGNATURE" {} +
codesign --force --timestamp=none --identifier com.simplifymyspace.app --sign "$SIGNATURE" "$BUNDLE"

echo "Built $BUNDLE ($(du -sh "$BUNDLE" | cut -f1))"
