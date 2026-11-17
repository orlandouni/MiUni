# Despliegue en AWS (demo de 24 horas)

Todo MiUni corre en **un solo servidor EC2** con Docker Compose (`docker-compose.prod.yml`):

| Servicio | Qué hace |
| --- | --- |
| `web` | Caddy: sirve el frontend compilado, saca el certificado HTTPS y reenvía `/api` a la API |
| `api` | Backend .NET (`docker/api/Dockerfile`) |
| `rag` | Chat (repo aparte [MiUni-RAG](https://github.com/angluisb/MiUni-RAG)), con el modelo BGE-M3 en CPU |
| `db` | PostgreSQL 16 con PostGIS, pgRouting y pgvector (la misma imagen que en desarrollo) |

Sin dominio propio se usa uno gratuito de [sslip.io](https://sslip.io): la IP `3.120.45.67`
se convierte en `https://3-120-45-67.sslip.io`, con certificado de Let's Encrypt.

## Costo

| Concepto | 24 horas |
| --- | ---: |
| EC2 `t4g.large` (2 vCPU ARM, 8 GB) | ~1.60 USD |
| Disco gp3 de 30 GB | ~0.08 USD |
| IP pública IPv4 | ~0.12 USD |
| **Total** | **~1.80 USD** |

Se necesitan 8 GB porque el modelo del chat ocupa unos 3 GB en memoria. Si tu cuenta tiene
créditos (Billing and Cost Management → Credits), se descuentan de ahí.

**Lo único que puede salir caro es olvidar apagarlo** (unos 50 USD al mes). Ver el último paso.

## 1. Preparar la cuenta (una sola vez)

1. Entra a la consola como usuario raíz y activa MFA: menú de tu nombre (arriba a la derecha) →
   **Security credentials** → **Assign MFA device**.
2. Crea una alerta de gasto: busca **Budgets** → **Create budget** → **Use a template** →
   **Zero spend budget** o **Monthly cost budget** de 5 USD, con tu correo.
3. Arriba a la derecha elige la región **US West (Oregon) us-west-2**. Todo lo que sigue se hace en esa región.

## 2. Crear el servidor

1. Busca **EC2** → **Launch instance**.
2. **Name**: `miuni`.
3. **Application and OS Images**: Ubuntu → **Ubuntu Server 24.04 LTS**, arquitectura **64-bit (Arm)**.
4. **Instance type**: `t4g.large`.
5. **Key pair**: **Create new key pair** → nombre `miuni`, tipo ED25519, formato `.pem`.
   Se descarga `miuni.pem`; guárdalo en `C:\Users\<tu usuario>\.ssh\`. No lo compartas.
6. **Network settings**: marca **Allow SSH traffic from → My IP**, **Allow HTTPS traffic from the internet**
   y **Allow HTTP traffic from the internet** (Caddy usa el puerto 80 para sacar el certificado).
7. **Configure storage**: `30` GiB, `gp3`.
8. **Launch instance**. En la lista de instancias copia la **Public IPv4 address**.

No detengas (Stop) la instancia: al volver a encenderla cambia la IP y con ella el dominio.
Reiniciarla (Reboot) sí está bien.

## 3. Entrar por SSH desde tu PC (PowerShell)

```powershell
# Una sola vez: OpenSSH exige que solo tu usuario pueda leer la llave.
icacls $HOME\.ssh\miuni.pem /inheritance:r /grant:r "$($env:USERNAME):R"

ssh -i $HOME\.ssh\miuni.pem ubuntu@<IP-PÚBLICA>
```

## 4. Instalar Docker y bajar el código (en el servidor)

```bash
git clone https://github.com/orlandouni/MiUni.git
git clone https://github.com/angluisb/MiUni-RAG.git
cd MiUni
bash deploy/instalar-servidor.sh
exit
```

Vuelve a entrar con el mismo `ssh` (así tu usuario ya puede usar Docker) y crea la configuración:

```bash
cd MiUni
bash deploy/crear-env.sh
```

El script propone el dominio `<ip-con-guiones>.sslip.io`, genera la contraseña de la base y la
clave JWT, y te pide sin mostrarlas en pantalla:

- La clave de Gemini (la misma del `.env` de MiUni-RAG en tu PC, o una nueva en
  [Google AI Studio](https://aistudio.google.com/apikey)).
- Un Gmail y su [contraseña de aplicación](https://myaccount.google.com/apppasswords) para los
  correos de verificación. Puedes dejarlo vacío y confirmar cuentas a mano (paso 8).

Todo queda en `deploy/production.env`, que Git ignora. No lo pegues en ningún chat.

## 5. Compilar las imágenes

```bash
docker compose -f docker-compose.prod.yml --env-file deploy/production.env build
```

Tarda unos 10–15 minutos la primera vez (sobre todo PyTorch para el chat).

## 6. Llevar tus datos (opcional, recomendado)

Así producción tiene tus lugares, caminos, perfiles de BuhoRater y los documentos del chat
ya indexados (indexarlos otra vez en el servidor tarda 3–4 horas). Usa la base de
**Docker** de tu PC, que es la que tiene pgvector y los embeddings.

En tu PC, desde la carpeta de MiUni (PowerShell):

```powershell
docker compose exec db sh -c 'pg_dump -U $POSTGRES_USER -d $POSTGRES_DB -Fc -f /tmp/miuni.dump'
docker compose cp db:/tmp/miuni.dump .\miuni.dump
ssh -i $HOME\.ssh\miuni.pem ubuntu@<IP-PÚBLICA> "mkdir -p MiUni/deploy/respaldos"
scp -i $HOME\.ssh\miuni.pem .\miuni.dump ubuntu@<IP-PÚBLICA>:MiUni/deploy/respaldos/
```

En el servidor:

```bash
bash deploy/restaurar-base.sh deploy/respaldos/miuni.dump
```

El script bloquea la cuenta `admin.dev`, porque su contraseña está publicada en el README.
`miuni.dump` trae las cuentas de tu base local: no lo subas a Git.

## 7. Migrar y arrancar

```bash
alias miuni='docker compose -f docker-compose.prod.yml --env-file deploy/production.env'
miuni up -d --wait db
miuni run --rm api --migrar true
miuni up -d
miuni ps
miuni logs --tail 30 web   # debe decir "certificate obtained successfully"
```

Abre `https://<ip-con-guiones>.sslip.io`.

El alias dura solo esa sesión SSH; para que se quede, agrégalo a `~/.bashrc`.

## 8. Cuenta de administrador

Regístrate en la página con tu correo. Si no configuraste Gmail, confirma el correo a mano:

```bash
miuni exec -T db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"' <<'SQL'
UPDATE "AspNetUsers" SET "EmailConfirmed" = true WHERE "NormalizedEmail" = upper('tu@correo.com');
SQL
```

Luego dale el rol de administrador:

```bash
miuni run --rm api --asignar-admin tu@correo.com
```

## 9. Calentar el chat antes de la presentación

La primera pregunta descarga el modelo BGE-M3 (~2 GB) y tarda unos minutos. Haz una pregunta
de prueba en el chat apenas arranque. Si falla, revisa `miuni logs --tail 50 rag`.

## 10. Apagar todo al terminar

1. EC2 → **Instances** → selecciona `miuni` → **Instance state** → **Terminate (delete) instance**.
   Esto borra también el disco (queda en *Volumes* solo si cambiaste esa opción al crearla).
2. EC2 → **Volumes**: confirma que no quedó ninguno `available`.
3. Al día siguiente revisa **Billing and Cost Management → Bills**.

Si quieres guardar la base antes de terminar, en el servidor:
`miuni exec db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /respaldos/final.dump'`
y bájala con `scp -i $HOME\.ssh\miuni.pem ubuntu@<IP-PÚBLICA>:MiUni/deploy/respaldos/final.dump .`
