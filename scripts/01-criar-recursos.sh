#!/usr/bin/env bash
# =============================================================================
# DimDim - PASSO 1: cria TODA a infraestrutura na Azure usando o Azure CLI
#
#   Resource Group -> Log Analytics -> Application Insights -> SQL Server ->
#   Firewall -> Azure SQL Database -> App Service Plan -> Web App -> App Settings
#
# Onde executar: Azure Cloud Shell (Bash) ou terminal bash com "az login" feito.
# Uso:           GRUPO=meugrupo bash scripts/01-criar-recursos.sh
#
# Variáveis opcionais: LOCATION (padrão brazilsouth), SKU (padrão B1), RUNTIME,
#                      SQL_ADMIN, SQL_PASSWORD (se omitida, será pedida sem eco).
# NENHUMA senha é gravada em arquivo: ela só existe na memória durante o script.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.dimdim.env"

# Se já rodou antes, reaproveita os mesmos nomes (evita criar recursos duplicados).
if [[ -f "$ENV_FILE" && -z "${SUFIXO:-}" ]]; then
  # shellcheck disable=SC1090
  source "$ENV_FILE"
fi

# ------------------------------ 1. Variáveis ---------------------------------
GRUPO="${GRUPO:-dimdim}"                 # nome do grupo: minúsculas e números, 3 a 12 caracteres
SUFIXO="${SUFIXO:-$RANDOM}"              # torna os nomes globais únicos na Azure
LOCATION="${LOCATION:-brazilsouth}"
SKU="${SKU:-B1}"                         # plano do App Service (F1 = gratuito, com limitações)
RUNTIME="${RUNTIME:-DOTNETCORE:10.0}"    # confira com: az webapp list-runtimes --os linux
SQL_ADMIN="${SQL_ADMIN:-dimdimadmin}"

if [[ ! "$GRUPO" =~ ^[a-z0-9]{3,12}$ ]]; then
  echo "ERRO: GRUPO deve ter de 3 a 12 caracteres, apenas letras minúsculas e números. Ex.: GRUPO=alfa42"
  exit 1
fi

RG="rg-${GRUPO}-webapp"
LOG_WORKSPACE="log-${GRUPO}"
APPINSIGHTS="appi-${GRUPO}"
SQL_SERVER="sql-${GRUPO}-${SUFIXO}"
SQL_DB="dimdimdb"
PLAN="plan-${GRUPO}"
WEBAPP="app-${GRUPO}-${SUFIXO}"

# Senha do administrador do SQL (pedida sem aparecer na tela).
if [[ -z "${SQL_PASSWORD:-}" ]]; then
  read -r -s -p "Defina a senha do admin do SQL (mínimo 12 caracteres, com maiúscula, minúscula e número; use só letras, números e _ - # @ %): " SQL_PASSWORD
  echo
