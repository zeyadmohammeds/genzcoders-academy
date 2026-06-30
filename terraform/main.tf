terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# ─── VPC ─────────────────────────────────────────────────────────────────────
resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
  enable_dns_support   = true
  tags                 = { Name = "genzcoders-vpc" }
}

resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id
  tags   = { Name = "genzcoders-igw" }
}

# Public subnets (ALB)
resource "aws_subnet" "public" {
  count                   = 2
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.${count.index}.0/24"
  availability_zone       = var.availability_zones[count.index]
  map_public_ip_on_launch = true
  tags                    = { Name = "genzcoders-public-${count.index}" }
}

resource "aws_eip" "nat" {
  domain = "vpc"
  tags   = { Name = "genzcoders-nat-eip" }
}

resource "aws_nat_gateway" "main" {
  allocation_id = aws_eip.nat.id
  subnet_id     = aws_subnet.public[0].id
  tags          = { Name = "genzcoders-nat" }
}

# Private subnets (ECS, RDS)
resource "aws_subnet" "private" {
  count             = 2
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.${count.index + 10}.0/24"
  availability_zone = var.availability_zones[count.index]
  tags              = { Name = "genzcoders-private-${count.index}" }
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }
  tags = { Name = "genzcoders-public-rt" }
}

resource "aws_route_table_association" "public" {
  count          = 2
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table" "private" {
  vpc_id = aws_vpc.main.id
  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.main.id
  }
  tags = { Name = "genzcoders-private-rt" }
}

resource "aws_route_table_association" "private" {
  count          = 2
  subnet_id      = aws_subnet.private[count.index].id
  route_table_id = aws_route_table.private.id
}

# ─── Security Groups ──────────────────────────────────────────────────────────
resource "aws_security_group" "alb" {
  name        = "genzcoders-alb-sg"
  description = "Allow HTTPS inbound"
  vpc_id      = aws_vpc.main.id

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "genzcoders-alb-sg" }
}

resource "aws_security_group" "ecs" {
  name        = "genzcoders-ecs-sg"
  description = "Allow ingress from ALB only"
  vpc_id      = aws_vpc.main.id

  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = { Name = "genzcoders-ecs-sg" }
}

resource "aws_security_group" "rds" {
  name        = "genzcoders-rds-sg"
  description = "Allow ingress from ECS tasks only"
  vpc_id      = aws_vpc.main.id

  ingress {
    from_port       = 1433
    to_port         = 1433
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs.id]
  }

  tags = { Name = "genzcoders-rds-sg" }
}

# ─── RDS ──────────────────────────────────────────────────────────────────────
resource "aws_db_subnet_group" "main" {
  name       = "genzcoders-db-subnet"
  subnet_ids = aws_subnet.private[*].id
  tags       = { Name = "genzcoders-db-subnet" }
}

resource "aws_db_instance" "main" {
  identifier             = "genzcoders-db"
  engine                 = "sqlserver-ex"
  engine_version         = "16.00"
  instance_class         = "db.t3.small"
  allocated_storage      = 20
  storage_encrypted      = true
  db_name                = "GenZCoders"
  username               = "admin"
  password               = random_password.rds.result
  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  backup_retention_period = 7
  backup_window          = "03:00-04:00"
  maintenance_window     = "sun:04:00-sun:05:00"
  skip_final_snapshot    = false
  final_snapshot_identifier = "genzcoders-db-final-${formatdate("YYYY-MM-DD-hhmm", timestamp())}"
  tags                   = { Name = "genzcoders-db" }
}

resource "random_password" "rds" {
  length  = 16
  special = false
}

# ─── Secrets Manager ──────────────────────────────────────────────────────────
resource "aws_secretsmanager_secret" "db_connection" {
  name        = "genzcoders/db-connection"
  description = "SQL Server connection string for genzcoders"
}

resource "aws_secretsmanager_secret_version" "db_connection" {
  secret_id = aws_secretsmanager_secret.db_connection.id
  secret_string = jsonencode({
    connectionString = "Server=${aws_db_instance.main.address},1433;Database=GenZCoders;User Id=admin;Password=${random_password.rds.result};TrustServerCertificate=True;Encrypt=True;"
  })
}

resource "aws_secretsmanager_secret" "google_oauth" {
  name        = "genzcoders/google-oauth"
  description = "Google OAuth credentials"
}

resource "aws_secretsmanager_secret" "zoom" {
  name        = "genzcoders/zoom"
  description = "Zoom API credentials"
}

resource "aws_secretsmanager_secret" "email" {
  name        = "genzcoders/email"
  description = "Email (SendGrid) credentials"
}

resource "aws_secretsmanager_secret" "paymob" {
  name        = "genzcoders/paymob"
  description = "Paymob payment credentials"
}

resource "aws_secretsmanager_secret" "fawry" {
  name        = "genzcoders/fawry"
  description = "Fawry payment credentials"
}

# ─── ALB ──────────────────────────────────────────────────────────────────────
resource "aws_lb" "main" {
  name               = "genzcoders-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = aws_subnet.public[*].id
  tags               = { Name = "genzcoders-alb" }
}

