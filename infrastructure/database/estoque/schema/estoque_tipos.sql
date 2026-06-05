SET search_path TO projeto_sistemas;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.tipo_movimentacao_estoque_enum AS ENUM (
    'ENTRADA',
    'SAIDA',
    'AJUSTE',
    'VENDA',
    'BALANCO'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.status_movimentacao_estoque_enum AS ENUM (
    'RASCUNHO',
    'CONFIRMADA',
    'CANCELADA'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

