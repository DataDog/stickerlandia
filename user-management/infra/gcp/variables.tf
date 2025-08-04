variable "repository" {
  type = string
}

variable "worker_repository" {
  type = string
}

variable "image_tag" {
  type = string
}

variable "dd_api_key" {
  type    = string
  default = ""
}

variable "dd_site" {
  type    = string
  default = "datadoghq.com"
}

variable "db_connection_string" {
  type = string

}
