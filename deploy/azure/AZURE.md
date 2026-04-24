# Deploying to Azure (free tier)

End-to-end walkthrough for hosting the Task Management app on Azure at zero cost. Target audience: anyone with a fresh Azure account.

## What gets provisioned

| Resource | SKU | Why it's free |
|---|---|---|
| Azure SQL Database | `GP_S_Gen5_2` serverless, 32 GB | [Azure SQL Database Free Offer](https://learn.microsoft.com/azure/azure-sql/database/free-offer) — 100K vCore-seconds + 32 GB/mo forever. Auto-pauses after 1 h idle. |
| Container App (API) | Consumption, 0.25 vCPU / 0.5 GiB | First 180K vCPU-s + 360K GiB-s + 2M requests / subscription / month are free. Scale-to-zero keeps us inside it. |
| Container App (SPA) | Consumption, 0.25 vCPU / 0.5 GiB | Same free grant as above. |
| Container Apps Env | Consumption | No standalone charge. |
| Log Analytics | `PerGB2018`, 30-day retention | First 5 GB ingestion/mo free. |

Single region. One replica per app. HTTPS + managed certificate on `*.<env>.azurecontainerapps.io` out of the box.

**Known caveat**: first request after idle wakes both the SQL database (~10–30 s) and the Container App (~3–5 s). Once warm, everything is fast. Acceptable trade for free-forever.

## Prerequisites

- An Azure account — [sign up free](https://azure.microsoft.com/free/). A credit card is used for identity only; nothing is charged as long as you stay inside the free tiers above.
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) 2.50+ (`az --version`).
- The GitHub Actions workflow in `.github/workflows/ci.yml` has already pushed images to `ghcr.io/<owner>/task-management/{api,frontend}:latest` on the default branch.
- (One-time) accept the Azure SQL Free Offer terms by creating a free database through the Portal: [Free SQL Database](https://azure.microsoft.com/products/azure-sql/database/). You only need to acknowledge the offer; no need to actually finish that portal flow.

## Steps

### 1. Log in and pick a subscription

```bash
az login
az account list --output table
az account set --subscription "<subscription-id-or-name>"
```

### 2. Configure `deploy.env`

```bash
cd deploy/azure
cp deploy.env.example deploy.env
# Edit deploy.env — set SQL_ADMIN_PASSWORD, JWT_SECRET (32+ chars), API_IMAGE, SPA_IMAGE
```

Password rules for `SQL_ADMIN_PASSWORD`: 12+ chars with at least one uppercase, lowercase, digit, and non-alphanumeric. Don't use `admin`/`sa`/`root` for `SQL_ADMIN_LOGIN`.

### 3. Run the deploy script

```bash
chmod +x deploy.sh
./deploy.sh
```

The script:
1. Creates the resource group (idempotent).
2. Registers the required resource providers.
3. Runs `az deployment group create` against `main.bicep`.
4. Prints the SPA URL, API URL, and SQL FQDN.

First run takes 5–10 minutes (SQL Server provisioning is the slow part). Re-runs are faster — Bicep is declarative and only pushes the diff.

### 4. Browse to the SPA URL

The seeded demo user works immediately:

```
email:    demo@icon.mt
password: Passw0rd!
```

It logs straight in to a dashboard with ~60 tasks, 15 tags, and varied statuses / priorities / due dates.

## Updating

Push to `main` → CI rebuilds and pushes new images to GHCR → run `./deploy.sh` again and Bicep rolls out a new revision. Container Apps traffic cuts over once the new revision's readiness probe passes.

To force a pull of the same tag without a new image:

```bash
az containerapp revision copy --name tasksmt-api --resource-group rg-taskmanagement
az containerapp revision copy --name tasksmt-spa --resource-group rg-taskmanagement
```

## Tearing it all down

```bash
az group delete --name rg-taskmanagement --yes --no-wait
```

Deletes every resource in one shot. No trailing invoices.

## Cost sanity check

Expected monthly cost on a quiet demo (reviewer opens the URL once or twice): **$0.00**.

The free grants absorb everything as long as the SPA and API stay mostly idle. If you deliberately hammer the stack (load tests, leaving tabs open) you can blow through the Container Apps grant and start paying pennies per hour. The Azure SQL free offer has `freeLimitExhaustionBehavior: AutoPause`, so the database simply stops responding rather than racking up charges once the monthly vCore-seconds run out.

## Troubleshooting

- **`The subscription is not registered to use the namespace ...`** — the script auto-registers Microsoft.App, Microsoft.OperationalInsights, and Microsoft.Sql, but registration can take a minute. Re-run `./deploy.sh`.
- **`Login failed for user '...' (Microsoft.Data.SqlClient)` from the API** — the SQL Server admin password in `deploy.env` was changed after the first deploy. Container App secrets are immutable within a revision; push a new revision: `./deploy.sh`.
- **`FreeLimitExhausted`** in the SQL logs — the database hit its 100K vCore-seconds cap for the month; auto-paused until next month. Not an error, not a billing event.
- **SPA shows "network error" on login** — the SPA's `API_BASE_URL` env var (baked into `env.js` at container start) didn't match the API Container App's FQDN, or CORS origins don't include the SPA URL. Check `az containerapp show --name tasksmt-api ... --query 'properties.template.containers[0].env'`.
- **Long cold start** — first hit after 1 h idle wakes SQL (~10–30 s) and the API Container App (~3–5 s). Expected behaviour; documents the trade for free hosting.

## What the reviewer sees

A single URL. They click it, log in as `demo@icon.mt`, see ~60 realistic tasks across pending/in-progress/completed, varied priorities, due dates (some overdue), tagged with ~15 different tags. Drag-and-drop, filters, tag manager, light/dark theme, English/Maltese locale switch — all live against a real Azure deployment.
