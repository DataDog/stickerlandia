# Service-specific Service Bus Queue
resource "azurerm_servicebus_queue" "sticker_claimed" {
  name         = "users.stickerClaimed.v1"
  namespace_id = local.shared.servicebus_namespace_id

  partitioning_enabled = true
}

# Service-specific Service Bus Topic
resource "azurerm_servicebus_topic" "user_registered" {
  name         = "users.userRegistered.v1"
  namespace_id = local.shared.servicebus_namespace_id

  partitioning_enabled = true
}

# Store Service Bus connection string in shared Key Vault
resource "azurerm_key_vault_secret" "servicebus_connection_string" {
  name         = "users-servicebus-connection-string"
  value        = local.shared.servicebus_connection_string
  key_vault_id = local.shared.key_vault_id
  tags         = local.tags
}
