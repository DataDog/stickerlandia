# Shared Infrastructure Stack
# Deploys shared resources for integrated environments (dev, prod)

module "shared" {
  source = "./modules/shared-resources"

  env                             = var.env
  location                        = var.location
  dd_api_key                      = var.dd_api_key
  dd_site                         = var.dd_site
  vnet_address_space              = var.vnet_address_space
  container_apps_subnet_prefix    = var.container_apps_subnet_prefix
  private_endpoints_subnet_prefix = var.private_endpoints_subnet_prefix
  postgresql_subnet_prefix        = var.postgresql_subnet_prefix
  postgresql_admin_username       = var.postgresql_admin_username
}
