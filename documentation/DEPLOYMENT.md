# Deployment

## Docker Compose

```bash
cp .env.example .env
docker compose up -d --build
```

Servicios: PostgreSQL, MinIO, backend, frontend. Redis es opcional (`--profile optional`).

## Produccion AWS S3

En `Storage`:

```
Provider=S3
Endpoint=s3.amazonaws.com
AccessKey=...
SecretKey=...
Bucket=sportreplay-videos
UseSsl=true
Region=us-east-1
```

`IStorageService` es S3-compatible (MinIO SDK).

## HTTPS y secretos

- Termina TLS en un reverse proxy (Nginx, ALB, Traefik)
- `JWT_SECRET` minimo 32 caracteres
- No loguear passwords ni tokens
- Rate limiting activo en `/api`
- CORS por `Cors:AllowedOrigins`

## Health

Usa `/health/ready` en el load balancer (depende de PostgreSQL).