resource "aws_lb_target_group" "api" {
  name        = "genzcoders-api-tg"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "ip"

  health_check {
    path                = "/health"
    interval            = 30
    timeout             = 10
    healthy_threshold   = 2
    unhealthy_threshold = 3
    matcher             = "200-399"
  }

  tags = { Name = "genzcoders-api-tg" }
}

resource "aws_lb_listener" "https" {
  load_balancer_arn = aws_lb.main.arn
  port              = 443
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-TLS13-1-2-2021-06"
  certificate_arn   = var.acm_certificate_arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.api.arn
  }
}

resource "aws_lb_listener" "http_redirect" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type = "redirect"
    redirect {
      port        = "443"
      protocol    = "HTTPS"
      status_code = "HTTP_301"
    }
  }
}

# ─── ECR ──────────────────────────────────────────────────────────────────────
resource "aws_ecr_repository" "main" {
  name                 = var.ecr_repository_name
  image_tag_mutability = "MUTABLE"
  image_scanning_configuration {
    scan_on_push = true
  }
  tags = { Name = var.ecr_repository_name }
}

# ─── ECS ──────────────────────────────────────────────────────────────────────
resource "aws_ecs_cluster" "main" {
  name = var.ecs_cluster_name
  setting {
    name  = "containerInsights"
    value = "enabled"
  }
  tags = { Name = var.ecs_cluster_name }
}

resource "aws_ecs_task_definition" "api" {
  family                   = "genzcoders-api"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "512"
  memory                   = "1024"
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([
    {
      name  = "api"
      image = "${aws_ecr_repository.main.repository_url}:latest"
      portMappings = [
        {
          containerPort = 8080
          protocol      = "tcp"
        }
      ]
      environment = [
        { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
        { name = "ASPNETCORE_URLS", value = "http://+:8080" },
        { name = "Cors__AllowedOrigins__0", value = var.frontend_url },
      ]
      secrets = [
        { name = "ConnectionStrings__DefaultConnection", valueFrom = aws_secretsmanager_secret.db_connection.arn },
        { name = "Authentication__Google__ClientId", valueFrom = "${aws_secretsmanager_secret.google_oauth.arn}:ClientId::" },
        { name = "Authentication__Google__ClientSecret", valueFrom = "${aws_secretsmanager_secret.google_oauth.arn}:ClientSecret::" },
        { name = "Zoom__AccountId", valueFrom = "${aws_secretsmanager_secret.zoom.arn}:AccountId::" },
        { name = "Zoom__ClientId", valueFrom = "${aws_secretsmanager_secret.zoom.arn}:ClientId::" },
        { name = "Zoom__ClientSecret", valueFrom = "${aws_secretsmanager_secret.zoom.arn}:ClientSecret::" },
        { name = "Email__Password", valueFrom = "${aws_secretsmanager_secret.email.arn}:Password::" },
        { name = "Email__FromAddress", valueFrom = "${aws_secretsmanager_secret.email.arn}:FromAddress::" },
        { name = "Paymob__ApiKey", valueFrom = "${aws_secretsmanager_secret.paymob.arn}:ApiKey::" },
        { name = "Paymob__HmacSecret", valueFrom = "${aws_secretsmanager_secret.paymob.arn}:HmacSecret::" },
        { name = "Fawry__MerchantCode", valueFrom = "${aws_secretsmanager_secret.fawry.arn}:MerchantCode::" },
        { name = "Fawry__SecurityKey", valueFrom = "${aws_secretsmanager_secret.fawry.arn}:SecurityKey::" },
      ]
      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "/ecs/genzcoders-api"
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "api"
        }
      }
      healthCheck = {
        command     = ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
        interval    = 30
        timeout     = 10
        retries     = 3
        startPeriod = 30
      }
    }
  ])

  tags = { Name = "genzcoders-api-task" }
}

resource "aws_ecs_service" "api" {
  name            = var.ecs_service_name
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets         = aws_subnet.private[*].id
    security_groups = [aws_security_group.ecs.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api.arn
    container_name   = "api"
    container_port   = 8080
  }

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }

  depends_on = [aws_lb_listener.https]
  tags       = { Name = var.ecs_service_name }
}

# ─── IAM ──────────────────────────────────────────────────────────────────────
resource "aws_iam_role" "ecs_execution" {
  name = "genzcoders-ecs-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_execution" {
  role       = aws_iam_role.ecs_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role_policy" "ecs_execution_secrets" {
  name = "genzcoders-ecs-secrets-access"
  role = aws_iam_role.ecs_execution.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue",
          "kms:Decrypt"
        ]
        Resource = [
          aws_secretsmanager_secret.db_connection.arn,
          aws_secretsmanager_secret.google_oauth.arn,
          aws_secretsmanager_secret.zoom.arn,
          aws_secretsmanager_secret.email.arn,
          aws_secretsmanager_secret.paymob.arn,
          aws_secretsmanager_secret.fawry.arn,
        ]
      },
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogStream",
          "logs:PutLogEvents"
        ]
        Resource = "*"
      }
    ]
  })
}

resource "aws_iam_role" "ecs_task" {
  name = "genzcoders-ecs-task-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })
}

# ─── CloudWatch ───────────────────────────────────────────────────────────────
resource "aws_cloudwatch_log_group" "api" {
  name              = "/ecs/genzcoders-api"
  retention_in_days = 30
  tags              = { Name = "genzcoders-api-logs" }
}
