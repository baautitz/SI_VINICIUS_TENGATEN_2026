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

CREATE TABLE IF NOT EXISTS contas_pagar (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  fornecedor_id INTEGER NOT NULL,
  nfe_id INTEGER,
  descricao VARCHAR(255) NOT NULL,
  data_emissao DATE,
  data_vencimento DATE,
  valor_original NUMERIC(14, 2) NOT NULL,
  valor_saldo NUMERIC(14, 2) NOT NULL,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  condicao_pagamento_id INTEGER,
  observacao TEXT,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  CONSTRAINT contas_pagar_fornecedor_fk
    FOREIGN KEY (fornecedor_id)
    REFERENCES fornecedores (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_condicao_pagamento_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_valor_original_ck
    CHECK (valor_original >= 0),
  CONSTRAINT contas_pagar_valor_saldo_ck
    CHECK (valor_saldo >= 0)
);

CREATE TABLE IF NOT EXISTS contas_pagar_parcelas (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  conta_pagar_id INTEGER NOT NULL,
  numero_parcela INTEGER NOT NULL,
  data_vencimento DATE,
  valor_parcela NUMERIC(14, 2) NOT NULL,
  valor_pago NUMERIC(14, 2) NOT NULL DEFAULT 0,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  CONSTRAINT contas_pagar_parcelas_conta_fk
    FOREIGN KEY (conta_pagar_id)
    REFERENCES contas_pagar (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_parcelas_numero_unique
    UNIQUE (conta_pagar_id, numero_parcela),
  CONSTRAINT contas_pagar_parcelas_numero_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT contas_pagar_parcelas_valor_parcela_ck
    CHECK (valor_parcela >= 0),
  CONSTRAINT contas_pagar_parcelas_valor_pago_ck
    CHECK (valor_pago >= 0)
);

CREATE TABLE IF NOT EXISTS contas_receber (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  cliente_id INTEGER NOT NULL,
  venda_id INTEGER,
  nfe_id INTEGER,
  descricao VARCHAR(255) NOT NULL,
  data_emissao DATE,
  data_vencimento DATE,
  valor_original NUMERIC(14, 2) NOT NULL,
  valor_saldo NUMERIC(14, 2) NOT NULL,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  condicao_pagamento_id INTEGER,
  observacao TEXT,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  CONSTRAINT contas_receber_cliente_fk
    FOREIGN KEY (cliente_id)
    REFERENCES clientes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_venda_fk
    FOREIGN KEY (venda_id)
    REFERENCES vendas (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_condicao_pagamento_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_valor_original_ck
    CHECK (valor_original >= 0),
  CONSTRAINT contas_receber_valor_saldo_ck
    CHECK (valor_saldo >= 0)
);

CREATE TABLE IF NOT EXISTS contas_receber_parcelas (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  conta_receber_id INTEGER NOT NULL,
  numero_parcela INTEGER NOT NULL,
  data_vencimento DATE,
  valor_parcela NUMERIC(14, 2) NOT NULL,
  valor_recebido NUMERIC(14, 2) NOT NULL DEFAULT 0,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  CONSTRAINT contas_receber_parcelas_conta_fk
    FOREIGN KEY (conta_receber_id)
    REFERENCES contas_receber (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_parcelas_numero_unique
    UNIQUE (conta_receber_id, numero_parcela),
  CONSTRAINT contas_receber_parcelas_numero_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT contas_receber_parcelas_valor_parcela_ck
    CHECK (valor_parcela >= 0),
  CONSTRAINT contas_receber_parcelas_valor_recebido_ck
    CHECK (valor_recebido >= 0)
);
