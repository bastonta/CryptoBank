#!/bin/bash

# Start terraform
terraform -chdir=terraform apply
terraform -chdir=terraform output | awk '{ gsub (" ", "", $0); print}' > .env

# Export environment variables
postgres_user=postgres
postgres_password=qwerty
export $(cat .env | xargs)

# Create ansible inventory
echo "" > ansible/inventory.ini
{
  echo "[frontend]"
  echo "$frontend_public_ip"
  echo ""

  echo "[frontend:vars]"
  echo "ansible_user=root"
  echo "backend_ip=$backend_ip"
  echo ""

  echo "[backend]"
  echo "$backend_public_ip"
  echo ""

  echo "[backend:vars]"
  echo "ansible_user=root"
  echo "database_ip=$database_ip"
  echo "postgres_user=$postgres_user"
  echo "postgres_password=$postgres_password"
  echo ""

  echo "[database]"
  echo "$database_public_ip"
  echo ""

  echo "[database:vars]"
  echo "ansible_user=root"
  echo "database_ip=$database_ip"
  echo "backend_ip=$backend_ip"
  echo "database_volume_id=$database_volume_id"
  echo "postgres_user=$postgres_user"
  echo "postgres_password=$postgres_password"
  echo ""
} >> ansible/inventory.ini

# Start ansible
ansible-playbook -i ansible/inventory.ini ansible/playbook.yml
