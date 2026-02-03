# Log Analytics Workspace (required by Container Apps)
resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-stickerlandia-${var.env}"
  location            = azurerm_resource_group.shared.location
  resource_group_name = azurerm_resource_group.shared.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.tags
}
