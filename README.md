# Projeto Acadêmico

Este repositório contém um projeto acadêmico desenvolvido com o objetivo de praticar conceitos de arquitetura fullstack, integração entre backend e frontend, persistência de dados e orquestração de serviços com Docker.

A aplicação é composta por:

- Backend em C# (.NET)
- Frontend em Next.js
- Banco de dados PostgreSQL

Tudo seguindo uma estrutura moderna e organizada, pensada para facilitar o desenvolvimento e a execução local.

---

## Visão Geral

O projeto foi idealizado para fins de estudo e experimentação, simulando uma aplicação real com separação clara de responsabilidades entre as camadas.

### Componentes:

- Backend em C# com .NET
- Frontend em Next.js
- Banco de dados PostgreSQL
- Ambiente conteinerizado com Docker

---

## Estrutura do Projeto

O sistema segue uma arquitetura desacoplada:

- **Backend**: responsável pela lógica de negócio e exposição da API
- **Frontend**: responsável pela interface e consumo da API
- **Banco de Dados**: responsável pela persistência dos dados
- **Docker**: responsável pela orquestração dos serviços

---

## Como Executar o Projeto

### 1. Criar o arquivo de variáveis de ambiente

Na raiz do projeto, crie um arquivo `.env` com base no `.env.example`:

```bash
cp .env.example .env
```

Ajuste as variáveis conforme necessário.

---

### 2. Subir os serviços

Com o arquivo `.env` configurado, execute:

```bash
docker compose --env-file .env -f infrastructure/docker-compose.yaml up
```

Esse comando irá iniciar:

- PostgreSQL
- Scripts de inicialização do banco
- Backend
- Frontend

---

### 3. Acessar a aplicação

Após os containers estarem ativos:

- A aplicação estará disponível nas portas definidas no `.env`

---

## Inicialização do Banco de Dados

O banco é provisionado automaticamente durante a inicialização.

Os scripts estão localizados em:

```
infrastructure/database
```

Esses scripts são aplicados automaticamente ao subir os containers, sem necessidade de configuração manual.

---

## Objetivos Acadêmicos

Este projeto foi desenvolvido com foco em aprendizado prático, abordando:

- Estruturação de aplicações modernas
- Separação de responsabilidades (frontend/backend)
- Desenvolvimento de APIs com C# e .NET
- Integração com PostgreSQL
- Uso de Docker para automação de ambiente
- Organização baseada em boas práticas

---

## Tecnologias Utilizadas

- C# / .NET
- Next.js
- PostgreSQL
- Docker
- Docker Compose
