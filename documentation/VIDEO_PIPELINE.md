# Video pipeline

1. `VideoRecordingWorker` detecta partidos `Scheduled` cuyo horario ya empezo.
2. `StartRecording` crea `Recording` y lanza FFmpeg.
3. Camaras reales: `ffmpeg -rtsp_transport tcp -i rtsp://... -c copy source.mp4`
4. Camaras simuladas: `ffmpeg -f lavfi -i testsrc ...`
5. Segmentos y HLS se suben a MinIO/S3. PostgreSQL guarda paths.
6. `POST /api/video-clips` valida `endTime > startTime` y `duration <= MAX_CLIP_DURATION_SECONDS`.
7. `VideoProcessingWorker` recorta, thumbnail y marca `ready`.
8. Reproduccion con signed URLs (expiran, default 15 min).
9. `VideoCleanupWorker` borra objetos con `ExpiresAt` vencido.

Config:

```
VIDEO_RETENTION_DAYS=7
MAX_CLIP_DURATION_SECONDS=60
```

El club puede overridear estos valores en `club_settings`.
