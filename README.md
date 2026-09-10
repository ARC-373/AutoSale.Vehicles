# AutoSale: Serviço de Veículos

API REST do Serviço de Veículos da Prova Substitutiva do Tech Challenge FIAP Pós-Tech SOAT Fase 4.

Este repositório contém exclusivamente o microsserviço responsável pelo cadastro, atualização e ciclo de vida dos veículos. As funcionalidades de venda e pagamento pertencem ao repositório parceiro [AutoSale.Sales](https://github.com/ARC-373/AutoSale.Sales). Os dois serviços possuem código, execução e bancos de dados independentes e comunicam-se por APIs REST.

## Índice

- [Responsabilidades e escopo](#responsabilidades-e-escopo)
- [Funcionalidades](#funcionalidades)
- [Tecnologias e arquitetura](#tecnologias-e-arquitetura)
- [Estrutura da aplicação](#estrutura-da-aplicação)
- [Execução local](#execução-local)
- [Documentação no Scalar](#documentação-no-scalar)
- [Endpoints principais](#endpoints-principais)
- [Ciclo de vida do veículo](#ciclo-de-vida-do-veículo)
- [Integração com Vendas](#integração-com-vendas)
- [Autenticação e autorização](#autenticação-e-autorização)
- [Modelagem do banco](#modelagem-do-banco)
- [Testes](#testes)
- [CI/CD](#cicd)
- [Observabilidade](#observabilidade)

## Responsabilidades e escopo

O Serviço de Veículos é a fonte de verdade dos dados e da disponibilidade de cada veículo. Nesta versão, suas operações de usuário são restritas a integrantes do grupo Cognito `admins`, que podem cadastrar e atualizar veículos disponíveis, consultar o catálogo completo e acompanhar todas as reservas ou o histórico de um veículo.

O serviço também oferece uma API interna, acessível apenas pelo Serviço de Vendas, para reservar um veículo, confirmar sua venda ou liberar sua reserva. Clientes finais não compram veículos diretamente nesta API. Processo de venda, pagamento e webhook do provedor de pagamentos são responsabilidades do [AutoSale.Sales](https://github.com/ARC-373/AutoSale.Sales).

## Funcionalidades

| Área | Funcionalidade | Comportamento |
| --- | --- | --- |
| Administração | Cadastrar veículo | Cria marca, modelo, ano, cor e preço no estado `Available`. |
| Administração | Atualizar veículo | Altera dados mediante controle por `version`; somente veículos disponíveis podem ser editados. |
| Administração | Consultar veículos | Lista o catálogo paginado ou consulta um veículo pelo identificador. |
| Administração | Acompanhar reservas | Lista todas as reservas ou o histórico de um veículo, com snapshots e estados `Reserved`, `Confirmed` e `Released`. |
| Integração | Reservar veículo | Vendas informa `saleId` e preço esperado; a operação bloqueia o veículo e é idempotente para a mesma solicitação. |
| Integração | Confirmar venda | Converte a reserva válida em venda confirmada e muda o veículo para `Sold`. |
| Integração | Liberar reserva | Cancela reserva não confirmada e devolve o veículo a `Available`. |
| Sincronização | Publicar catálogo | Registra mudanças em transactional outbox e as envia a Vendas com retentativas. |
| Operação | Saúde e telemetria | Expõe health checks e envia traces e métricas via OpenTelemetry. |

## Tecnologias e arquitetura

- .NET 10, ASP.NET Core Web API, Entity Framework Core 10 e Npgsql;
- PostgreSQL 16 exclusivo deste serviço;
- Amazon Cognito (OIDC/OAuth 2.0 e JWT) para administradores;
- chave no cabeçalho `X-Service-Key` para comunicação entre microsserviços;
- OpenAPI, Scalar, Docker, Docker Compose e OpenTelemetry;
- xUnit para testes de domínio, aplicação, API, infraestrutura, integração e arquitetura;
- GitHub Actions para build, testes e validação do Docker Compose.

### Arquitetura de microsserviços

A solução da Fase 4 é composta por microsserviços independentes. Veículos e Vendas são implantáveis separadamente, possuem responsabilidades e bancos segregados e trocam dados por HTTP/REST. Uma indisponibilidade temporária de Vendas não desfaz alterações confirmadas em Veículos: o transactional outbox conserva os eventos de catálogo para publicação posterior.

Internamente, este microsserviço usa Clean Architecture. As dependências apontam para as camadas centrais, limite verificado por testes automatizados.

![Diagrama da arquitetura do AutoSale](docs/architecture/autosale-architecture.png)

| Camada | Responsabilidade |
| --- | --- |
| **Domain** | `Vehicle`, `VehicleReservation` e `CatalogOutbox`, seus estados, transições e invariantes. |
| **Application** | Casos de uso, DTOs e portas para persistência, relógio e integração com Vendas. |
| **Infrastructure** | EF Core/PostgreSQL, repositórios, transações, migrations, cliente HTTP, outbox e worker. |
| **SharedKernel** | Tipos independentes como `Entity`, `Result`, `Error` e `ErrorType`. |
| **API** | Controllers, contratos, autenticação, autorização, `ProblemDetails`, Scalar e health checks. |

## Estrutura da aplicação

```text
AutoSale.Vehicles/
├── .github/workflows/ci.yml
├── docs/
│   ├── architecture/
│   ├── readme/                         # Evidências da documentação interativa
│   └── spec/                           # Definições do Tech Challenge
├── src/
│   ├── AutoSale.Vehicles.Api/
│   │   ├── Authentication/  Authorization/  Contracts/
│   │   ├── Controllers/  Extensions/  Middleware/
│   │   └── Program.cs
│   ├── AutoSale.Vehicles.Application/
│   │   ├── Abstractions/  Catalog/  Common/  Reservations/
│   │   └── Vehicles/
│   ├── AutoSale.Vehicles.Domain/
│   │   ├── Catalog/  Reservations/  Vehicles/
│   ├── AutoSale.Vehicles.Infrastructure/
│   │   ├── BackgroundServices/  Clock/  Integrations/Sales/
│   │   └── Persistence/
│   └── BuildingBlocks/AutoSale.SharedKernel/
├── tests/
│   ├── AutoSale.Vehicles.Api.IntegrationTests/
│   ├── AutoSale.Vehicles.Api.UnitTests/
│   ├── AutoSale.Vehicles.Application.UnitTests/
│   ├── AutoSale.Vehicles.ArchitectureTests/
│   ├── AutoSale.Vehicles.Domain.UnitTests/
│   └── AutoSale.Vehicles.Infrastructure.UnitTests/
├── docker-compose.yml
├── otel-collector-config.yaml
└── AutoSale.Vehicles.slnx
```

## Execução local

### Usuários de teste no Cognito
| Usuário          | Senha       | Grupo    | Observações                    |
| ---------------- | ----------- | -------- | ------------------------------ |
| `admin.autosale` | `!Fiap2026` | `admins` | Usuário administrador de teste |
| `buyer.autosale` | `!Fiap2026` | --       | Usuário comprador de teste.    |

Outros usuários cadastrados se classificam como compradores.

### Pré-requisitos

- [Git](https://git-scm.com/);
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) com Docker Compose v2 ou gerenciador de containers equivalente;
- opcionalmente, SDK do .NET 10 para executar testes fora dos containers;
- conta do Amazon Cognito no grupo `admins` para testar endpoints administrativos.

### Subir os dois microsserviços

Clone os repositórios como diretórios irmãos:

```powershell
git clone https://github.com/ARC-373/AutoSale.Vehicles.git
git clone https://github.com/ARC-373/AutoSale.Sales.git
```

Inicie o Serviço de Veículos:

```powershell
Set-Location AutoSale.Vehicles
docker compose up --build -d
```

Em outro terminal, inicie o Serviço de Vendas:

```powershell
Set-Location AutoSale.Sales
docker compose up --build -d
```

Cada repositório possui seu `.env` e PostgreSQL próprios. Configure os mesmos valores complementares para:

- `SALES_TO_VEHICLES_SERVICE_KEY`: autentica Vendas perante Veículos;
- `VEHICLES_TO_SALES_SERVICE_KEY`: autentica Veículos perante Vendas;
- `SALES_BASE_ADDRESS`: endereço interno da API de Vendas;
- `CATALOG_PUBLISHER_ENABLED=true`: habilita a sincronização assíncrona do catálogo.

A versão disponibilizada no repositório já inclui valores válidas e pareados de variáveis de ambiente para que as duas aplicações se comuniquem corretamente.
Os Compose conectam as APIs à rede Docker compartilhada `autosale-integration`. Só execute os testes ponta a ponta depois que os dois ambientes estiverem saudáveis. 
  

No Serviço de Veículos, confira e opere o ambiente com:

```powershell
Invoke-WebRequest http://localhost:8080/health
Invoke-WebRequest http://localhost:8080/health/live
docker compose ps
docker compose logs vehicles-api
docker compose down
```

As migrations são aplicadas na inicialização da API. Para remover também os volumes, use conscientemente `docker compose down --volumes --remove-orphans`.

## Documentação no Scalar

Com Veículos em execução, acesse <http://localhost:8080/docs/>. O OpenAPI fica em <http://localhost:8080/openapi/v1.json>.

Para operações administrativas, abra **Authentication**, selecione `CognitoOAuth`, autorize com uma conta do grupo `admins` e use o *access token*. Os endpoints internos apresentam o esquema `ServiceKey`, destinado somente aos microsserviços.

![Scalar inicial](docs/readme/scalar.jpg)
![Autenticação no Scalar](docs/readme/scalar2.jpg)
![Scalar autenticado](docs/readme/scalar3.jpg)

## Endpoints principais

Erros seguem `ProblemDetails`. Listagens aceitam `page` (padrão `1`) e `pageSize` (padrão `20`, máximo `100`).

### Operações administrativas

| Método e rota | Permissão | Descrição |
| --- | --- | --- |
| `POST /api/v1/vehicles` | JWT + `admins` | Cadastra veículo; retorna `201 Created`. |
| `PUT /api/v1/vehicles/{id}` | JWT + `admins` | Atualiza veículo disponível, validando `version`. |
| `GET /api/v1/vehicles/{id}` | JWT + `admins` | Consulta dados e estado do veículo. |
| `GET /api/v1/vehicles?page=1&pageSize=20` | JWT + `admins` | Lista veículos por preço e identificador. |
| `GET /api/v1/reservations?page=1&pageSize=20` | JWT + `admins` | Lista todas as reservas. |
| `GET /api/v1/vehicles/{vehicleId}/reservations?page=1&pageSize=20` | JWT + `admins` | Lista reservas do veículo. |

### Integração com o Serviço de Vendas

| Método e rota | Permissão | Descrição |
| --- | --- | --- |
| `PUT /internal/v1/vehicles/{vehicleId}/reservations/{saleId}` | `X-Service-Key` | Reserva pelo preço esperado; retorna `201` ao criar ou `200` na repetição idempotente. |
| `PUT /internal/v1/vehicles/{vehicleId}/reservations/{saleId}/confirmation` | `X-Service-Key` | Confirma a reserva e marca o veículo como vendido. |
| `PUT /internal/v1/vehicles/{vehicleId}/reservations/{saleId}/release` | `X-Service-Key` | Libera a reserva e torna o veículo disponível. |
| `PUT /internal/v1/catalog/vehicles/{vehicleId}` | chave enviada por Veículos | Endpoint hospedado em Vendas, chamado pelo worker para atualizar sua projeção. |

Também são públicos `GET /health` (API e PostgreSQL) e `GET /health/live` (processo).

### Exemplos

Cadastro:

```json
{
  "make": "Toyota",
  "model": "Corolla XEi",
  "year": 2026,
  "color": "Prata",
  "price": 149990.00
}
```

Atualização com controle otimista:

```json
{
  "make": "Toyota",
  "model": "Corolla XEi",
  "year": 2026,
  "color": "Cinza",
  "price": 147990.00,
  "version": 1
}
```

Corpo da reserva solicitada por Vendas:

```json
{
  "expectedPrice": 147990.00
}
```

## Ciclo de vida do veículo

```text
cadastro ──> Available ──reserva──> Reserved ──confirmação──> Sold
                    ^                    │
                    └────liberação───────┘
```

1. Um administrador cadastra o veículo, inicialmente `Available`.
2. Enquanto disponível, ele pode ser atualizado. Cada alteração incrementa `version` e gera um item de outbox.
3. Ao iniciar uma venda, Vendas pede a reserva com seu `saleId` e o preço esperado.
4. Veículos bloqueia a linha com `SELECT ... FOR UPDATE`, compara o preço, muda para `Reserved` e grava uma reserva com snapshot.
5. Após o pagamento, Vendas solicita confirmação ou liberação. A primeira leva a `Sold`; a segunda retorna a `Available`.
6. Veículos reservados ou vendidos não podem ser editados. Transação, bloqueio pessimista e versão impedem operações concorrentes incompatíveis.

## Integração com Vendas

A comunicação ocorre nos dois sentidos:

- **Vendas → Veículos:** chamadas síncronas reservam, confirmam ou liberam. O `saleId` correlaciona os serviços e torna repetições seguras; `X-Service-Key` autentica as chamadas.
- **Veículos → Vendas:** cadastro, edição e mudança de status criam registros em `catalog_outbox` na mesma transação. O `CatalogPublisherWorker` os envia em lotes para `PUT /internal/v1/catalog/vehicles/{vehicleId}` e registra sucesso ou retentativa.

Vendas mantém sua projeção para listagens e conduz compra e pagamento. Veículos continua sendo a autoridade sobre disponibilidade e rejeita preço divergente, veículo indisponível, reserva de outra venda e transições terminais inválidas.

## Autenticação e autorização

- endpoints administrativos validam JWT do Cognito, `token_use=access`, `client_id` e o grupo `admins` em `cognito:groups`;
- endpoints internos usam uma chave compartilhada específica em `X-Service-Key`;
- health checks são públicos;
- esta aplicação não armazena senha, CPF, nome ou e-mail de comprador, somente o `saleId` técnico recebido de Vendas.

## Modelagem do banco

O PostgreSQL exclusivo deste microsserviço possui três tabelas gerenciadas por migrations:

| Tabela | Campos relevantes | Regras e índices |
| --- | --- | --- |
| `vehicles` | `id`, dados do veículo, `status`, `reservation_sale_id`, `sold_sale_id`, timestamps e `version` | Preço/versão positivos; coerência do estado; índice `(status, price, id)`; `version` é token de concorrência. |
| `vehicle_reservations` | `sale_id`, `vehicle_id`, `status`, snapshots dos dados/versão e timestamps | `sale_id` é PK; FK para `vehicles`; snapshots positivos; coerência de timestamps; índice `(vehicle_id, created_at_utc)`. |
| `catalog_outbox` | `id`, `vehicle_id`, `vehicle_version`, `payload_json`, processamento, tentativas, erro e lease | FK; chave única `(vehicle_id, vehicle_version)`; índices para pendências e leases expirados. |

A reserva preserva o snapshot do veículo no momento da criação. O banco de Vendas é separado e está documentado em [AutoSale.Sales](https://github.com/ARC-373/AutoSale.Sales).

## Testes

```powershell
dotnet test AutoSale.Vehicles.slnx --configuration Release
```

| Tipo | Projeto | Foco |
| --- | --- | --- |
| Domínio | `AutoSale.Vehicles.Domain.UnitTests` | Invariantes, estados e transições de veículos, reservas e outbox. |
| Aplicação | `AutoSale.Vehicles.Application.UnitTests` | Cadastro, atualização, reserva, confirmação, liberação e publicação do catálogo. |
| API | `AutoSale.Vehicles.Api.UnitTests` | Controllers, HTTP, autenticação por chave e políticas. |
| Infraestrutura | `AutoSale.Vehicles.Infrastructure.UnitTests` | Cliente HTTP de Vendas, URI, autenticação, respostas, timeout e indisponibilidade. |
| Integração | `AutoSale.Vehicles.Api.IntegrationTests` | Pipeline HTTP, health checks, autenticação e API exposta. |
| Arquitetura | `AutoSale.Vehicles.ArchitectureTests` | Dependências permitidas entre as camadas. |

Testes entre os dois serviços devem ser executados após ambos os ambientes Docker estarem ativos, validando sincronização, reserva e conclusão ou cancelamento da venda.

## CI/CD

O workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) é acionado em Pull Requests, *pushes* para `master` e manualmente. A esteira restaura dependências, compila em Release, executa todos os testes, publica resultados TRX, constrói a imagem Docker e valida inicialização e health check.

Isso permite versionar, testar e implantar Veículos independentemente de Vendas, conforme a arquitetura de microsserviços da Fase 4.

## Observabilidade

A API instrumenta ASP.NET Core, chamadas HTTP e runtime com OpenTelemetry e envia traces e métricas via OTLP ao Collector do Compose. Para diagnóstico, use `docker compose ps`, `docker compose logs vehicles-api` e os logs do worker. IDs de veículo e venda correlacionam o fluxo distribuído sem replicar dados pessoais.
