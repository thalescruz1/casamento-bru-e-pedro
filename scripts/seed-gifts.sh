#!/usr/bin/env bash
# Cadastra os 16 presentes da lua de mel via API admin.
# Uso:
#   ADMIN_EMAIL="..." ADMIN_PASSWORD="..." bash scripts/seed-gifts.sh
# Opcional:
#   BASE_URL="https://..." (default = HML)
# Dependências: bash, curl, jq

set -euo pipefail

BASE_URL="${BASE_URL:-https://polite-stone-01b8d8c0f.7.azurestaticapps.net}"
ADMIN_EMAIL="${ADMIN_EMAIL:?defina ADMIN_EMAIL no ambiente}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:?defina ADMIN_PASSWORD no ambiente}"

command -v jq >/dev/null 2>&1 || { echo "✖ jq não encontrado — instale com 'brew install jq'"; exit 1; }
command -v curl >/dev/null 2>&1 || { echo "✖ curl ausente"; exit 1; }

COOKIE_JAR="$(mktemp)"
TMP_DIR="$(mktemp -d)"
trap 'rm -f "$COOKIE_JAR"; rm -rf "$TMP_DIR"' EXIT

echo "▶ BASE_URL = $BASE_URL"
echo "▶ Logando como $ADMIN_EMAIL..."
login_resp=$(curl -sS -w '\n%{http_code}' -c "$COOKIE_JAR" \
  -H 'content-type: application/json' \
  -d "$(jq -n --arg e "$ADMIN_EMAIL" --arg p "$ADMIN_PASSWORD" '{email: $e, password: $p}')" \
  "$BASE_URL/api/auth/login")
login_code="${login_resp##*$'\n'}"
login_body="${login_resp%$'\n'*}"
if [[ "$login_code" != "200" ]]; then
  echo "✖ Login falhou (HTTP $login_code)"
  echo "$login_body"
  exit 1
fi
echo "  ✓ logado"
echo

# seed_gift TITLE DESCRIPTION PRICE IMAGE_QUERY
seed_gift() {
  local title="$1" description="$2" price="$3" image_query="$4"
  local hash; hash=$(printf '%s' "$title" | shasum | cut -c1-8)
  local img_path="$TMP_DIR/gift-$hash.jpg"

  echo "→ $title (R$ $price)"

  # Baixa imagem (LoremFlickr — Flickr-backed, fotos reais por keyword)
  if ! curl -sS -L --max-time 20 -o "$img_path" "https://loremflickr.com/720/720/${image_query}"; then
    echo "  ⚠ download falhou; tentando picsum como fallback"
    curl -sS -L --max-time 20 -o "$img_path" "https://picsum.photos/seed/${hash}/720/720"
  fi
  if [[ ! -s "$img_path" ]]; then
    echo "  ✖ não consegui obter imagem"
    return 1
  fi

  # Upload da imagem
  local upload_resp blob_name
  upload_resp=$(curl -sS -b "$COOKIE_JAR" -F "file=@$img_path" "$BASE_URL/api/gifts/upload-image")
  blob_name=$(printf '%s' "$upload_resp" | jq -r '.blobName // empty')
  if [[ -z "$blob_name" ]]; then
    echo "  ✖ upload falhou: $upload_resp"
    return 1
  fi
  echo "  ✓ imagem subida"

  # Cria o gift
  local create_resp gift_id
  create_resp=$(curl -sS -b "$COOKIE_JAR" \
    -H 'content-type: application/json' \
    -d "$(jq -n \
      --arg t "$title" \
      --arg d "$description" \
      --argjson p "$price" \
      --arg b "$blob_name" \
      '{title: $t, description: $d, price: $p, imageBlobName: $b}')" \
    "$BASE_URL/api/gifts")
  gift_id=$(printf '%s' "$create_resp" | jq -r '.id // empty')
  if [[ -z "$gift_id" ]]; then
    echo "  ✖ criação do gift falhou: $create_resp"
    return 1
  fi
  echo "  ✓ gift $gift_id criado"
  echo
}

seed_gift "Expresso pós almoço" \
  "Um cafezinho italiano logo depois da pasta — tradição que a gente quer levar pra viagem." \
  50 "espresso,coffee,italian"

seed_gift "Vale Gelato" \
  "Aquele gelato cremoso na piazza, no fim da tarde." \
  60 "gelato,italian-icecream"

seed_gift "Cappuccino durante a tarde para irritar os italianos" \
  "Tomar cappuccino depois das 11h — só pra ver os italianos balançando a cabeça." \
  100 "cappuccino,coffee"

seed_gift "Aperol Spritz ao pôr do sol" \
  "Aperol em mão, sol caindo. Não tem combo melhor." \
  100 "aperol,spritz,cocktail"

seed_gift "Souvenir Italiano de presente para o Cláudinho" \
  "Um mimo italiano pro Cláudio, nosso cachorrinho que vai estar com saudade." \
  120 "italy,venice,souvenir"

seed_gift "Café da manhã" \
  "A primeira refeição do dia, à mesa, com calma." \
  150 "breakfast,brunch"

seed_gift "Drinks no bar do hotel" \
  "Encerrar a noite no bar do hotel, conversando até tarde." \
  150 "cocktail,bar,hotel"

seed_gift "Almoço tradicional italiano" \
  "Massa fresca, vinho, mesa cheia de pão." \
  200 "pasta,italian,lunch"

seed_gift "Pizzada sem glúten" \
  "Pizza italiana adaptada pra Helo aproveitar igual." \
  250 "pizza,italian"

seed_gift "Degustação de vinhos" \
  "Uma manhã entre vinhedos descobrindo rótulos novos." \
  300 "wine,tasting,vineyard"

seed_gift "Picnic especial com queijos e vinhos" \
  "Cesta no parque, queijo, charcuterie, sol." \
  400 "picnic,cheese,wine"

seed_gift "Hotelzinho para o Cláudio durante a lua de mel" \
  "Um daycare 5 estrelas pro Cláudio enquanto a gente viaja." \
  500 "puppy,dog"

seed_gift "Visita autoguiada às vinhas" \
  "Um passeio sem pressa por entre as vinhas." \
  600 "vineyard,grapes"

seed_gift "Relax a Due — Day spa para os recém-casados" \
  "Massagem pra dois, pra tirar a tensão do casamento." \
  1000 "spa,massage,wellness"

seed_gift "Jantar à Luz de Velas" \
  "Jantar à luz de velas em algum cantinho especial." \
  1500 "candlelight,dinner,romantic"

seed_gift "5 diárias no hotel" \
  "5 noites pra estender a viagem mais um pouco." \
  3000 "hotel,suite,luxury"

echo "✓ Tudo cadastrado!"
