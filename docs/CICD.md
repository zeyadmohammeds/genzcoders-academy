# CI/CD Pipeline — GitHub Actions

## Workflows

### 1. CI (`ci.yml`)

Triggers on every push and PR to `main`.

- Checkout
- Setup .NET 10
- Restore dependencies
- Build
- Run unit + integration tests
- Lint (`dotnet format --verify-no-changes`)
- Report test coverage

### 2. Deploy (`deploy.yml`)

Triggers on push to `main` (after CI passes) or manual `workflow_dispatch`.

- Configure AWS credentials via OIDC (no long-lived keys)
- Build and tag Docker image
- Push to ECR
- Force new ECS deployment
- Wait for service stability
- Post deployment summary

## Required GitHub Secrets

| Secret Name | Description |
|-------------|-------------|
| `AWS_ROLE_ARN` | IAM role ARN for GitHub Actions OIDC (see below) |
| `AWS_REGION` | AWS region (e.g., `eu-west-1`) |
| `ECR_REPOSITORY` | ECR repository name (e.g., `genzcoders-backend`) |
| `ECS_CLUSTER` | ECS cluster name (e.g., `genzcoders-cluster`) |
| `ECS_SERVICE` | ECS service name (e.g., `genzcoders-service`) |

## OIDC Setup (One-Time)

1. In AWS IAM, create an **Identity Provider** for GitHub Actions:
   - Provider URL: `https://token.actions.githubusercontent.com`
   - Audience: `sts.amazonaws.com`

2. Create an IAM role with the following trust policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::<ACCOUNT_ID>:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:<GITHUB_ORG>/<REPO>:*"
        }
      }
    }
  ]
}
```

3. Attach these **permissions** to the role:
   - `AmazonEC2ContainerRegistryPowerUser`
   - `AmazonECS_FullAccess`
   - `SecretsManagerReadWrite` (if managing secrets)

## Environment Protection

The deploy workflow uses a `production` GitHub environment with **required reviewers** — no direct-to-production pushes without approval.

## Verification

After deployment:
1. Check the ECS service status in AWS Console → "Stable"
2. Hit the `/health` endpoint on the ALB DNS name
3. Check CloudWatch Logs for any startup errors
