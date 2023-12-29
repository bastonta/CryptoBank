variable "hcloud_token" {
  type = string
  sensitive = true
}

variable "ssh_key_fingerprint" {
  type = string
  default = "7f:2a:46:41:ae:6e:a0:05:5a:60:d6:0a:81:c0:72:d1"
}
