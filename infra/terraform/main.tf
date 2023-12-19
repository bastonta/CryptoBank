resource "hcloud_network" "network" {
  name     = "main_network"
  ip_range = "10.0.0.0/16"
}

resource "hcloud_network_subnet" "subnet" {
  type         = "cloud"
  network_id   = hcloud_network.network.id
  network_zone = "eu-central"
  ip_range     = "10.0.1.0/24"
}

resource "hcloud_server" "app_server" {
  name        = "app"
  server_type = "cx21"
  image       = "ubuntu-22.04"
  location    = "hel1"

  network {
    network_id = hcloud_network.network.id
    ip = "10.0.1.1"
  }

  labels = {
    purpose = "app"
  }
}
