#!/bin/bash

# Start terraform
terraform -chdir=terraform apply
terraform -chdir=terraform output | awk '{ gsub (" ", "", $0); print}' > .env

# Export environment variables
export $(cat .env | xargs)

# Create ansible inventory
echo "[database]" > ansible/inventory.ini
echo "$database_public_ip" >> ansible/inventory.ini
echo "" >> ansible/inventory.ini
echo "[database:vars]" >> ansible/inventory.ini
echo "ansible_user=root" >> ansible/inventory.ini
echo "database_ip=$database_ip" >> ansible/inventory.ini
echo "backend_ip=$backend_ip" >> ansible/inventory.ini
echo "database_volume_id=$database_volume_id" >> ansible/inventory.ini

# Start ansible
ansible-playbook -i ansible/inventory.ini ansible/playbook.yml
