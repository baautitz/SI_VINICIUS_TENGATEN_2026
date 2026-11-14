SET search_path TO projeto_sistemas;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.tipo_movimentacao_estoque_enum AS ENUM (
    'ENTRADA',
    'SAIDA'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.origem_movimentacao_estoque_enum AS ENUM (
    'MANUAL',
    'VENDA',
    'COMPRA',
    'BALANCO',
    'ESTORNO',
    'DEVOLUCAO_VENDA',
    'DEVOLUCAO_COMPRA'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

-- Migração: bancos já criados não têm o valor.
ALTER TYPE projeto_sistemas.origem_movimentacao_estoque_enum ADD VALUE IF NOT EXISTS 'DEVOLUCAO_COMPRA';

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.status_balanco_enum AS ENUM (
    'ABERTO',
    'FECHADO',
    'CANCELADO'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

-- Motivo tipado de toda saída manual e de toda divergência do balanço (ADR 0006).
DO $$
BEGIN
  CREATE TYPE projeto_sistemas.motivo_movimentacao_estoque_enum AS ENUM (
    'PERDA_AVARIA',
    'FURTO_ROUBO',
    'VENCIMENTO',
    'ERRO_DE_CONTAGEM',
    'OUTROS'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;
