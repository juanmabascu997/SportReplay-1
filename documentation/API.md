# API

Base: `http://localhost:5080`

Autenticacion: `Authorization: Bearer {accessToken}`

## Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

## Clubes y canchas

- `GET/POST /api/clubs`
- `GET/PUT/DELETE /api/clubs/{id}`
- `GET/PUT /api/clubs/{clubId}/settings`
- `GET /api/clubs/{clubId}/dashboard`
- `GET/POST /api/clubs/{clubId}/courts`
- `PUT/DELETE /api/courts/{id}`

## Camaras

- `GET /api/cameras`
- `GET/POST /api/courts/{courtId}/cameras`
- `PUT/DELETE /api/cameras/{id}`
- `POST /api/cameras/{id}/test-connection`
- `POST /api/cameras/{id}/start`
- `POST /api/cameras/{id}/stop`
- `GET /api/cameras/{id}/status`

Las respuestas de camara nunca incluyen `username`, `passwordEncrypted` ni URLs autenticadas.

## Partidos y video

- `GET /api/matches?clubId&courtId&date&time`
- `POST /api/matches`
- `GET /api/matches/{id}`
- `GET /api/matches/{id}/recordings`
- `GET /api/matches/{id}/clips`
- `POST /api/video-clips`
- `GET /api/videos/{id}/stream`
- `GET /api/videos/{id}/download`

`POST /api/video-clips` responde `{ clipId, status: processing }` y encola FFmpeg.

## Pagos y WhatsApp

- `POST /api/payments/create`
- `GET /api/payments/{id}`
- `POST /api/payments/webhook`
- `POST /api/whatsapp/send-video`
- `GET/POST /api/whatsapp/webhook`

## Health

- `GET /health`
- `GET /health/ready`
