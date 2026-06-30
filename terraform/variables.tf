variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "eu-west-1"
}

variable "availability_zones" {
  description = "Availability zones"
  type        = list(string)
  default     = ["eu-west-1a", "eu-west-1b"]
}

variable "acm_certificate_arn" {
  description = "ARN of the ACM certificate for the ALB HTTPS listener"
  type        = string
}

variable "frontend_url" {
  description = "Frontend URL for CORS"
  type        = string
  default     = "https://genzacademy.vercel.app"
}

variable "ecr_repository_name" {
  description = "ECR repository name"
  type        = string
  default     = "genzcoders-backend"
}

variable "ecs_cluster_name" {
  description = "ECS cluster name"
  type        = string
  default     = "genzcoders-cluster"
}

variable "ecs_service_name" {
  description = "ECS service name"
  type        = string
  default     = "genzcoders-service"
}
