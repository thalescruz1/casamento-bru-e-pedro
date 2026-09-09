# Casamento Heloisa & Thales

Site oficial do casamento de Helo & Thales — 08 de agosto de 2026, Château du Plas, São Roque/SP.

## Stack

- **Frontend**: Angular 19 · standalone components · signals · SCSS.
- **Backend**: .NET 10 · Azure Functions isolated worker · Clean Architecture (Domain → Application → Infrastructure → Api) · MediatR + FluentValidation.
- **Persistência**: Azure Cosmos DB Serverless (3 containers: `rsvps`, `gifts`, `payments`).
- **Imagens**: Azure Blob Storage com URLs SAS renováveis.
- **Pagamento**: Asaas (Pix + cartão) — reserva única por item.
- **Hospedagem**: Azure Static Web Apps (front + API integrados).
- **Auth admin**: credenciais próprias (e-mail + senha) persistidas em Cosmos, sessão via cookie httpOnly assinado com HMAC-SHA256.

## Estrutura

```
casamentothalesehelo/
├── src/
│   ├── Domain/            # entidades, VOs, domain events
│   ├── Application/       # MediatR handlers, DTOs, interfaces
│   ├── Infrastructure/    # Cosmos repos, Asaas gateway, Blob storage
│   └── Api/               # Azure Functions
├── web/                   # Angular workspace
├── tests/
│   ├── Application.Tests/
│   └── Infrastructure.Tests/
├── staticwebapp.config.json
├── docker-compose.yml     # Cosmos Emulator + Azurite
├── Directory.Build.props
├── Directory.Packages.props
└── casamento.slnx
```

## Pré-requisitos

- **Node 22+** (via `.nvmrc` — rode `nvm use`).
- **.NET 10 SDK** (via `global.json`).
- **Docker** (para Cosmos Emulator + Azurite).
- **Azure Functions Core Tools** v4 (`npm i -g azure-functions-core-tools@4 --unsafe-perm true`).
- **Static Web Apps CLI** (`npm i -g @azure/static-web-apps-cli`).

## Setup inicial

```bash
# 1. Ativar Node 22
nvm use

# 2. Instalar dependências do front
cd web && npm install && cd ..

# 3. Restaurar .NET
dotnet restore casamento.slnx

# 4. Subir infra local (Cosmos + Azurite)
docker compose up -d

# 5. Configurar segredos locais (Asaas + Auth)
cd src/Api
dotnet user-secrets init
dotnet user-secrets set "Asaas:ApiKey" "<sua_chave_sandbox_asaas>"
dotnet user-secrets set "Asaas:WebhookSecret" "<segredo_webhook>"

# gere uma chave forte (>=32 chars) para assinar os cookies de sessão
dotnet user-secrets set "Auth:TokenSigningKey" "$(openssl rand -base64 48)"

# credenciais iniciais do painel — criadas automaticamente no primeiro startup
dotnet user-secrets set "Auth:SeedEmail" "thales@casamento.com"
dotnet user-secrets set "Auth:SeedPassword" "SenhaFortePraTrocar#2026"
dotnet user-secrets set "Auth:SeedDisplayName" "Thales"
cd ../..
```

> Em dev, `local.settings.json` já vem com um seed de exemplo (`thales@casamento.local` / `TrocarEsseSegredo#2026`). Os User Secrets acima sobrescrevem.

As demais variáveis (Cosmos, Blob) já vêm pré-configuradas em `src/Api/local.settings.json` apontando para os emuladores locais.

## Rodar em desenvolvimento

Abra três terminais (ou use `concurrently`):

```bash
# Terminal 1 — infra
docker compose up

# Terminal 2 — Functions API
cd src/Api && func start

# Terminal 3 — Angular + SWA proxy
npx swa start http://localhost:4200 --api-location src/Api --api-port 7071
# (em paralelo, outro terminal) cd web && ng serve
```

Acesse `http://localhost:4280` (porta do SWA CLI). O CLI faz proxy do front para `/` e das Functions para `/api/*`, incluindo emulação do `/.auth/me`.

## Build

```bash
# Front
cd web && ng build

# Back
dotnet build casamento.slnx -c Release

# Testes
dotnet test casamento.slnx
```

## Deploy (Azure SWA)

1. Crie um Static Web App no Azure (plano Standard para suportar roles e Functions BYO ou use Managed Functions).
2. Configure as variáveis de ambiente na aba *Configuration* do SWA:
   - `Cosmos__ConnectionString`, `Cosmos__DatabaseName`
   - `Blob__ConnectionString`
   - `Asaas__BaseUrl`, `Asaas__ApiKey`, `Asaas__WebhookSecret`
   - `APPINSIGHTS_INSTRUMENTATIONKEY`
3. Aponte o build para `web/dist/web/browser` (output Angular) e API para `src/Api`.
4. Configure o webhook Asaas apontando para `https://<sua-swa>.azurestaticapps.net/api/webhooks/asaas` usando o mesmo `WebhookSecret` como `access_token`.
5. Convide o e-mail do casal via SWA *Role management* atribuindo a role `admin`.

## Fluxos principais

- **RSVP**: `POST /api/rsvp` (anônimo, rate-limit 3/5min por IP hash) → Cosmos.
- **Presentes público**: `GET /api/gifts` → lista itens disponíveis + reservados.
- **Reserva**: `POST /api/gifts/{id}/reserve` → cria cobrança Asaas → retorna `checkoutUrl` → front redireciona.
- **Webhook Asaas**: `POST /api/webhooks/asaas` (validado por `access_token`, idempotente).
- **Timer**: a cada 5min libera reservas expiradas (>30min sem pagamento).
- **Login admin**: `POST /api/auth/login` (email + senha, PBKDF2-SHA512 com 210k iterações, lockout após 5 falhas por 15min) → seta cookie httpOnly assinado com HMAC-SHA256, `SameSite=Strict`, `Secure` em prod.
- **Admin**: `/admin/login` → após login, `/admin/rsvps` e `/admin/gifts`. Endpoints backend protegidos verificam o cookie antes do handler (fora do pipeline MediatR).

## Segurança

- FluentValidation em todo input antes de chegar ao domínio.
- CSP restritiva, X-Frame-Options DENY, SameSite Lax.
- Secrets via User Secrets (dev) / Key Vault (prod).
- Logs structured via Serilog — PII nunca é logada.
- Webhook HMAC-validated + idempotência por `eventId`.
- Upload de imagem: whitelist MIME, limite 5MB, reprocessada por ImageSharp (remove EXIF).

## Custos estimados (produção)

- Static Web Apps Standard: ~US$ 9/mês.
- Cosmos DB Serverless: < R$ 10/mês para volume esperado.
- Blob Storage: < R$ 1/mês.
- Asaas: Pix 0,99%, cartão 1,99% + R$ 0,49 por transação.
- Application Insights: free tier.

## Testes

```bash
dotnet test casamento.slnx                # testes de domínio/Application
cd web && ng test                         # testes Angular (placeholder, ainda sem specs)
```

## Contato

Dúvidas: Thales Cruz.
