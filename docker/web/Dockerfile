# Frontend React compilado y servido por Caddy, que además da HTTPS y
# reenvía /api a la API. Se construye desde la raíz del repo.
FROM node:22-alpine AS build
WORKDIR /web
COPY MiUni.Web/package.json MiUni.Web/package-lock.json ./
RUN npm ci
COPY MiUni.Web/ ./
# Vacío: el navegador llama a /api en el mismo dominio y Caddy lo reenvía.
ENV VITE_API_URL=
RUN npm run build

FROM caddy:2-alpine
COPY docker/web/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /web/dist /srv
