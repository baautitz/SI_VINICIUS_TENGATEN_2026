# Projeto Acadêmico

Aplicação acadêmica de gestão com backend em .NET, frontend em Next.js e banco de dados PostgreSQL.

## Componentes

- Backend em C#/.NET 10
- Frontend em Next.js
- PostgreSQL 17
- Docker Compose para a stack completa e para o banco usado no desenvolvimento local

O backend está organizado em três projetos:

- `Backend.Core`: regras de negócio, entidades, comandos e validações
- `Backend.Infrastructure.PostgreSQL`: persistência com Dapper e Npgsql
- `Backend.Web`: API HTTP e documentação Swagger

## Configuração

Crie o arquivo de ambiente na raiz:

```bash
cp .env.example .env
```

Ajuste os valores se necessário. `POSTGRES_PORT` define a porta publicada localmente para o PostgreSQL.

## Executar a stack completa com Docker

O projeto possui um único Compose, em `infrastructure/docker-compose.yaml`:

```bash
node infra.js up
```

Esse comando inicia o PostgreSQL, o backend e o frontend, construindo as imagens quando necessário.

Para consultar os serviços e os logs:

```bash
node infra.js ps
node infra.js logs
```

Para parar a stack sem remover o volume de dados:

```bash
node infra.js down
```

Também é possível executar diretamente:

```bash
docker compose --env-file .env -f infrastructure/docker-compose.yaml up -d --build
```

Após a inicialização:

- Frontend: `http://localhost:3000`
- Backend/Swagger: `http://localhost:8080`
- PostgreSQL: `localhost:${POSTGRES_PORT}` (por padrão, `localhost:5432`)

Os scripts SQL de `infrastructure/database` são executados automaticamente na primeira inicialização de um volume PostgreSQL vazio.

## Desenvolvimento local

O Compose de desenvolvimento sobe somente o PostgreSQL. Backend e frontend são executados localmente em modo de desenvolvimento pelo `infra.js`.

Para iniciar o banco, o backend com `dotnet watch` e o frontend com `npm run dev`:

```bash
node infra.js dev:up
```

O comando usa `infrastructure/docker-compose.dev.yaml`, não constrói as imagens das aplicações e exibe os logs prefixados de `[Backend]` e `[Frontend]`.

O backend usa o watcher por polling no ambiente dev para evitar problemas com o limite de observadores de arquivos do Linux, mantendo o hot reload ativo.

Para encerrar os processos e o banco, use `Ctrl+C` no terminal do `dev:up`. Para parar somente o PostgreSQL:

```bash
node infra.js dev:down
```

Também é possível iniciar os serviços manualmente:

```bash
node infra.js db:up
```

Em um terminal:

```bash
dotnet watch \
  --project Backend/Backend.Web/Backend.Web.csproj \
  run \
  --urls http://localhost:8080
```

Em outro terminal:

```bash
cd frontend
PORT=3000 NEXT_PUBLIC_API_URL=http://localhost:8080 npm run dev
```

No Windows PowerShell, configure as variáveis do frontend antes de executar `npm run dev`:

```powershell
$env:PORT = "3000"
$env:NEXT_PUBLIC_API_URL = "http://localhost:8080"
npm run dev
```

O `appsettings.Development.json` usa os valores padrão do `.env.example`. Se os valores do `.env` forem alterados, sobrescreva `ConnectionStrings__DefaultConnection` no ambiente do backend.

## Comandos auxiliares

Os aliases `prod:up`, `prod:down` e `prod:<comando>` continuam disponíveis para compatibilidade com o fluxo anterior e usam o Compose principal.

## Tecnologias

- C# / .NET 10
- ASP.NET Core
- Next.js
- React
- PostgreSQL
- Docker / Docker Compose

## Padrões de interface e feedback

O frontend segue um contrato único para feedback operacional. Erros de API,
domínio, rede ou mutações são apresentados uma única vez em toast; a superfície
que iniciou a ação, inclusive um dialog, permanece aberta para que a pessoa
possa corrigir ou revisar os dados. Validações de preenchimento continuam
próximas ao respectivo campo, com `aria-invalid` e mensagem inline.

Mensagens operacionais não devem ser renderizadas como `Alert` dentro de
dialogs, nem registradas com `console.error` ou `console.warn` em fluxos de
uso. A API responde com envelope estruturado, consumido pelo frontend sem
lançar objetos literais. A matriz completa de estados, feedback e navegação
está em [frontend/docs/ui-feedback.md](frontend/docs/ui-feedback.md).
