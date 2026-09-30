#!/usr/bin/env bash
# =============================================================================
# DimDim - ALTERNATIVA: deploy automatizado só com Azure CLI (az webapp deploy)
# Publica o projeto, gera um .zip e envia para o Web App. Use quando não for
# usar o GitHub Actions (ou para reenviar a aplicação rapidamente).
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/.dimdim.env"

# Garante o SDK do .NET 10 (o Cloud Shell pode ter outra versão).
if ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
  echo "SDK do .NET 10 não encontrado. Instalando em \$HOME/.dotnet ..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$HOME/dotnet-install.sh"
  bash "$HOME/dotnet-install.sh" --channel 10.0 --install-dir "$HOME/.dotnet"
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$DOTNET_ROOT:$PATH"
fi

SAIDA="$REPO_DIR/publish"
ZIP="$REPO_DIR/publish.zip"
rm -rf "$SAIDA" "$ZIP"

echo "Compilando e publicando (Release)..."
dotnet publish "$REPO_DIR/src/DimDim.Web/DimDim.Web.csproj" -c Release -o "$SAIDA"
(cd "$SAIDA" && zip -qr "$ZIP" .)

echo "Enviando para o Web App $WEBAPP ..."
az webapp deploy --resource-group "$RG" --name "$WEBAPP" --src-path "$ZIP" --type zip

HOST="$(az webapp show --name "$WEBAPP" --resource-group "$RG" --query defaultHostName -o tsv)"
echo "Verificando a aplicação (pode levar cerca de 1 minuto para iniciar)..."
curl --fail --silent --show-error --retry 12 --retry-delay 10 --retry-all-errors "https://$HOST/health" && echo
echo "Deploy concluído: https://$HOST"
