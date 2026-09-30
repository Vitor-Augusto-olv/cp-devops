#!/usr/bin/env bash
# =============================================================================
# DimDim - PASSO 2: executa o DDL (scripts/ddl-tabelas.sql) no Azure SQL Database
# Requer o utilitário "sqlcmd" (já vem instalado no Azure Cloud Shell).
# Alternativa sem sqlcmd: Portal do Azure > banco > Query editor > cole o .sql
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/.dimdim.env"

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "sqlcmd não encontrado. Use o Query editor do Portal do Azure e cole o conteúdo de scripts/ddl-tabelas.sql."
  exit 1
fi

if [[ -z "${SQL_PASSWORD:-}" ]]; then
  read -r -s -p "Senha do admin do SQL ($SQL_ADMIN): " SQL_PASSWORD
  echo
fi

SERVIDOR="tcp:${SQL_SERVER}.database.windows.net,1433"

echo "Executando o DDL em ${SQL_SERVER}/${SQL_DB} ..."
sqlcmd -S "$SERVIDOR" -d "$SQL_DB" -U "$SQL_ADMIN" -P "$SQL_PASSWORD" -N -l 30 -b -i "$SCRIPT_DIR/ddl-tabelas.sql"

echo "Tabelas existentes no banco:"
sqlcmd -S "$SERVIDOR" -d "$SQL_DB" -U "$SQL_ADMIN" -P "$SQL_PASSWORD" -N -l 30 -Q "SELECT name AS tabela FROM sys.tables ORDER BY name"

echo "Pronto. Próximo passo: bash scripts/03-configurar-github-actions.sh"
