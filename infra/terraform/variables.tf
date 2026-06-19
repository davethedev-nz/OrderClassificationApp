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

variable "sql_admin_username" {
  type    = string
  default = "sqladminuser"
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}

variable "entra_admin_login_name" {
  type        = string
  description = "Name of the Entra user or group that becomes SQL admin."
}

variable "entra_admin_object_id" {
  type        = string
  description = "Object ID of the Entra user or group that becomes SQL admin."
}

