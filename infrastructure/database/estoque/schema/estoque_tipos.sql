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
    'DEVOLUCAO_VENDA'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

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
