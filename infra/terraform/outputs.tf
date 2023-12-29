output "database_public_ip" {
  value = hcloud_server.database.ipv4_address
}

output "database_ip" {
  value = one([for i in flatten(hcloud_server.database.network) : i.ip])
}

output "frontend_public_ip" {
  value = hcloud_server.frontend.ipv4_address
}

output "frontend_ip" {
  value = one([for i in flatten(hcloud_server.frontend.network) : i.ip])
}

output "backend_public_ip" {
  value = hcloud_server.backend.ipv4_address
}

output "backend_ip" {
  value = one([for i in flatten(hcloud_server.backend.network) : i.ip])
}

output "database_volume_id" {
  value = hcloud_volume.database.id
}
