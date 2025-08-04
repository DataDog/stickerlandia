resource "google_service_account" "cloudrun_service_identity" {
  account_id = "sticker-users-service-account"
}

resource "google_cloud_run_v2_service" "default" {
  name     = "stickerlandia-user-management"
  location = "europe-west2"
  ingress  = "INGRESS_TRAFFIC_ALL"

  template {
    revision                         = "stickerlandia-user-management-${var.image_tag}"
    service_account                  = google_service_account.cloudrun_service_identity.email
    max_instance_request_concurrency = 10
    volumes {
      name = "shared-volume"
      empty_dir {
        medium = "MEMORY"
      }
    }
    containers {
      image      = "${var.repository}:${var.image_tag}"
      depends_on = ["datadog"]
      volume_mounts {
        name       = "shared-volume"
        mount_path = "/shared-volume"
      }
      env {
        name  = "DD_API_KEY"
        value = var.dd_api_key
      }
      env {
        name  = "DD_TRACE_ENABLED"
        value = "true"
      }
      env {
        name  = "DD_SITE"
        value = var.dd_site
      }
      env {
        name  = "DD_TRACE_PROPAGATION_STYLE"
        value = "datadog"
      }
      env {
        name  = "GCLOUD_PROJECT_ID"
        value = data.google_project.project.project_id
      }
      env {
        name  = "DD_LOGS_ENABLED"
        value = "true"
      }
      env {
        name  = "ConnectionStrings__messaging"
        value = ""
      }
      env {
        name  = "ConnectionStrings__database"
        value = var.db_connection_string
      }
      env {
        name  = "DRIVING"
        value = "ASPNET"
      }
      env {
        name  = "DRIVEN"
        value = "GCP"
      }
      env {
        name  = "DISABLE_SSL"
        value = "true"
      }
    }
    containers {
      name  = "datadog"
      image = "gcr.io/datadoghq/serverless-init:latest"
      volume_mounts {
        name       = "shared-volume"
        mount_path = "/shared-volume"
      }
      env {
        name  = "DD_ENV"
        value = "dev"
      }
      env {
        name  = "DD_SERVERLESS_LOG_PATH"
        value = "shared-volume/logs/*.log"
      }
      env {
        name  = "DD_VERSION"
        value = var.image_tag
      }
      env {
        name  = "DD_SERVICE"
        value = "user-management"
      }
      env {
        name  = "DD_SITE"
        value = var.dd_site
      }
      env {
        name  = "DD_LOGS_ENABLED"
        value = "true"
      }
      env {
        name  = "DD_API_KEY"
        value = var.dd_api_key
      }
      env {
        name  = "GCLOUD_PROJECT_ID"
        value = data.google_project.project.project_id
      }
      env {
        name  = "DD_HEALTH_PORT"
        value = "12345"
      }
      startup_probe {
        initial_delay_seconds = 0
        timeout_seconds       = 1
        period_seconds        = 10
        failure_threshold     = 3
        tcp_socket {
          port = 12345
        }
      }
    }
  }

  traffic {
    type     = "TRAFFIC_TARGET_ALLOCATION_TYPE_REVISION"
    percent  = 100
    revision = "stickerlandia-user-management-${var.image_tag}"
    tag      = "live"
  }
}



resource "google_cloud_run_v2_service" "worker_service" {
  name     = "stickerlandia-user-management-worker"
  location = "europe-west2"
  ingress  = "INGRESS_TRAFFIC_ALL"

  template {
    revision                         = "stickerlandia-user-management-worker-${var.image_tag}"
    service_account                  = google_service_account.cloudrun_service_identity.email
    max_instance_request_concurrency = 10
    volumes {
      name = "shared-volume"
      empty_dir {
        medium = "MEMORY"
      }
    }
    scaling {
      min_instance_count = 1
    }
    containers {
      image      = "${var.worker_repository}:${var.image_tag}"
      depends_on = ["datadog"]
      volume_mounts {
        name       = "shared-volume"
        mount_path = "/shared-volume"
      }
      env {
        name  = "DD_API_KEY"
        value = var.dd_api_key
      }
      env {
        name  = "DD_TRACE_ENABLED"
        value = "true"
      }
      env {
        name  = "DD_SITE"
        value = var.dd_site
      }
      env {
        name  = "DD_TRACE_PROPAGATION_STYLE"
        value = "datadog"
      }
      env {
        name  = "GCLOUD_PROJECT_ID"
        value = data.google_project.project.project_id
      }
      env {
        name  = "DD_LOGS_ENABLED"
        value = "true"
      }
      env {
        name  = "ConnectionStrings__messaging"
        value = ""
      }
      env {
        name  = "ConnectionStrings__database"
        value = var.db_connection_string
      }
      env {
        name  = "DRIVING"
        value = "ASPNET"
      }
      env {
        name  = "DRIVEN"
        value = "GCP"
      }
      env {
        name  = "DISABLE_SSL"
        value = "true"
      }
    }
    containers {
      name  = "datadog"
      image = "gcr.io/datadoghq/serverless-init:latest"
      volume_mounts {
        name       = "shared-volume"
        mount_path = "/shared-volume"
      }
      env {
        name  = "DD_ENV"
        value = "dev"
      }
      env {
        name  = "DD_SERVERLESS_LOG_PATH"
        value = "shared-volume/logs/*.log"
      }
      env {
        name  = "DD_VERSION"
        value = var.image_tag
      }
      env {
        name  = "DD_SERVICE"
        value = "user-management"
      }
      env {
        name  = "DD_SITE"
        value = var.dd_site
      }
      env {
        name  = "DD_LOGS_ENABLED"
        value = "true"
      }
      env {
        name  = "DD_API_KEY"
        value = var.dd_api_key
      }
      env {
        name  = "GCLOUD_PROJECT_ID"
        value = data.google_project.project.project_id
      }
      env {
        name  = "DD_HEALTH_PORT"
        value = "12345"
      }
      startup_probe {
        initial_delay_seconds = 0
        timeout_seconds       = 1
        period_seconds        = 10
        failure_threshold     = 3
        tcp_socket {
          port = 12345
        }
      }
    }
  }

  traffic {
    type     = "TRAFFIC_TARGET_ALLOCATION_TYPE_REVISION"
    percent  = 100
    revision = "stickerlandia-user-management-worker-${var.image_tag}"
    tag      = "live"
  }
}

# resource "google_project_iam_member" "firestore-access" {
#   project = data.google_project.project.project_id
#   role    = "roles/datastore.user"
#   member  = "serviceAccount:${google_service_account.cloudrun_service_identity.email}"
# }

# resource "google_project_iam_member" "cloudtasks-access" {
#   project = data.google_project.project.project_id
#   role    = "roles/cloudtasks.enqueuer"
#   member  = "serviceAccount:${google_service_account.cloudrun_service_identity.email}"
# }

# resource "google_project_iam_member" "pubsub-access" {
#   project = data.google_project.project.project_id
#   role    = "roles/pubsub.publisher"
#   member  = "serviceAccount:${google_service_account.cloudrun_service_identity.email}"
# }
