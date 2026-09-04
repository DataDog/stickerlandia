# Azure Front Door Standard (shared entry point for all services)
resource "azurerm_cdn_frontdoor_profile" "shared" {
  name                = "afd-stickerlandia-${var.env}"
  resource_group_name = azurerm_resource_group.shared.name
  sku_name            = "Standard_AzureFrontDoor"
  tags                = local.tags
}

# Shared Front Door Endpoint
resource "azurerm_cdn_frontdoor_endpoint" "shared" {
  name                     = "stickerlandia-api-${var.env}"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.shared.id
  tags                     = local.tags
}
