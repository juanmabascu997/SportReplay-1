# Arquitectura

SportReplay usa una arquitectura modular en .NET 8:

- `SportReplay.Domain`: entidades y enums. Sin dependencias de infraestructura.
- `SportReplay.Application`: contratos, DTOs, validaciones FluentValidation, opciones.
- `SportReplay.Infrastructure`: EF Core, MinIO, FFmpeg, Mercado Pago, WhatsApp, workers.
- `SportReplay.Api`: controladores, JWT, Swagger, health checks, ProblemDetails.
- `SportReplay.VideoWorker`: host independiente con los mismos hosted services.

## Flujo de video

Camera -> RTSP/ONVIF o Simulated -> FFmpeg -> segmentos -> MinIO/S3 -> HLS -> React/Hls.js

PostgreSQL guarda solo metadata. Los archivos viven en object storage.

## Autorizacion

JWT + roles `Admin`, `ClubOwner`, `Operator`, `Player`.  
`IClubAuthorization` impide que un propietario opere otro club.

## Jobs

Tabla `processing_jobs`:

- `ProcessClip`
- `SendWhatsApp`

Workers: `VideoRecordingWorker`, `VideoProcessingWorker`, `VideoCleanupWorker`, `WhatsAppDispatchWorker`.

## Extensibilidad IA

Los clips se generan a partir de `Recording` + timestamps. Un futuro detector de jugadas puede crear `VideoClip` con `StartTime`/`EndTime` sin cambiar el pipeline.
