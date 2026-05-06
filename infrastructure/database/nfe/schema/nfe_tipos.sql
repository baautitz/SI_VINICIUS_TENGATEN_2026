SET search_path TO projeto_sistemas;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.tipo_operacao_enum AS ENUM ('ENTRADA', 'SAIDA');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.status_nfe_enum AS ENUM ('RASCUNHO', 'EMITIDA', 'CANCELADA');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.modalidade_frete_enum AS ENUM (
    'EMITENTE',
    'DESTINATARIO',
    'TERCEIROS',
    'REMETENTE_PROPRIO',
    'DESTINATARIO_PROPRIO',
    'SEM_TRANSPORTE'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.indicador_pagamento_enum AS ENUM ('A_VISTA', 'A_PRAZO');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;
