resource "azurerm_servicebus_namespace" "stickerlandia_users_service_bus" {
  name                = "stickerlandia-${var.env}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard"
  tags = {
    source = "terraform"
    env    = var.env
  }
}

resource "azurerm_servicebus_queue" "sticker_claimed_queue" {
  name         = "users.stickerClaimed.v1"
  namespace_id = azurerm_servicebus_namespace.stickerlandia_users_service_bus.id

  partitioning_enabled = true
}

resource "azurerm_servicebus_topic" "user_registered_topic" {
  name         = "users.userRegistered.v1"
  namespace_id = azurerm_servicebus_namespace.stickerlandia_users_service_bus.id

  partitioning_enabled = true
}