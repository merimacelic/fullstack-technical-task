#!/usr/bin/env bash
# =============================================================================
# Idempotent Azure deployment wrapper.
# Creates (or updates) the resource group + everything in main.bicep, then
# prints the public URLs. Safe to re-run — Bicep is declarative and will only
# apply diffs.
#
# Prerequisites:
#   - Azure CLI logged in: az login
#   - A recent Bicep CLI (bundled with az 2.20+)
#   - Subscription has accepted the free-SQL-offer terms (one-time, done via
#     the Azure Portal the first time you create a free SQL database).
#
# Usage:
#   chmod +x deploy.sh
#   ./deploy.sh                    # uses defaults from deploy.env if present
#
# All parameters are read from environment variables so the same script works
# locally and from CI. deploy.env.example lists every variable.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Load deploy.env if present; real secrets stay out of git (see .gitignore).
if [[ -f "$SCRIPT_DIR/deploy.env" ]]; then
  # shellcheck disable=SC1091
  source "$SCRIPT_DIR/deploy.env"
fi

: "${AZURE_SUBSCRIPTION:?Set AZURE_SUBSCRIPTION to the subscription id or name}"
: "${AZURE_LOCATION:=westeurope}"
: "${RESOURCE_GROUP:=rg-taskmanagement}"
: "${PROJECT_NAME:=tasksmt}"
: "${SQL_ADMIN_LOGIN:?Set SQL_ADMIN_LOGIN (not 'admin' / 'sa')}"
: "${SQL_ADMIN_PASSWORD:?Set SQL_ADMIN_PASSWORD (12+ chars, meets Azure SQL complexity)}"
: "${JWT_SECRET:?Set JWT_SECRET (32+ chars)}"
: "${API_IMAGE:?Set API_IMAGE (e.g. ghcr.io/you/task-management/api:latest)}"
: "${SPA_IMAGE:?Set SPA_IMAGE (e.g. ghcr.io/you/task-management/frontend:latest)}"
: "${SEED_DEMO_DATA:=true}"
: "${EXTRA_CORS_ORIGIN:=}"

echo "==> Selecting subscription $AZURE_SUBSCRIPTION"
az account set --subscription "$AZURE_SUBSCRIPTION"

echo "==> Ensuring resource group $RESOURCE_GROUP exists in $AZURE_LOCATION"
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$AZURE_LOCATION" \
  --only-show-errors \
  --output none

echo "==> Ensuring required resource providers are registered"
for provider in Microsoft.App Microsoft.OperationalInsights Microsoft.Sql; do
  az provider register --namespace "$provider" --only-show-errors --output none
done

echo "==> Deploying main.bicep (this can take 5-10 minutes the first time)"
DEPLOYMENT_NAME="tasks-$(date -u +%Y%m%d%H%M%S)"
az deployment group create \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters \
      projectName="$PROJECT_NAME" \
      location="$AZURE_LOCATION" \
      sqlAdminLogin="$SQL_ADMIN_LOGIN" \
      sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
      jwtSecret="$JWT_SECRET" \
      apiImage="$API_IMAGE" \
      spaImage="$SPA_IMAGE" \
      seedDemoData="$SEED_DEMO_DATA" \
      extraCorsOrigin="$EXTRA_CORS_ORIGIN" \
  --only-show-errors \
  --output none

echo "==> Reading deployment outputs"
OUTPUTS=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query properties.outputs \
  --output json)

API_URL=$(echo "$OUTPUTS" | sed -n 's/.*"apiUrl":[[:space:]]*{[[:space:]]*"type":[[:space:]]*"String",[[:space:]]*"value":[[:space:]]*"\([^"]*\)".*/\1/p' | head -n1)
SPA_URL=$(echo "$OUTPUTS" | sed -n 's/.*"spaUrl":[[:space:]]*{[[:space:]]*"type":[[:space:]]*"String",[[:space:]]*"value":[[:space:]]*"\([^"]*\)".*/\1/p' | head -n1)
SQL_FQDN=$(echo "$OUTPUTS" | sed -n 's/.*"sqlFqdn":[[:space:]]*{[[:space:]]*"type":[[:space:]]*"String",[[:space:]]*"value":[[:space:]]*"\([^"]*\)".*/\1/p' | head -n1)

cat <<EOF

==============================================================================
Deployment complete.

  SPA    : $SPA_URL
  API    : $API_URL
  SQL    : $SQL_FQDN

  Demo credentials (seeded if Seeding__DemoData=true):
    email    : demo@icon.mt
    password : Passw0rd!

Notes:
  - First request after ~1h idle wakes both the Container App (~3-5s) and the
    SQL database (~10-30s). Subsequent requests are warm.
  - Re-running deploy.sh is safe — Bicep applies only the diff.
  - To tear everything down: az group delete --name $RESOURCE_GROUP --yes --no-wait
==============================================================================
EOF
