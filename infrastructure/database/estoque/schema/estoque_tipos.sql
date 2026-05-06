SET search_path TO projeto_sistemas;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.tipo_movimentacao_estoque_enum AS ENUM (
    'ENTRADA',
    'SAIDA',
    'AJUSTE',
    'VENDA'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;
