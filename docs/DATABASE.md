# Database — Amazon RDS for SQL Server

## Migration Plan (Shared Hosting → RDS)

### 1. Provision RDS Instance (AWS Console or IaC)

| Setting | Value |
|---------|-------|
| Engine | SQL Server Express Edition (or Standard if >10GB) |
| Instance Class | `db.t3.small` (2 vCPU, 2GB RAM) — start small |
| Storage | 20GB gp3, auto-scaling enabled |
| VPC | Same VPC as ECS service |
| Public Access | **No** — only accessible from ECS security group |
| Backup | Automated backups enabled, 7-day retention |
| Multi-AZ | Not needed for dev/staging; optional for production |

### 2. Security Group Rules

| Direction | Protocol | Port | Source | Purpose |
|-----------|----------|------|--------|---------|
| Inbound | TCP | 1433 | ECS tasks security group | Application access |
| Inbound | TCP | 1433 | Bastion / VPN (optional) | Admin access |

### 3. Export & Import Data

```bash
# On current shared hosting, generate a bacpac or script
# Then restore to RDS:
sqlcmd -S <RDS_ENDPOINT> -U admin -P <password> -i export.sql
```

Or use the EF Core approach:

```bash
# Ensure all migrations are applied to RDS
dotnet ef database update --connection "Server=<RDS_ENDPOINT>;Database=GenZCoders;User Id=admin;Password=<password>;TrustServerCertificate=True"
```

### 4. Connection String

The app reads the connection string from `ConnectionStrings:DefaultConnection`. In production on ECS, this comes from **AWS Secrets Manager** (see `AWS_DEPLOYMENT.md`).

Format:
```
Server=<RDS_ENDPOINT>,1433;Database=GenZCoders;User Id=<username>;Password=<password>;TrustServerCertificate=True;Encrypt=True;
```

**Never hardcode** — use the ECS secrets injection described in the deployment guide.

### 5. Migration Strategy

- `Program.cs` already runs `await db.Database.MigrateAsync()` on startup (idempotent).
- **Production gate**: do NOT auto-migrate production without a manual approval step in the CI/CD deploy workflow. Use the deploy workflow's environment protection rule.
- Destructive changes (column drops, table drops) should be done via a separate migration PR and applied manually after verification.

### 6. Backup & Recovery

- RDS automated backups: 7-day retention (configurable)
- Before any migration: take a manual snapshot
- Point-in-time recovery: supported by RDS
