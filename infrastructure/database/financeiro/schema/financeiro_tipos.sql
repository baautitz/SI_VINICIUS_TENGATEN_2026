SET search_path TO projeto_sistemas;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.status_titulo_financeiro_enum AS ENUM (
    'ABERTO',
    'PARCIAL',
    'PAGO',
    'CANCELADO'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.origem_titulo_financeiro_enum AS ENUM (
    'MANUAL',
    'VENDA',
    'COMPRA',
    'DEVOLUCAO_VENDA'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;
