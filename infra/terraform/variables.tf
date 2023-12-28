variable "hcloud_token" {
  type = string
  sensitive = true
}

variable "ssh_key_fingerprint" {
  type = string
  default = "7f:2a:46:41:ae:6e:a0:05:5a:60:d6:0a:81:c0:72:d1"
}

variable "frontend_ip" {
  type = string
  default = "10.0.1.1"
}

variable "backend_ip" {
  type = string
  default = "10.0.1.2"
}

variable "database_ip" {
  type = string
  default = "10.0.1.3"
}
