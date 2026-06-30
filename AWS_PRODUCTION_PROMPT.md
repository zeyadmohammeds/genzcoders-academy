# Production Deployment Prompt — genzcoders-backend → Docker + AWS

Copy everything below into Claude Code (or your coding agent) while it has access to the `genzcoders-backend` repo.

---

## PROMPT START

I have an ASP.NET Core Web API backend (repo: `genzcoders-backend`, solution file `genzcoders.slnx`, SQL Server database) currently deployed manually on shared hosting (monster.asp.net). I need you to transform this into a production-grade, containerized system deployed on AWS, with full CI/CD, testing, and security hardening. Work through the phases below in order. After each phase, run the project locally (or via `dotnet build` / `docker build`) to confirm it works before moving to the next phase. Commit after each phase with a clear commit message.

### Context
- Tech stack: ASP.NET Core Web API, Entity Framework Core, SQL Server
- Current state: works locally / on shared hosting only, no Docker, no CI/CD, no automated tests, no secrets management
- Target: Dockerized API, deployed on AWS, with CI/CD via GitHub Actions, automated testing, and security hardening
- Frontend (separate repo `courses_frontend`, React + Vite + TS) stays on Vercel and will call this API via HTTPS — do not touch the frontend repo, but make sure CORS is configured for it

---

### PHASE 1 — Dockerize the backend

1. Inspect the solution structure (`genzcoders.slnx`, `global.json`, and the `genzcoders/` project folder) to find the actual Web API project (`.csproj`) and its target framework version.
2. Create a **multi-stage Dockerfile** at the repo root:
   - Stage 1 (`build`): use the correct `mcr.microsoft.com/dotnet/sdk:<version>` image matching `global.json`, restore and publish the project in Release mode.
   - Stage 2 (`runtime`): use `mcr.microsoft.com/dotnet/aspnet:<version>` (smaller image), copy only the published output, run as a **non-root user**, expose port 8080 (ASP.NET Core 8+ default), and set `ASPNETCORE_URLS=http://+:8080`.
3. Add a `.dockerignore` file excluding `bin/`, `obj/`, `.git/`, `*.user`, `appsettings.Development.json`.
4. Create a `docker-compose.yml` for **local development** that spins up:
   - The API container
   - A `mssql` (SQL Server) container with a persisted volume
   - Proper environment variable wiring between them (connection string via env var, not hardcoded)
5. Verify the connection string and all secrets (JWT signing key, SQL credentials, Zoom API keys if any, mail credentials) are read from environment variables / `appsettings.json` placeholders — NOT hardcoded. Update `Program.cs` / `Startup.cs` if needed so config is `IConfiguration`-driven.
6. Add a health check endpoint at `/health` (use `Microsoft.Extensions.Diagnostics.HealthChecks`, including a SQL Server check) — AWS load balancers and container orchestrators need this.
7. Build and run the container locally to confirm the API responds and connects to the dockerized SQL Server.

---

### PHASE 2 — Database: migrate off SQL Server shared hosting

1. Keep SQL Server as the engine (EF Core migrations stay valid) but move it to **Amazon RDS for SQL Server** (or, if cost is a concern, propose Amazon RDS for PostgreSQL with a migration plan — ask me which I prefer before doing this, since it changes EF Core providers).
2. Ensure all EF Core migrations are committed and reproducible (`dotnet ef migrations list`), and add a startup step (or a one-off CI job) that runs `dotnet ef database update` safely against the target environment — never auto-apply destructive migrations to production without a manual approval gate.
3. Document the RDS setup steps needed on the AWS side (instance class, storage, security group rules limiting inbound access to the ECS service only, automated backups enabled, multi-AZ optional) in a `docs/DATABASE.md` file — I will provision RDS manually or you can give me the Terraform/CDK for it (see Phase 5).

---

### PHASE 3 — Push to AWS (ECR + ECS Fargate)

1. Add a `docs/AWS_DEPLOYMENT.md` describing the target architecture:
   - **Amazon ECR** — container registry for the Docker image
   - **Amazon ECS on Fargate** — runs the container, no EC2 management needed
   - **Application Load Balancer (ALB)** — routes HTTPS traffic to the ECS service, terminates TLS using an **ACM certificate**
   - **Amazon RDS** — the database from Phase 2
   - **AWS Secrets Manager** — stores DB connection string, JWT secret, and any third-party API keys; ECS task definition pulls them at runtime (never baked into the image)
   - **Route 53** (optional) — custom domain pointing to the ALB
