-- Migração idempotente para bancos criados antes de contas_pagar/contas_receber terem texto_pesquisa.
-- Bancos novos já nascem certos pelo 00_master.sql; este script NÃO roda na inicialização.
-- Uso: psql -d <banco> -f infrastructure/database/migracoes/texto_pesquisa_contas.sql
\set ON_ERROR_STOP on

BEGIN;

CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

SET search_path TO projeto_sistemas, public;

ALTER TABLE contas_pagar ADD COLUMN IF NOT EXISTS texto_pesquisa TEXT NOT NULL DEFAULT '';
ALTER TABLE contas_receber ADD COLUMN IF NOT EXISTS texto_pesquisa TEXT NOT NULL DEFAULT '';

-- Os schemas são idempotentes: recriam funções, triggers e índices (texto e valor) sem duplicar.
\ir ../financeiro/schema/contas_pagar.sql
\ir ../financeiro/schema/contas_receber.sql

-- O UPDATE dispara o trigger BEFORE, que recalcula o texto de cada título.
UPDATE contas_pagar SET texto_pesquisa = '';
UPDATE contas_receber SET texto_pesquisa = '';

COMMIT;
