# WhatsApp Cloud API

Flujo:

1. Usuario pide clip por WhatsApp
2. Se crea `VideoRequest` (`AwaitingPayment`)
3. Se crea `Payment` Mercado Pago
4. Webhook `approved` => `VideoRequest.Paid`
5. `WhatsAppDispatchWorker` llama Cloud API `type=video` con signed URL
6. Se guarda `wamid`, telefono, URL, `sentAt`
7. El webhook de Meta actualiza `deliveredAt` / `readAt`

Los tokens no se persisten en la base. Solo environment variables.

Sandbox: si no hay token, se registra `wamid.sandbox.{id}` como enviado.
