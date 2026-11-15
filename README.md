# SI Vinicius Tengaten 2026

Sistema de gestão (ERP) para uma loja, feito como projeto da faculdade. Cobre o ciclo de compra e venda de mercadoria: cadastro de produtos, estoque, compras, vendas, devoluções, contas a pagar e a receber e emissão de NF-e.

## O que o sistema faz

- **Vendas e compras**, com devolução e estorno de cada uma.
- **Estoque**: movimentações, balanço (inventário) e custo médio por SKU.
- **Financeiro**: contas a pagar e a receber, parcelas, baixa, abatimento por devolução e reembolso.
- **Notas fiscais**: importação de XML de NF-e de entrada e emissão de NF-e de devolução e de baixa.
- **Cadastros**: catálogo (produtos, SKUs, marcas, categorias, atributos), parceiros (clientes, fornecedores, emitentes, transportadoras, veículos) e localização.

Os termos usados no código e na tela (Abatimento, Reembolso, Baixa, Estorno, Movimentação...) estão definidos em [GLOSSARY.md](GLOSSARY.md).

## Tecnologias

| Parte | O que usa |
| --- | --- |
| Backend | C# / .NET 10, ASP.NET Core, Dapper + Npgsql, FluentValidation, Swagger (NSwag) |
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS 4, TanStack (Query, Table, Form), Zod |
| Banco | PostgreSQL 18 |
| Infra | Docker Compose, orquestrado pelo `infra.js` |
| Testes | xUnit e Testcontainers no backend; scripts Node no frontend |

## Como rodar

Você precisa de Docker, Node 20 ou mais novo e o SDK do .NET 10.

### Desenvolvimento

```bash
node infra.js dev:up
```

Sobe o PostgreSQL no Docker e roda o backend (`dotnet watch`) e o frontend (`npm run dev`) na sua máquina, com hot reload. O frontend precisa de `npm install` na primeira vez (`cd frontend && npm install`).

- Frontend: http://localhost:3000
- API e Swagger: http://localhost:8080
- PostgreSQL: `localhost:5432`

`Ctrl+C` encerra tudo, inclusive o banco. Para parar só o banco: `node infra.js dev:down`.

### Stack completa em containers

```bash
node infra.js up      # sobe postgres, backend e frontend (constrói as imagens)
node infra.js ps      # estado dos serviços
node infra.js logs    # logs
node infra.js down    # para os containers, mantém o volume do banco
```

As portas 3000 e 8080 não podem estar em uso, então pare o ambiente de desenvolvimento antes.

### Configuração

As variáveis (banco, portas) ficam no `.env.example`. Se quiser mudar algo, copie para `.env`; sem ele o `infra.js` usa o `.env.example`.

```bash
cp .env.example .env
```

Se mudar usuário, senha ou porta do banco, lembre que o `appsettings.Development.json` do backend usa os valores padrão. Nesse caso defina `ConnectionStrings__DefaultConnection` no ambiente do backend.

### Banco de dados

Os scripts de `infrastructure/database` rodam sozinhos na primeira vez que o volume do Postgres é criado. **Não há migrations**: depois de mudar o schema, recrie o volume.

```bash
docker compose -f infrastructure/docker-compose.dev.yaml down -v
node infra.js dev:up
```

Isso apaga os dados do banco de desenvolvimento.

## Testes

Backend (xUnit). Os de integração usam PostgreSQL via Testcontainers e precisam de Docker:

```bash
dotnet test Backend/Backend.Tests                                  # tudo
dotnet test Backend/Backend.Tests --filter "Category!=Integration" # só unitários, sem Docker
dotnet test Backend/Backend.Tests --filter "Category=Integration"  # só integração
```

Todo teste que usa `PostgresFixture` precisa da trait `[Trait("Category", "Integration")]`.

Frontend:

```bash
cd frontend
npx tsc --noEmit    # tipos
npm run lint
npm run test:ordenacao   # há um script test:* por área; veja `npm run`
```

## Estrutura

```
Backend/
  Backend.Core/                      regras de negócio: entidades, serviços, comandos, validações
  Backend.Infrastructure.PostgreSQL/ repositórios (Dapper)
  Backend.Web/                       controllers e Swagger
  Backend.Tests/                     testes, organizados por módulo em Unit e Integration
frontend/                            app Next.js (telas em src/features)
infrastructure/                      docker-compose e scripts SQL do banco
docs/                                decisões (adr), atalhos de teclado e guias
infra.js                             comandos de infra e de desenvolvimento
```

## Documentação

- [GLOSSARY.md](GLOSSARY.md): vocabulário do domínio.
- [docs/adr](docs/adr): decisões de arquitetura e de produto, com o motivo de cada uma.
- [docs/atalhos.md](docs/atalhos.md): atalhos de teclado das telas.
- [frontend/docs](frontend/docs): regras de feedback e de layout da interface.
