#!/bin/sh
set -e
cd "$(dirname "$0")/../.."

CONFIGURATION=${CONFIGURATION:-Release}
RUNTIME=${RUNTIME:-win-x64}
VERSION=${VERSION:-$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' Directory.Build.props | head -1)}
DOTNET=${DOTNET:-dotnet}
PUBLISH=build/publish-$RUNTIME
RELEASES=${RELEASES:-build/releases}

rm -rf "$PUBLISH"
mkdir -p "$RELEASES"

"$DOTNET" publish src/App -c "$CONFIGURATION" -r "$RUNTIME" --self-contained true -o "$PUBLISH"
rm -f "$PUBLISH"/*.pdb

set -- \
  --packId SimplifyMySpace \
  --packTitle "Simplify my Space" \
  --packAuthors "Simplify" \
  --packVersion "$VERSION" \
  --packDir "$PUBLISH" \
  --mainExe SimplifyMySpace.exe \
  --icon assets/icon/AppIcon.ico \
  --channel "$RUNTIME" \
  --outputDir "$RELEASES" \
  --splashImage assets/installer/splash.png \
  --splashProgressColor "#6E8BFF"

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*)
    set -- "$@" --msi \
      --msiBanner assets/installer/msi-banner.bmp \
      --msiLogo assets/installer/msi-logo.bmp
    [ -n "$SIGN_PARAMS" ] && set -- "$@" --signParams "$SIGN_PARAMS"
    vpk pack "$@"
    ;;
  *)
    echo "note: packing for Windows from $(uname -s): no MSI and no signing, those need Windows." >&2
    vpk "[win]" pack "$@"
    ;;
esac

[ -n "$SIGN_PARAMS" ] || echo "warning: no SIGN_PARAMS, the installer will not be signed." >&2
rm -rf "$PUBLISH"
echo "Release $VERSION for $RUNTIME written to $RELEASES"
