# Shared Container App Environment
# All services in this environment can communicate internally via:
# http://<app-name>.internal.<environment-unique-id>.<region>.azurecontainerapps.io
resource "azurerm_container_app_environment" "shared" {
  name                           = "cae-stickerlandia-${var.env}"
  location                       = azurerm_resource_group.shared.location
  resource_group_name            = azurerm_resource_group.shared.name
  log_analytics_workspace_id     = azurerm_log_analytics_workspace.main.id
  infrastructure_subnet_id       = azurerm_subnet.container_apps.id
  internal_load_balancer_enabled = false
  tags                           = local.tags
}
