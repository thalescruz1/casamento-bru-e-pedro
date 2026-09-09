#!/usr/bin/env bash
# Migra todos os gifts (com imagens) de um SWA pra outro.
# Uso:
#   FROM_BASE="https://hml..." FROM_PWD="..." \
#   TO_BASE="https://prod..." TO_PWD="..." \
#   ADMIN_EMAIL="thales@casamento.com" \
#   bash scripts/migrate-gifts.sh

set -euo pipefail

FROM_BASE="${FROM_BASE:?defina FROM_BASE}"
TO_BASE="${TO_BASE:?defina TO_BASE}"
FROM_PWD="${FROM_PWD:?defina FROM_PWD}"
TO_PWD="${TO_PWD:?defina TO_PWD}"
ADMIN_EMAIL="${ADMIN_EMAIL:?defina ADMIN_EMAIL}"

command -v jq >/dev/null 2>&1 || { echo "✖ jq ausente"; exit 1; }

FROM_JAR="$(mktemp)"
TO_JAR="$(mktemp)"
TMP_DIR="$(mktemp -d)"
trap 'rm -f "$FROM_JAR" "$TO_JAR"; rm -rf "$TMP_DIR"' EXIT

login() {
  local base="$1" pwd="$2" jar="$3"
  local code
  code=$(curl -sS -o /dev/null -w '%{http_code}' -c "$jar" \
    -H 'content-type: application/json' \
    -d "$(jq -n --arg e "$ADMIN_EMAIL" --arg p "$pwd" '{email: $e, password: $p}')" \
    "$base/api/auth/login")
  if [[ "$code" != "200" ]]; then
    echo "  ✖ Login em $base falhou (HTTP $code)"
    return 1
  fi
}

echo "▶ Login em FROM=$FROM_BASE..."
login "$FROM_BASE" "$FROM_PWD" "$FROM_JAR"
echo "  ✓"

echo "▶ Login em TO=$TO_BASE..."
login "$TO_BASE" "$TO_PWD" "$TO_JAR"
echo "  ✓"

echo
echo "▶ Listando gifts no FROM (admin)..."
GIFTS_JSON=$(curl -sS -b "$FROM_JAR" "$FROM_BASE/api/gifts/admin")
COUNT=$(echo "$GIFTS_JSON" | jq length)
echo "  ✓ $COUNT gifts encontrados"

echo
echo "▶ Verificando se TO já tem gifts (segurança contra duplicação)..."
TO_COUNT=$(curl -sS -b "$TO_JAR" "$TO_BASE/api/gifts/admin" | jq length)
if [[ "$TO_COUNT" -gt 0 ]]; then
  echo "  ⚠ TO já tem $TO_COUNT gifts. Aborta pra evitar duplicar."
  echo "  Pra forçar: limpe o destino antes (admin → remover) ou use --force."
  if [[ "${FORCE:-}" != "1" ]]; then exit 1; fi
fi
echo "  ✓ vazio"

echo
echo "▶ Migrando..."
i=0
echo "$GIFTS_JSON" | jq -c '.[]' | while IFS= read -r gift; do
  i=$((i+1))
  TITLE=$(echo "$gift" | jq -r '.title')
  DESCRIPTION=$(echo "$gift" | jq -r '.description')
  PRICE=$(echo "$gift" | jq -r '.price')
  IMG_URL=$(echo "$gift" | jq -r '.imageUrl // empty')

  echo "  [$i/$COUNT] $TITLE (R\$ $PRICE)"

  BLOB_NAME=""
  if [[ -n "$IMG_URL" ]]; then
    IMG_PATH="$TMP_DIR/img-$i.jpg"
    if curl -sS -L --max-time 30 -o "$IMG_PATH" "$IMG_URL" && [[ -s "$IMG_PATH" ]]; then
      UPLOAD_RESP=$(curl -sS -b "$TO_JAR" -F "file=@$IMG_PATH" "$TO_BASE/api/gifts/upload-image")
      BLOB_NAME=$(echo "$UPLOAD_RESP" | jq -r '.blobName // empty')
      if [[ -z "$BLOB_NAME" ]]; then
        echo "    ⚠ upload de imagem falhou: $UPLOAD_RESP"
      fi
    else
      echo "    ⚠ download da imagem falhou — gift sem imagem"
    fi
  fi

  PAYLOAD=$(jq -n \
    --arg t "$TITLE" \
    --arg d "$DESCRIPTION" \
    --argjson p "$PRICE" \
    --arg b "$BLOB_NAME" \
    '{title: $t, description: $d, price: $p, imageBlobName: ($b | select(. != "") // null)}')

  CREATE_RESP=$(curl -sS -b "$TO_JAR" \
    -H 'content-type: application/json' \
    -d "$PAYLOAD" \
    "$TO_BASE/api/gifts")
  GIFT_ID=$(echo "$CREATE_RESP" | jq -r '.id // empty')
  if [[ -z "$GIFT_ID" ]]; then
    echo "    ✖ falhou ao criar gift: $CREATE_RESP"
  else
    echo "    ✓ criado $GIFT_ID"
  fi
done

echo
echo "✓ Migração concluída"
