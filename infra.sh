#!/usr/bin/env bash

# Encaminha todos os argumentos do atalho para a aplicação .NET de setup de infraestrutura
docker compose -f ./infra/docker-compose.dev.yml --env-file ./.env.development -- "$@"
