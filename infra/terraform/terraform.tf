terraform {
  required_version = ">= 0.15"
  required_providers {
    hcloud = {
      source = "hetznercloud/hcloud"
      version = "1.44.1"
    }
  }
}

provider "hcloud" {
  token = var.hcloud_token
}
