# Service Bus Namespace (shared - services create their own queues/topics)
resource "azurerm_servicebus_namespace" "shared" {
  name                = "sb-stickerlandia-${var.env}"
  location            = azurerm_resource_group.shared.location
  resource_group_name = azurerm_resource_group.shared.name
  sku                 = "Standard"
  tags                = local.tags
}
