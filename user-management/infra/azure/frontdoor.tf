# Origin Group for User Management API
resource "azurerm_cdn_frontdoor_origin_group" "users_api" {
  count                    = var.deploy_container_apps ? 1 : 0
  name                     = "users-api-origin-group"
  cdn_frontdoor_profile_id = local.shared.frontdoor_profile_id
  session_affinity_enabled = false

  load_balancing {
    sample_size                 = 4
    successful_samples_required = 3
  }

  health_probe {
    path                = "/api/users/v1/health"
    request_type        = "GET"
    protocol            = "Https"
    interval_in_seconds = 30
  }
}

# Origin pointing to User Management Container App
resource "azurerm_cdn_frontdoor_origin" "users_api" {
  count                         = var.deploy_container_apps ? 1 : 0
  name                          = "users-api-origin"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.users_api[0].id
  enabled                       = true

  certificate_name_check_enabled = true
  host_name                      = azurerm_container_app.api[0].ingress[0].fqdn
  http_port                      = 80
  https_port                     = 443
  origin_host_header             = azurerm_container_app.api[0].ingress[0].fqdn
  priority                       = 1
  weight                         = 1000
}

# Route for User Management API traffic
resource "azurerm_cdn_frontdoor_route" "users_api" {
  count                         = var.deploy_container_apps ? 1 : 0
  name                          = "users-api-route"
  cdn_frontdoor_endpoint_id     = local.shared.frontdoor_endpoint_id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.users_api[0].id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.users_api[0].id]
  enabled                       = true

  forwarding_protocol    = "HttpsOnly"
  https_redirect_enabled = true
  patterns_to_match      = ["/api/users/*", "/auth/*", "/.well-known/*"]
  supported_protocols    = ["Http", "Https"]

  link_to_default_domain = true
}