2. Write the **ECS task definition** (JSON) referencing the ECR image, with `secrets` block pulling from Secrets Manager, correct CPU/memory sizing for a small app (start with 0.5 vCPU / 1GB), and the `/health` endpoint wired as the container health check.
3. Provide **Infrastructure as Code** using AWS CDK (TypeScript or C#, your choice — C# CDK fits this stack) or Terraform, covering: ECR repo, ECS cluster + service + task definition, ALB + target group + listener (HTTPS), security groups (ALB → ECS on 8080 only, ECS → RDS on 1433 only), and Secrets Manager entries (values left as placeholders for me to fill in).
4. Do NOT hardcode AWS account IDs, regions, or credentials anywhere in committed code — use variables/parameters.

---

### PHASE 4 — CI/CD with GitHub Actions

Create `.github/workflows/ci.yml`:
- Triggers on every push and PR to `main`
- Steps: checkout → setup .NET → restore → build → run unit tests → run `dotnet format --verify-no-changes` (or equivalent lint check) → fail the build on any error

Create `.github/workflows/deploy.yml`:
- Triggers on push to `main` (after CI passes) or manual `workflow_dispatch`
- Steps: checkout → configure AWS credentials (via GitHub OIDC role assumption — **not** long-lived access keys, set up an IAM role trust policy for GitHub Actions) → build Docker image → push to ECR → update the ECS service (force new deployment) → wait for service stability → post a deployment summary
- Use environment protection rules (`production` environment in GitHub) requiring manual approval before the deploy job runs

Document in `docs/CICD.md` exactly which GitHub repo secrets/variables need to be set (AWS role ARN, ECR repo name, ECS cluster/service names, AWS region) — do not invent values, leave clear placeholders for me to fill in.

---

### PHASE 5 — Automated testing

1. Create a `genzcoders.Tests` xUnit project referencing the main API project.
2. Add **unit tests** for service-layer logic — prioritize `ApplicationService` (mentioned in `IMPLEMENTATION_SUMMARY.md` as having had a 500-error bug), `MaterialService`, `ZoomMeetingService`, `CourseRoundStudentService`, and the discount/pricing logic if it exists in this repo. Use Moq to mock `DbContext`/repositories.
3. Add **integration tests** using `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) against an in-memory or test SQL Server container (via `Testcontainers.MsSql` NuGet package) — covering at minimum: auth flow (login/JWT issuance), the Materials CRUD endpoints, the ZoomMeeting CRUD endpoints, and the CourseRoundStudent assignment flow described in `IMPLEMENTATION_SUMMARY.md`.
4. Wire these tests into the `ci.yml` workflow from Phase 4 so the build fails if tests fail.
5. Target meaningful coverage (don't chase 100% — prioritize auth, payment/discount logic, and enrollment flows since those are business-critical) and add a coverage report step (`coverlet` + upload as a build artifact).

---

### PHASE 6 — Security hardening

1. **HTTPS only**: enforce `UseHttpsRedirection()` and `UseHsts()` in production; confirm ALB-to-container traffic plan (TLS termination at ALB is fine, but document it).
2. **CORS**: lock down to only the known frontend origins (`https://genzacademy.vercel.app` and any preview/staging URLs I give you) — no wildcard `*` in production.
3. **Rate limiting**: add `Microsoft.AspNetCore.RateLimiting` middleware with sensible limits on auth endpoints (login, register) to prevent brute force/credential stuffing, and a global limiter for the rest of the API.
4. **Secrets**: confirm zero secrets exist in `appsettings.json`, `appsettings.Development.json`, or anywhere in git history. If any are found, flag them to me explicitly (I will need to rotate them) and add `git-secrets` or a pre-commit hook to prevent future leaks.
5. **JWT hardening**: confirm token expiry is reasonable (e.g., 15–60 min access token + refresh token flow if not already present), signing key comes from Secrets Manager, and validate `ValidateIssuer`/`ValidateAudience`/`ValidateLifetime` are all `true`.
6. **Input validation**: confirm all controller actions use model validation (`[ApiController]` + DataAnnotations or FluentValidation) and return proper 400s rather than letting bad input hit EF Core directly.
7. **Dependency scanning**: add a `dotnet list package --vulnerable` check (or GitHub Dependabot config) to the CI workflow so known-vulnerable NuGet packages are flagged automatically.
8. **Logging**: ensure no PII or secrets are logged; structured logging (e.g., Serilog) writing to console (so it's picked up by CloudWatch Logs via ECS automatically) rather than to local files.

---

### Deliverables checklist (confirm each at the end)

- [ ] `Dockerfile` + `.dockerignore` + `docker-compose.yml` (local dev works)
- [ ] `/health` endpoint live
- [ ] `docs/DATABASE.md` (RDS migration plan)
- [ ] `docs/AWS_DEPLOYMENT.md` + IaC (CDK or Terraform) for ECR/ECS/ALB/RDS/Secrets Manager
- [ ] `.github/workflows/ci.yml` (build + test on every PR)
- [ ] `.github/workflows/deploy.yml` (OIDC-based deploy to ECS on merge to main, with manual approval gate)
- [ ] `docs/CICD.md` listing required GitHub secrets/variables
- [ ] `genzcoders.Tests` project with unit + integration tests, wired into CI
- [ ] Security hardening items above all confirmed or flagged
- [ ] Updated root `README.md` with new local dev instructions (`docker-compose up`) and a link to the deployment docs

Work phase by phase. After each phase, summarize what you did, what you could NOT do without my input (AWS account access, secret values, domain name, etc.), and what you need from me before continuing.

## PROMPT END
