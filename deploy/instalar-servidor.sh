#!/usr/bin/env bash
# Prepara un EC2 nuevo con Ubuntu 24.04: Docker, Compose, Git y 4 GB de swap.
# Uso (en el servidor): bash deploy/instalar-servidor.sh   y luego cerrar y abrir la sesión SSH.
set -euo pipefail

sudo apt-get update
sudo apt-get install -y docker.io docker-compose-v2 docker-buildx git openssl
sudo systemctl enable --now docker
sudo usermod -aG docker "$USER"

# Swap: compilar la API y cargar el modelo del chat a la vez puede pasar de 8 GB.
if ! swapon --show | grep -q /swapfile; then
  sudo fallocate -l 4G /swapfile
  sudo chmod 600 /swapfile
  sudo mkswap /swapfile
  sudo swapon /swapfile
  echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
fi

echo "Listo. Cierra la sesión SSH y vuelve a entrar para usar docker sin sudo."
