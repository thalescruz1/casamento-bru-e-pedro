# Release Notes

## v0.1.0 (unreleased)

Primeira versão do site Heloisa & Thales.

### Features

- Landing page responsiva (Hero/Save the Date, Countdown, Convite, Cerimônia & Festa, Local com mapa ilustrado, RSVP form, Presentes, Hospedagem, FAQ).
- API de RSVP (`POST /api/rsvp`) persistindo em Azure Cosmos DB Serverless.
- Lista de presentes pública com reserva única por item.
- Integração Asaas (Pix + cartão) para pagamento online.
- Dashboard admin protegido por SWA Auth com CRUD de presentes, upload de imagens e listagem/export de RSVPs.
- Webhook Asaas idempotente para confirmação/estorno de pagamento.
- Job timer que libera reservas expiradas (30min sem pagamento).

### Stack

- .NET 10 · Azure Functions isolated worker · Clean Architecture + MediatR + FluentValidation.
- Angular 19 · standalone components · signals.
- Azure Static Web Apps · Cosmos DB Serverless · Blob Storage · Application Insights.
