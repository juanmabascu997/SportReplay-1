# Mercado Pago

`MercadoPagoService` crea preferencias Checkout Pro.

Webhook `POST /api/payments/webhook`:

1. Valida firma `x-signature` / `x-request-id` cuando hay `MERCADOPAGO_WEBHOOK_SECRET`
2. Consulta el pago real en la API de Mercado Pago (no confia solo en el payload)
3. Actualiza `Payment`
4. Si `approved` y hay `VideoRequest`, lo marca `Paid` y encola WhatsApp
5. Persiste `PaymentWebhook` con indice unico `(Provider, EventType, ExternalId)` para idempotencia

Estados: `pending`, `approved`, `rejected`, `cancelled`, `refunded`.

Sin access token, un POST sandbox `{ paymentId, status: "approved" }` alcanza para desarrollo.
