#!/bin/sh
set -e
cd "$(dirname "$0")/../.."

CONFIGURATION=${CONFIGURATION:-Release}
RUNTIME=${RUNTIME:-osx-arm64}
VERSION=${VERSION:-$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' Directory.Build.props | head -1)}
DOTNET=${DOTNET:-dotnet}
OUTPUT=build
PUBLISH="$OUTPUT/publish-$RUNTIME"
RELEASES=${RELEASES:-$OUTPUT/releases}

rm -rf "$PUBLISH"
mkdir -p "$RELEASES"

"$DOTNET" publish src/App -c "$CONFIGURATION" -r "$RUNTIME" --self-contained true -o "$PUBLISH"
rm -f "$PUBLISH"/*.pdb
sh scripts/macos/thin.sh "$PUBLISH" "$RUNTIME"

PLIST="$OUTPUT/Info-$RUNTIME.plist"
sh scripts/macos/plist.sh "$VERSION" "$PLIST"

set -- \
  --packId SimplifyMySpace \
  --packTitle "Simplify my Space" \
  --packAuthors "Simplify" \
  --packVersion "$VERSION" \
  --packDir "$PUBLISH" \
  --mainExe SimplifyMySpace \
  --icon assets/icon/AppIcon.icns \
  --plist "$PLIST" \
  --channel "$RUNTIME" \
  --outputDir "$RELEASES" \
  --instWelcome assets/installer/welcome.rtf \
  --instConclusion assets/installer/conclusion.rtf

[ -n "$SIGN_IDENTITY" ] && set -- "$@" --signAppIdentity "$SIGN_IDENTITY"
[ -n "$SIGN_INSTALL_IDENTITY" ] && set -- "$@" --signInstallIdentity "$SIGN_INSTALL_IDENTITY"
[ -n "$NOTARY_PROFILE" ] && set -- "$@" --notaryProfile "$NOTARY_PROFILE"
[ -n "$KEYCHAIN" ] && set -- "$@" --keychain "$KEYCHAIN"

if [ -z "$SIGN_IDENTITY" ]; then
  echo "warning: no SIGN_IDENTITY, the release will not be signed or notarized." >&2
fi

vpk pack "$@"

rm -rf "$PUBLISH" "$PLIST"
echo "Release $VERSION for $RUNTIME written to $RELEASES"
