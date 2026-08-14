# Camera setup

## RTSP

Usa la URL del stream principal, por ejemplo:

`rtsp://192.168.1.50:554/Streaming/Channels/101`

El backend cifra el password con AES-256. `test-connection` hace:

1. TCP al puerto 554 si hay IP
2. Probe FFmpeg `-rtsp_transport tcp`
3. SOAP ONVIF `GetDeviceInformation` si hay `onvifUrl`

## ONVIF

Completa `onvifUrl` (normalmente `http://ip:80/onvif/device_service`). El descubrimiento es best-effort; el recording usa RTSP.

## Camaras simuladas

`protocol = Simulated` o `isSimulated = true`. No hay red. Sirven para seed, tests y demos.

## Heartbeat

`LastHeartbeat` se actualiza en tests, start recording y worker. Estados: `ONLINE`, `OFFLINE`, `ERROR`.
