output "database_public_ip" {
  value = hcloud_server.database.ipv4_address
}

output "frontend_public_ip" {
  value = hcloud_server.frontend.ipv4_address
}

output "backend_public_ip" {
  value = hcloud_server.backend.ipv4_address
}

output "database_volume_id" {
  value = hcloud_volume.database.id
}
