# FGC.Notifications

Microsserviço de notificações para a plataforma FCG (FIAP Cloud Games). Processa eventos de pagamento e envia notificações (simuladas via log) de forma assíncrona através de Azure Functions com trigger de Azure Service Bus. Projeto da **Fase 3 do Tech Challenge — PosTech FIAP**.

## Fluxo de Comunicação entre Microsserviços

```mermaid
graph LR
    Client([Cliente]) -->|HTTP| APIM[API Gateway]
    APIM -->|/api/users/**| Users[FGC.Users API]
    APIM -->|/api/games/**| Games[FCG.Games API]

    Games -->|OrderPlacedEvent| Q1[/order-placed/]
    Q1 -->|ServiceBusTrigger| Payments[FCG.Payments Function]
    Payments -->|PaymentProcessedEvent| Q2[/payments-processed/]
    Q2 -->|BackgroundService| Games
    Payments -->|PaymentProcessedEvent| Q3[/notifications-payment-processed/]
    Q3 -->|ServiceBusTrigger| Notifications[FGC.Notifications Function]

    Users --- DB1[(FGCUsersDb)]
    Games --- DB2[(FCGGamesDb)]
    Payments --- DB3[(FCGPaymentsDb)]

    Games -.->|Logs & Traces| AI[Application Insights]
    Users -.->|Logs & Traces| AI
    Payments -.->|Logs & Traces| AI
    Notifications -.->|Logs & Traces| AI
```

## Fluxo de Notificações

```mermaid
sequenceDiagram
    participant C as Cliente
    participant G as FCG.Games API
    participant SB as Azure Service Bus
    participant P as FCG.Payments Function
    participant N as FGC.Notifications Function

    C->>G: POST /api/games/{id}/purchase (JWT)
    G->>SB: OrderPlacedEvent → order-placed

    SB->>P: Trigger: order-placed
    P->>SB: PaymentProcessedEvent → notifications-payment-processed

    SB->>N: Trigger: notifications-payment-processed
    alt Status = Approved
        N-->>N: Log: compra aprovada + email de confirmação (simulado)
    else Status = Rejected
        N-->>N: Log: compra rejeitada
    end
```

## Arquitetura

Azure Function Isolated Worker (.NET 8) com estrutura simples:

```
NotificationsAPI/
├── Functions/
│   └── PaymentProcessedFunction.cs   # Azure Function (ServiceBusTrigger)
├── Shared/
│   └── Events/
│       └── PaymentProcessedEvent.cs  # Evento de domínio
├── Program.cs                        # Startup (Serilog + Application Insights)
├── host.json                         # Configuração do Azure Functions
├── appsettings.json                  # Configuração do Serilog
├── Dockerfile                        # Multi-stage build
└── NotificationsAPI.csproj
```

## Function

### PaymentProcessedFunction

- **Trigger**: Azure Service Bus — queue `notifications-payment-processed`
- **Input**: `ServiceBusReceivedMessage` com `PaymentProcessedEvent` no body
- **Comportamento**:
  - `Status == "Approved"` → Loga confirmação de compra + simula envio de email
  - `Status != "Approved"` → Loga rejeição do pagamento
  - Evento nulo/inválido → Log de warning

## Configuração

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `SERVICEBUS_CONNECTION` | Connection string do Azure Service Bus | (obrigatório) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights | (desabilitado se vazio) |

## CI/CD

Pipeline GitHub Actions (`.github/workflows/ci-cd.yml`):

- **CI** (push + PR na master): restore → build
- **CD** (apenas push na master): build Docker → push ACR → deploy Azure Container App

## Build & Run

```bash
# Build
dotnet build

# Rodar Functions localmente (requer Azure Functions Core Tools)
cd NotificationsAPI
func start
```

## Docker

```bash
docker build -f NotificationsAPI/Dockerfile -t fgc-notifications .
docker run -p 5099:80 \
  -e SERVICEBUS_CONNECTION="Endpoint=sb://..." \
  fgc-notifications
```

## Observabilidade

- **Serilog** com sinks para Console e Application Insights
- **Application Insights** para logs, traces e métricas centralizados
- Logs estruturados com `ServiceName: FGC.Notifications`

## Tecnologias

- .NET 8.0
- Azure Functions (Isolated Worker)
- Azure Service Bus (Queues)
- Serilog + Application Insights
