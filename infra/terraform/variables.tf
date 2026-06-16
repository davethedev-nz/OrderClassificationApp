variable "project_name" {
  description = "Prefix used for all Azure resources in this cram."
  type        = string
  default     = "orderclassification"
}

variable "location" {
  description = "Azure region for the deployment."
  type        = string
  default     = "australiaeast"
}

variable "environment" {
  description = "Environment name (dev/test/prod)."
  type        = string
  default     = "dev"
}

variable "tags" {
  description = "Common tags applied to all resources."
  type        = map(string)
  default = {
    owner   = "interview-cram"
    system  = "order-classification"
    managed = "terraform"
  }
}

variable "servicebus_topic_name" {
  description = "Service Bus topic for integration events."
  type        = string
  default     = "order-classification"
}

variable "servicebus_subscription_name" {
  description = "Service Bus subscription for the classification read model."
  type        = string
  default     = "classification-read-model"
}

