#!/usr/bin/env bash
# Crea deploy/production.env con claves aleatorias y el dominio gratuito de sslip.io.
# Pide la clave de Gemini y la del correo sin mostrarlas en pantalla.
# Uso (en el servidor, desde la raíz del repo): bash deploy/crear-env.sh
set -euo pipefail
cd "$(dirname "$0")/.."

destino=deploy/production.env
if [[ -f $destino ]]; then
  echo "$destino ya existe; no se sobrescribe (la contraseña de la base ya está en uso)." >&2
  exit 1
fi

# IP pública del EC2 (IMDSv2) -> 3-120-45-67.sslip.io
token=$(curl -sf -X PUT http://169.254.169.254/latest/api/token -H 'X-aws-ec2-metadata-token-ttl-seconds: 60')
ip=$(curl -sf -H "X-aws-ec2-metadata-token: $token" http://169.254.169.254/latest/meta-data/public-ipv4)
dominio="${ip//./-}.sslip.io"
read -rp "Dominio [$dominio]: " respuesta
dominio=${respuesta:-$dominio}

read -rsp "Clave de Gemini (Google AI Studio): " gemini; echo
read -rp "Correo Gmail que envía las verificaciones (vacío = sin correo): " smtp_usuario
smtp_contrasena=""
if [[ -n $smtp_usuario ]]; then
  read -rsp "Contraseña de aplicación de ese Gmail: " smtp_contrasena; echo
fi

umask 077
cat > "$destino" <<EOF
MIUNI_DOMAIN=$dominio
MIUNI_POSTGRES_DB=miuni
MIUNI_POSTGRES_USER=miuni
MIUNI_POSTGRES_PASSWORD=$(openssl rand -hex 32)
JWT_KEY=$(openssl rand -hex 32)
GEMINI_API_KEY=$gemini
MIUNI_RAG_PATH=../MiUni-RAG
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USUARIO=$smtp_usuario
SMTP_CONTRASENA=$smtp_contrasena
SMTP_REMITENTE=$smtp_usuario
EOF
echo "Se creó $destino (solo lo puede leer tu usuario). Dominio: https://$dominio"
