#!/bin/sh
# Bumps <Version> in Directory.Build.props, which is what cuts a release once
# pushed to main. Usually run as make bump-patch / bump-minor / bump-major.
#
#   sh build/bump.sh patch|minor|major    0.1.3 -> 0.1.4, 0.2.0, 1.0.0
#   sh build/bump.sh 1.4.0                set it exactly
#
set -e
cd "$(dirname "$0")/.."

CURRENT=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' Directory.Build.props | head -1)
PART=$1

case "$PART" in
  major|minor|patch)
    NEXT=$(CURRENT="$CURRENT" PART="$PART" awk 'BEGIN {
      split(ENVIRON["CURRENT"], v, ".")
      major = v[1] + 0; minor = v[2] + 0; patch = v[3] + 0
      if (ENVIRON["PART"] == "major") { major++; minor = 0; patch = 0 }
      else if (ENVIRON["PART"] == "minor") { minor++; patch = 0 }
      else { patch++ }
      printf "%d.%d.%d", major, minor, patch
    }')
    ;;
  [0-9]*)
    NEXT=$PART
    ;;
  *)
    echo "usage: sh build/bump.sh major|minor|patch|<version>" >&2
    echo "current version is $CURRENT" >&2
    exit 1
    ;;
esac

sed "s|<Version>$CURRENT</Version>|<Version>$NEXT</Version>|" Directory.Build.props > Directory.Build.props.tmp
mv Directory.Build.props.tmp Directory.Build.props
echo "$CURRENT -> $NEXT"
