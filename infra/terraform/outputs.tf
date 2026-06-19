output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "servicebus_namespace_name" {
  value = azurerm_servicebus_namespace.main.name
}

output "servicebus_topic_name" {
  value = azurerm_servicebus_topic.orders.name
}

output "web_app_name" {
  value = azurerm_linux_web_app.api.name
}

output "sql_server_fqdn" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_name" {
  value = azurerm_mssql_database.main.name
}

