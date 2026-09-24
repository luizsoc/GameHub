# GameHub — Frontend

Interface web do GameHub, uma comunidade de chat em tempo real para gamers.

**Stack:** React 19 · TypeScript · Vite · React Router · Axios · `@microsoft/signalr` · Oxlint

## Funcionalidades

- Cadastro, login e logout com JWT (sessão restaurada ao recarregar e encerrada quando o token expira)
- Rotas protegidas
- Lista e criação de canais
- Histórico de mensagens por canal (REST)
- Chat em tempo real via SignalR: entrada/saída de grupos por canal, reconexão automática,
  deduplicação por `id` e indicador de conexão
- Auto-scroll que acompanha novas mensagens apenas quando o usuário já está no fim da lista
- Layout responsivo e tema claro/escuro automático

## Pré-requisitos

- Node.js `^20.19.0` ou `>=22.12.0` (exigência do Vite 8)
- Backend do GameHub rodando em `http://localhost:5116` (perfil `http` de `backend/GameHub.Api`)
  e PostgreSQL (`docker compose up -d` na raiz do repositório)

## Como executar

```bash
npm install
npm run dev
```

A aplicação abre em `http://localhost:5173`. Em desenvolvimento, o Vite encaminha
`/api` (REST) e `/hubs` (SignalR, incluindo WebSocket) para o backend, então não é
necessário configurar CORS.

## Scripts

| Script            | Descrição                                   |
| ----------------- | ------------------------------------------- |
| `npm run dev`     | Servidor de desenvolvimento com HMR         |
| `npm run build`   | Checagem de tipos (`tsc -b`) + build de produção |
| `npm run lint`    | Oxlint                                      |
| `npm run preview` | Serve o build de produção localmente        |

## Estrutura

```
src/
├── api/          # Cliente Axios, chamadas REST e tradução de erros para o usuário
├── auth/         # AuthContext, armazenamento/decodificação do JWT, validações
├── components/   # Layout, canais, mensagens e componentes de formulário
├── hooks/        # useChannels, useMessages, useChatConnection, useChannelRealtime
├── pages/        # Login e cadastro
├── services/     # Conexão SignalR e contrato do ChatHub
└── types/        # Tipos espelhando os DTOs do backend
```

## Produção

O build usa URLs relativas (`/api`, `/hubs/chat`) por padrão, o que funciona
quando um reverse proxy serve o frontend e o backend no mesmo domínio.

Se a API estiver em outra origem, defina `VITE_API_URL` no momento do build
(veja `.env.example`) e libere a origem do frontend no backend com
`Cors__AllowedOrigins__0`. Variáveis `VITE_*` são públicas: nunca coloque
segredos nelas.

`npm run preview` serve o build de produção usando o mesmo proxy do
desenvolvimento, para conferência local.

## Notas

- O JWT fica no `localStorage` e é enviado como `Authorization: Bearer` nas chamadas REST
  e via `accessTokenFactory` na conexão SignalR. O frontend apenas decodifica o token para
  exibir o usuário e saber quando ele expira; a validação é sempre do backend.
- Nenhum segredo fica no frontend.
