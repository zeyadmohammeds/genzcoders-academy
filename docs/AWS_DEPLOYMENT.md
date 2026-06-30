# AWS Deployment Guide — genzcoders-backend

## Architecture

```
Internet
   │
   ▼
Route 53 ───► CloudFront (optional)
   │
   ▼
Application Load Balancer (HTTPS, ACM cert)
   │
   ▼
ECS Fargate ───► Amazon RDS (SQL Server)
   │
   ▼
AWS Secrets Manager (connection string, API keys)
```

## Components

### 1. Amazon ECR (Elastic Container Registry)

Stores the Docker image. Push after every CI build.

### 2. Amazon ECS on Fargate

Runs the container without managing EC2 instances.

| Setting | Value |
|---------|-------|
| CPU | 0.5 vCPU (512 units) |
| Memory | 1 GB (1024 units) |
| Task Role | Grants access to Secrets Manager secrets |
| Desired Count | 2 (for HA) |
| Auto-scaling | CPU-based, min=1, max=4 |

### 3. Application Load Balancer

- Terminates HTTPS via ACM certificate
- Forwards traffic to ECS tasks on port 8080
- Health check target: `/health`
- Idle timeout: 60 seconds

### 4. Amazon RDS (SQL Server)

See `DATABASE.md` for full setup.

### 5. AWS Secrets Manager

Secrets stored and referenced by name in the ECS task definition:

| Secret Name | Contains |
|-------------|----------|
| `genzcoders/db-connection` | SQL Server connection string |
| `genzcoders/google-oauth` | Google ClientId / ClientSecret |
| `genzcoders/zoom` | Zoom API credentials |
| `genzcoders/email` | SendGrid password, FromAddress |
| `genzcoders/paymob` | Paymob API key, HMAC secret |
| `genzcoders/fawry` | Fawry merchant code, security key |

### 6. Route 53 (Optional)

Point `api.genzacademy.com` (or your domain) to the ALB DNS name.

## Security

- All inbound traffic goes through ALB only (port 443)
- ECS tasks in private subnets (no public IPs)
- RDS in private subnet, accessible only from ECS security group
- Secrets never baked into the image — injected at runtime via Secrets Manager
- WAF (optional) in front of ALB for DDoS protection

## Deployment

See `CICD.md` for GitHub Actions workflow details.

## Local Testing

```bash
docker compose up --build
```

The API will be available at `http://localhost:8080`. Health check: `http://localhost:8080/health`.
