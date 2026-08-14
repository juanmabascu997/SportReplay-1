# SportReplay

Plataforma SaaS para clubes deportivos: canchas, camaras RTSP/ONVIF, grabacion de partidos, clips, Mercado Pago y envio por WhatsApp.

## 1. Instalar Docker

1. Instala [Docker Desktop](https://www.docker.com/products/docker-desktop/).
2. Confirma:

```bash
docker --version
docker compose version
```

## 2. Levantar PostgreSQL

Desde la raiz del repositorio:

```bash
docker compose up -d postgres
```

O desde `infrastructure/`:

```bash
docker compose up -d postgres
```

Datos de desarrollo:

- Database: `sportreplay`
- User: `sportreplay`
- Password: `sportreplay_dev`
- Port: `5432`

## 3. Levantar MinIO

```bash
docker compose up -d minio minio-init
```

- API: http://localhost:9000
- Console: http://localhost:9001
- User/Password: `minioadmin` / `minioadmin`
- Bucket: `sportreplay-videos` (se crea automaticamente)

## 4. Ejecutar Backend

Copia `.env.example` a `.env` y ajusta secretos.

```bash
cd src/backend/SportReplay.Api
dotnet run
```

API: http://localhost:5080  
Swagger: http://localhost:5080/swagger  
Health: http://localhost:5080/health  
Ready: http://localhost:5080/health/ready

En Development las migraciones y el seed se ejecutan al iniciar.

Usuario admin:

- email: `admin@sportreplay.local`
- password: `Admin123!`

Otros usuarios seed: `owner@sportreplay.local` / `Owner123!` y `player@sportreplay.local` / `Player123!`

## 5. Ejecutar Frontend

Requiere Node.js 18 o superior (el Dockerfile usa Node 20).

```bash
cd src/frontend/sport-replay-web
npm install
npm run dev
```

UI: http://localhost:5173

## 6. Ejecutar migrations

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/backend/SportReplay.Infrastructure --startup-project src/backend/SportReplay.Api
dotnet ef database update --project src/backend/SportReplay.Infrastructure --startup-project src/backend/SportReplay.Api
```

En desarrollo `Program.cs` ya corre `MigrateAsync()` al arrancar.

## 7. Configurar Mercado Pago

1. Crea una aplicacion en https://www.mercadopago.com/developers
2. Copia Access Token (sandbox o prod) a `MERCADOPAGO_ACCESS_TOKEN`
3. Configura webhook `POST /api/payments/webhook`
4. Copia el secreto a `MERCADOPAGO_WEBHOOK_SECRET`
5. Sin token, el backend entra en modo sandbox y genera un `initPoint` local

## 8. Configurar WhatsApp Cloud API

Variables:

```
WHATSAPP_ACCESS_TOKEN
WHATSAPP_PHONE_NUMBER_ID
WHATSAPP_BUSINESS_ACCOUNT_ID
WHATSAPP_API_VERSION=v21.0
```

Webhook: `GET/POST /api/whatsapp/webhook`  
Verify token por defecto: `sportreplay-verify`

Sin token, el envio queda registrado en sandbox (`wamid.sandbox...`).

## 9. Conectar una camara RTSP

1. Login como ClubOwner o Admin
2. Crea club y cancha
3. `POST /api/courts/{courtId}/cameras` con `rtspUrl`, usuario y password
4. `POST /api/cameras/{id}/test-connection`
5. Agenda un partido y `POST /api/cameras/{id}/start`

FFmpeg debe estar en el PATH. Credenciales RTSP se cifran y nunca se devuelven en la API.

## 10. Probar el sistema sin camaras reales

El seed crea 3 camaras `Simulated`. No intentan conectarse a la red.

```bash
POST /api/cameras/{id}/test-connection
POST /api/cameras/{id}/start
POST /api/video-clips
```

El worker genera un patron de prueba con FFmpeg `testsrc` (o un placeholder si FFmpeg no esta).

## Stack completo con Docker

```bash
docker compose up -d
```

Frontend: http://localhost:8080  
Backend: http://localhost:5080

## Arquitectura

Ver `documentation/ARCHITECTURE.md`.
