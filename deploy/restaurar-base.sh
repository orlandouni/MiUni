#!/usr/bin/env bash
# Carga en la base de producción un respaldo de tu base local (lugares, caminos,
# documentos del chat con sus embeddings, perfiles de BuhoRater, cuentas...).
# Uso (en el servidor, desde la raíz del repo): bash deploy/restaurar-base.sh deploy/respaldos/miuni.dump
# Debe correr con la base recién creada, antes de aplicar migraciones.
set -euo pipefail
cd "$(dirname "$0")/.."

respaldo=${1:?Indica el archivo .dump, por ejemplo deploy/respaldos/miuni.dump}
[[ $respaldo == deploy/respaldos/* ]] || { echo "Pon el respaldo dentro de deploy/respaldos/." >&2; exit 1; }

compose=(docker compose -f docker-compose.prod.yml --env-file deploy/production.env)
"${compose[@]}" up -d --wait db

# pg_restore sigue aunque algo ya exista (por ejemplo el esquema public o las
# extensiones que crea docker/postgres/init); esos avisos son normales.
"${compose[@]}" exec -T db sh -c \
  'pg_restore --no-owner --no-privileges -U "$POSTGRES_USER" -d "$POSTGRES_DB" "/respaldos/$(basename "$1")"' \
  sh "$respaldo" || echo "pg_restore terminó con avisos; revisa arriba que no falte ninguna tabla."

# La cuenta de desarrollo tiene una contraseña pública en el README: se bloquea.
"${compose[@]}" exec -T db sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"' <<'SQL'
UPDATE "AspNetUsers" SET "LockoutEnabled" = true, "LockoutEnd" = '9999-12-31'
WHERE "NormalizedEmail" IN ('ADMIN.DEV@MIUNI.MX', 'ADMIN.DEV@UNISON.MX');
SQL

echo "Respaldo cargado."
