# Azure Container Registry for local builds
# Only created when create_acr=true

resource "azurerm_container_registry" "acr" {
  count               = var.create_acr ? 1 : 0
  name                = "acrstickerlandia${replace(var.env, "-", "")}"
  resource_group_name = local.shared.resource_group_name
  location            = local.shared.resource_group_location
  sku                 = "Basic"
  admin_enabled       = false
  tags                = local.tags
}

# Grant the app identity permission to pull images from ACR
resource "azurerm_role_assignment" "acr_pull" {
  count                = var.create_acr ? 1 : 0
  scope                = azurerm_container_registry.acr[0].id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}
