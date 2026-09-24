# GameHub

Comunidade de chat em tempo real para gamers: cadastro/login com JWT, canais,
histórico de mensagens e chat em tempo real com SignalR.

| Camada   | Stack |
| -------- | ----- |
| Backend  | .NET 10 · ASP.NET Core · Clean Architecture · EF Core · PostgreSQL · JWT · SignalR |
| Frontend | React 19 · TypeScript · Vite · Axios · React Router · `@microsoft/signalr` |

```
backend/
  GameHub.Api/             # Controllers, ChatHub, configuração (Program.cs)
  GameHub.Application/     # Serviços, DTOs, interfaces
  GameHub.Domain/          # Entidades
  GameHub.Infrastructure/  # EF Core, repositórios, JWT, hash de senha
  GameHub.Tests/           # Testes (xUnit)
frontend/                  # SPA React (veja frontend/README.md)
docker-compose.yml         # PostgreSQL local de desenvolvimento
```

## Desenvolvimento local

Pré-requisitos: .NET 10 SDK, Node.js (`^20.19.0` ou `>=22.12.0`), Docker e a
ferramenta `dotnet-ef` (`dotnet tool install --global dotnet-ef`).

```bash
# 1. Banco de dados (credenciais locais de desenvolvimento)
docker compose up -d

# 2. Chave JWT de desenvolvimento, fora do repositório (User Secrets)
dotnet user-secrets set "Jwt:Key" "$(node -e "console.log(require('crypto').randomBytes(48).toString('base64'))")" --project backend/GameHub.Api

# 3. Migrations
dotnet ef database update --project backend/GameHub.Infrastructure --startup-project backend/GameHub.Api

# 4. Backend (http://localhost:5116)
dotnet run --project backend/GameHub.Api --launch-profile http

# 5. Frontend (http://localhost:5173), em outro terminal
cd frontend && npm install && npm run dev
```

Em desenvolvimento o Vite encaminha `/api` e `/hubs` para o backend, então não
há CORS envolvido.

Testes e qualidade:

```bash
dotnet test GameHub.slnx
cd frontend && npm run lint && npm run build
```

## Configuração

Nenhum segredo fica no repositório. O backend lê a configuração nesta ordem
(a última vence): `appsettings.json` → `appsettings.{Environment}.json` →
User Secrets (apenas em Development) → variáveis de ambiente.

| Chave (variável de ambiente)              | Obrigatória | Descrição |
| ----------------------------------------- | ----------- | --------- |
| `ASPNETCORE_ENVIRONMENT`                  | —           | `Development` localmente; `Production` (padrão) em deploy. |
| `ConnectionStrings__DefaultConnection`    | Sim         | Connection string do PostgreSQL. Em Development vem de `appsettings.Development.json` (banco local do Docker). |
| `Jwt__Key`                                | Sim         | Chave HMAC-SHA256 com **no mínimo 32 bytes**. Em Development vem de User Secrets. |
| `Cors__AllowedOrigins__0`, `__1`, …       | Não         | Origens do frontend quando ele é servido em outra origem que a API (ex.: `https://app.example.com`). Vazio = apenas mesma origem. |

A aplicação não inicia se a connection string ou a chave JWT estiverem ausentes
(ou se a chave for curta demais), com uma mensagem indicando o que configurar.

Frontend (build): `VITE_API_URL` é opcional e só é necessário quando a API
fica em outra origem. **Tudo que começa com `VITE_` é público no bundle** —
nunca coloque segredos ali. Veja `frontend/.env.example`.

### Topologias de produção suportadas

- **Mesmo domínio (recomendado):** um reverse proxy serve o frontend e
  encaminha `/api` e `/hubs` (com WebSocket) para o backend. Não precisa de
  `VITE_API_URL` nem de CORS.
- **Origens separadas:** build do frontend com `VITE_API_URL=https://api...` e
  backend com `Cors__AllowedOrigins__0=https://app...`.

## Segurança

- Senhas com hash (`PasswordHasher` do ASP.NET Core Identity); tokens JWT
  HS256 com expiração de 2 horas, validação de assinatura, algoritmo e
  expiração (sem tolerância de relógio).
- Fora de Development, erros não tratados retornam ProblemDetails genérico (sem
  stack trace ou detalhes internos) e HSTS é habilitado. Todas as respostas
  incluem `X-Content-Type-Options`, `X-Frame-Options` e `Referrer-Policy`.
- O token do SignalR vai na query string (`access_token`) da conexão WebSocket,
  como exige o navegador. Por isso **não reduza o nível de log de
  `Microsoft.AspNetCore` para `Information` em produção**: o log de requisições
  incluiria a URL com o token.
- Atrás de um reverse proxy que termina TLS, será necessário configurar
  *forwarded headers* para que HTTPS e HSTS reflitam o esquema original
  (etapa de deploy).
- A chave JWT de desenvolvimento usada nas primeiras versões ficou no histórico
  do Git e deve ser considerada comprometida: nunca a reutilize. Cada ambiente
  deve gerar a sua.
