# Database for the user-management service (within shared PostgreSQL server)
resource "azurerm_postgresql_flexible_server_database" "users" {
  name      = "stickerlandia_users"
  server_id = local.shared.postgresql_server_id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

# Store the connection string in shared Key Vault
resource "azurerm_key_vault_secret" "db_connection_string" {
  name         = "users-db-connection-string"
  value        = "Host=${local.shared.postgresql_server_fqdn};Port=5432;Database=${azurerm_postgresql_flexible_server_database.users.name};Username=${local.shared.postgresql_admin_username};Password=${local.shared.postgresql_admin_password};SSL Mode=Require;Trust Server Certificate=true"
  key_vault_id = local.shared.key_vault_id
  tags         = local.tags
}