fi
if [[ ! "$SQL_PASSWORD" =~ ^[A-Za-z0-9_@#%-]{12,}$ ]]; then
  echo "ERRO: senha inválida. Use 12+ caracteres apenas com letras, números e _ - # @ %"
  exit 1
fi

az account show >/dev/null 2>&1 || { echo "ERRO: faça login antes com: az login"; exit 1; }
az config set extension.use_dynamic_install=yes_without_prompt extension.dynamic_install_allow_preview=true >/dev/null 2>&1 || true

echo "=============================================================="
echo " Assinatura em uso:"
az account show --query "{nome:name, id:id}" -o table
echo " Grupo: $GRUPO | Região: $LOCATION | Sufixo: $SUFIXO"
echo "=============================================================="

# ------------------------------ 2. Provedores --------------------------------
echo "[1/9] Registrando provedores de recursos (só demora na primeira vez)..."
for p in Microsoft.Web Microsoft.Sql Microsoft.OperationalInsights Microsoft.Insights; do
  az provider register --namespace "$p" --wait -o none || echo "AVISO: não foi possível registrar $p"
done

# ------------------------------ 3. Resource Group ----------------------------
echo "[2/9] Criando o Resource Group: $RG"
az group create --name "$RG" --location "$LOCATION" -o none

# ------------------------------ 4. Application Insights ----------------------
echo "[3/9] Criando Log Analytics + Application Insights: $APPINSIGHTS"
az monitor log-analytics workspace create --resource-group "$RG" --workspace-name "$LOG_WORKSPACE" --location "$LOCATION" -o none
WS_ID="$(az monitor log-analytics workspace show --resource-group "$RG" --workspace-name "$LOG_WORKSPACE" --query id -o tsv)"
az monitor app-insights component create --app "$APPINSIGHTS" --resource-group "$RG" --location "$LOCATION" \
  --kind web --application-type web --workspace "$WS_ID" -o none
AI_CONN="$(az monitor app-insights component show --app "$APPINSIGHTS" --resource-group "$RG" --query connectionString -o tsv)"

# ------------------------------ 5. SQL Server + firewall ---------------------
echo "[4/9] Criando o servidor Azure SQL: $SQL_SERVER"
az sql server create --name "$SQL_SERVER" --resource-group "$RG" --location "$LOCATION" \
  --admin-user "$SQL_ADMIN" --admin-password "$SQL_PASSWORD" --minimal-tls-version 1.2 -o none

echo "[5/9] Configurando o firewall do servidor SQL"
# Regra 0.0.0.0 = "Permitir serviços do Azure" (necessária para o Web App e o Cloud Shell acessarem o banco)
az sql server firewall-rule create --resource-group "$RG" --server "$SQL_SERVER" \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none
MEU_IP="$(curl -s -m 10 https://api.ipify.org || true)"
if [[ "$MEU_IP" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  az sql server firewall-rule create --resource-group "$RG" --server "$SQL_SERVER" \
    --name AllowMyIP --start-ip-address "$MEU_IP" --end-ip-address "$MEU_IP" -o none
fi

echo "[6/9] Criando o banco Azure SQL Database: $SQL_DB (edição Basic)"
az sql db create --resource-group "$RG" --server "$SQL_SERVER" --name "$SQL_DB" \
  --edition Basic --backup-storage-redundancy Local -o none

# ------------------------------ 6. App Service + Web App ---------------------
echo "[7/9] Criando o App Service Plan (Linux, $SKU): $PLAN"
az appservice plan create --name "$PLAN" --resource-group "$RG" --location "$LOCATION" --is-linux --sku "$SKU" -o none

echo "[8/9] Criando o Web App ($RUNTIME): $WEBAPP"
az webapp create --name "$WEBAPP" --resource-group "$RG" --plan "$PLAN" --runtime "$RUNTIME" -o none
az webapp update --name "$WEBAPP" --resource-group "$RG" --https-only true -o none
if [[ "$SKU" != "F1" ]]; then
  az webapp config set --name "$WEBAPP" --resource-group "$RG" --always-on true -o none
fi
# Permite o deploy via GitHub Actions com "publish profile" (autenticação básica no SCM)
az resource update --resource-group "$RG" --namespace Microsoft.Web --resource-type basicPublishingCredentialsPolicies \
  --parent "sites/$WEBAPP" --name scm --set properties.allow=true -o none \
  || echo "AVISO: não foi possível habilitar o publish profile; use o deploy manual (scripts/04-deploy-manual.sh)."
az webapp log config --name "$WEBAPP" --resource-group "$RG" --application-logging filesystem --level information -o none

# ------------------------------ 7. Configurações (sem segredo no código) -----
echo "[9/9] Gravando connection string e Application Insights nas App Settings do Web App"
CONN="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${SQL_DB};User ID=${SQL_ADMIN};Password=${SQL_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
# "-o none" evita imprimir a connection string na tela (importante para o vídeo!)
az webapp config connection-string set --name "$WEBAPP" --resource-group "$RG" \
  --connection-string-type SQLAzure --settings DimDimDb="$CONN" -o none
az webapp config appsettings set --name "$WEBAPP" --resource-group "$RG" \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="$AI_CONN" ASPNETCORE_ENVIRONMENT=Production -o none

# Guarda apenas nomes (nunca senhas) para os próximos scripts.
cat > "$ENV_FILE" <<EOF
GRUPO=$GRUPO
SUFIXO=$SUFIXO
LOCATION=$LOCATION
RG=$RG
SQL_SERVER=$SQL_SERVER
SQL_DB=$SQL_DB
SQL_ADMIN=$SQL_ADMIN
WEBAPP=$WEBAPP
APPINSIGHTS=$APPINSIGHTS
EOF

HOST="$(az webapp show --name "$WEBAPP" --resource-group "$RG" --query defaultHostName -o tsv)"
echo
echo "=============================================================="
echo " Infraestrutura criada com sucesso!"
echo "   Resource Group : $RG"
echo "   Web App        : $WEBAPP  ->  https://$HOST"
echo "   SQL Server     : ${SQL_SERVER}.database.windows.net  (banco: $SQL_DB, usuário: $SQL_ADMIN)"
echo "   App Insights   : $APPINSIGHTS"
echo
echo " Próximo passo: bash scripts/02-criar-tabelas.sh"
echo "=============================================================="
