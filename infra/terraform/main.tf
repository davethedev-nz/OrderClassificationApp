locals {
  resource_prefix = "oc${var.environment}"
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = var.location
  tags     = var.tags
}

resource "azurerm_servicebus_namespace" "main" {
  name                = "sb${var.project_name}${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_servicebus_topic" "orders" {
  name         = var.servicebus_topic_name
  namespace_id = azurerm_servicebus_namespace.main.id
}

resource "azurerm_servicebus_subscription" "classification_read_model" {
  name               = var.servicebus_subscription_name
  topic_id           = azurerm_servicebus_topic.orders.id
  max_delivery_count = 10
}

resource "azurerm_servicebus_namespace_authorization_rule" "app" {
  name         = "app-access"
  namespace_id = azurerm_servicebus_namespace.main.id

  listen = true
  send   = true
  manage = false
}

resource "azurerm_key_vault" "main" {
  name                       = "kv-${local.resource_prefix}1"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  purge_protection_enabled   = false
  soft_delete_retention_days = 7
  tags                       = var.tags
}

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault_access_policy" "current_user" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
    "Recover",
    "Purge"
  ]
}

resource "azurerm_application_insights" "main" {
  name                = "appi-${local.resource_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  tags                = var.tags
}

resource "azurerm_service_plan" "main" {
  name                = "asp-${local.resource_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = "B1"
  tags                = var.tags
}

resource "azurerm_linux_web_app" "api" {
  name                = "app-${local.resource_prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true
  tags                = var.tags

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on = true
    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                        = var.environment
    "ConnectionStrings__OrderClassification"        = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.orderclassification_connection_string.versionless_id})"
    "Messaging__Provider"                           = "ServiceBus"
    "Messaging__ServiceBus__ConnectionString"       = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.servicebus_connection_string.versionless_id})"
    "Messaging__ServiceBus__TopicName"              = azurerm_servicebus_topic.orders.name
    "Messaging__ServiceBus__SubscriptionName"       = azurerm_servicebus_subscription.classification_read_model.name
    "Messaging__ServiceBus__OrderClassifiedSubject" = "orders.classified"
    "APPLICATIONINSIGHTS_CONNECTION_STRING"         = azurerm_application_insights.main.connection_string
  }
}

resource "azurerm_key_vault_access_policy" "web_app" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_web_app.api.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

resource "azurerm_key_vault_secret" "orderclassification_connection_string" {
  name         = "OrderClassification--ConnectionString"
  value        = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = azurerm_key_vault.main.id
  depends_on   = [azurerm_key_vault_access_policy.current_user]
}

resource "azurerm_key_vault_secret" "servicebus_connection_string" {
  name         = "OrderClassification--ServiceBusConnectionString"
  value        = azurerm_servicebus_namespace_authorization_rule.app.primary_connection_string
  key_vault_id = azurerm_key_vault.main.id
  depends_on   = [azurerm_key_vault_access_policy.current_user]
}

resource "azurerm_user_assigned_identity" "sql_server_identity" {
  name                = "id-sql-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
}
resource "azurerm_mssql_server" "main" {
    name                         = "sql-${local.resource_prefix}"
    resource_group_name          = azurerm_resource_group.main.name
    location                     = azurerm_resource_group.main.location
    version                      = "12.0"
    administrator_login          = var.sql_admin_username
    administrator_login_password = var.sql_admin_password
    tags                         = var.tags
  
    identity {
      type         = "UserAssigned"
      identity_ids = [azurerm_user_assigned_identity.sql_server_identity.id]
    }

    primary_user_assigned_identity_id = azurerm_user_assigned_identity.sql_server_identity.id

    azuread_administrator {
      login_username              = var.entra_admin_login_name
      object_id                   = var.entra_admin_object_id
      azuread_authentication_only = true
    }
}

resource "azurerm_mssql_database" "main" {
  name      = "sqldb-${var.project_name}-${var.environment}"
  server_id = azurerm_mssql_server.main.id
  sku_name  = "Basic"

  lifecycle {
    prevent_destroy = true
  }
}

