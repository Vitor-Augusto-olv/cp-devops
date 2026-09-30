#!/usr/bin/env bash
# =============================================================================
# DimDim - Remove TODOS os recursos criados (evita consumir o crédito da conta).
# Só rode depois que a nota do checkpoint for divulgada: o professor pode
# precisar acessar a aplicação durante a correção.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/.dimdim.env"

read -r -p "Isso apagará o Resource Group '$RG' e tudo dentro dele. Digite o nome do grupo ($RG) para confirmar: " CONFIRMA
if [[ "$CONFIRMA" != "$RG" ]]; then
  echo "Nome diferente. Nada foi apagado."
  exit 1
fi

az group delete --name "$RG" --yes --no-wait
rm -f "$SCRIPT_DIR/.dimdim.env"
echo "Exclusão iniciada (leva alguns minutos). Acompanhe com: az group exists --name $RG"
