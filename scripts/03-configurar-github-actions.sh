#!/usr/bin/env bash
# =============================================================================
# DimDim - PASSO 3: prepara o deploy automatizado com GitHub Actions
#
# O workflow (.github/workflows/deploy.yml) precisa de:
#   - secret   AZURE_WEBAPP_PUBLISH_PROFILE  (credencial de deploy do Web App)
#   - variável AZURE_WEBAPP_NAME             (nome do Web App)
#
# Este script obtém o publish profile com o Azure CLI e, se o GitHub CLI (gh)
# estiver autenticado, grava o secret e a variável direto no repositório.
# O conteúdo do publish profile NÃO é exibido na tela.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/.dimdim.env"

PERFIL="$HOME/publish-profile-${WEBAPP}.xml"
az webapp deployment list-publishing-profiles --name "$WEBAPP" --resource-group "$RG" --xml -o tsv > "$PERFIL"
chmod 600 "$PERFIL"

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  read -r -p "Repositório no GitHub (formato usuario/repositorio): " REPO
  gh secret set AZURE_WEBAPP_PUBLISH_PROFILE --repo "$REPO" < "$PERFIL"
  gh variable set AZURE_WEBAPP_NAME --repo "$REPO" --body "$WEBAPP"
  rm -f "$PERFIL"
  echo "Secret e variável gravados em $REPO. Agora faça um push na branch main (ou rode o workflow manualmente na aba Actions)."
else
  cat <<EOF

O GitHub CLI (gh) não está disponível/autenticado. Configure manualmente no GitHub:
  Repositório > Settings > Secrets and variables > Actions

  1) Aba "Secrets"   > New repository secret
       Nome : AZURE_WEBAPP_PUBLISH_PROFILE
       Valor: conteúdo do arquivo $PERFIL
              (ou baixe pelo Portal: Web App > Overview > Download publish profile)
  2) Aba "Variables" > New repository variable
       Nome : AZURE_WEBAPP_NAME
       Valor: $WEBAPP

ATENÇÃO: não mostre o conteúdo do publish profile no vídeo e apague o arquivo depois:
  rm -f "$PERFIL"
EOF
fi
