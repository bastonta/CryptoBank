#!/bin/bash

# Start terraform
terraform -chdir=terraform apply
terraform -chdir=terraform output | awk '{ gsub (" ", "", $0); print}' > .env

# Export environment variables
postgres_database=cryptobank
postgres_user=cryptobank
postgres_password=qwerty
export $(cat .env | xargs)

# Create ansible inventory
echo "" > ansible/inventory.ini
{
  echo "[all:vars]"
  echo "ansible_user=root"
  echo "ansible_ssh_common_args='-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null'"
  echo ""

  echo "[frontend]"
  echo "$frontend_public_ip"
  echo ""

  echo "[frontend:vars]"
  echo "backend_ip=$backend_ip"
  echo ""

  echo "[backend]"
  echo "$backend_public_ip"
  echo ""

  echo "[backend:vars]"
  echo "database_ip=$database_ip"
  echo "postgres_database=$postgres_database"
  echo "postgres_user=$postgres_user"
  echo "postgres_password=$postgres_password"
  echo ""

  echo "[database]"
  echo "$database_public_ip"
  echo ""

  echo "[database:vars]"
  echo "database_ip=$database_ip"
  echo "backend_ip=$backend_ip"
  echo "database_volume_id=$database_volume_id"
  echo "postgres_database=$postgres_database"
  echo "postgres_user=$postgres_user"
  echo "postgres_password=$postgres_password"
  echo ""
} >> ansible/inventory.ini

# Start ansible
ansible-playbook -i ansible/inventory.ini ansible/playbook.yml
