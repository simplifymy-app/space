#!/bin/sh
set -e

NAME=${NAME:-Simplify my Space Dev}
KEYCHAIN="$HOME/Library/Keychains/login.keychain-db"

if security find-identity -v -p codesigning | grep -qF "$NAME"; then
  echo "Identity \"$NAME\" already exists - nothing to do."
  exit 0
fi

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT INT TERM

cat > "$WORK/cert.cnf" <<CNF
[req]
distinguished_name = dn
x509_extensions = ext
prompt = no
[dn]
CN = $NAME
[ext]
basicConstraints = critical,CA:false
keyUsage = critical,digitalSignature
extendedKeyUsage = critical,codeSigning
CNF

openssl req -x509 -newkey rsa:2048 -nodes -days 7300 \
  -config "$WORK/cert.cnf" -keyout "$WORK/key.pem" -out "$WORK/cert.pem" 2>/dev/null

openssl pkcs12 -export -inkey "$WORK/key.pem" -in "$WORK/cert.pem" -out "$WORK/ident.p12" \
  -passout pass:temp -name "$NAME" \
  -certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES -macalg sha1 2>/dev/null

echo "Importing \"$NAME\" into your login keychain..."
security import "$WORK/ident.p12" -k "$KEYCHAIN" -P temp \
  -T /usr/bin/codesign -T /usr/bin/security

echo "Marking it trusted for code signing..."
security add-trusted-cert -r trustRoot -p codeSign -k "$KEYCHAIN" "$WORK/cert.pem"

security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "" "$KEYCHAIN" >/dev/null 2>&1 || \
  echo "note: codesign may ask for keychain access on first use - choose Always Allow."

echo
security find-identity -v -p codesigning | grep -F "$NAME" || {
  echo "error: the identity was not registered. Check Keychain Access for \"$NAME\"." >&2
  exit 1
}
echo
echo "Done. scripts/macos/bundle.sh will now pick this identity automatically."
echo "If you ever delete and recreate it, macOS will ask for permission once more."
