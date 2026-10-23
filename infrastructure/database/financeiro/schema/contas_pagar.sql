SET search_path TO projeto_sistemas;

-- Uma linha por parcela; numero_documento agrupa as parcelas de um mesmo lançamento.
CREATE TABLE IF NOT EXISTS contas_pagar (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  origem_tipo origem_titulo_financeiro_enum NOT NULL DEFAULT 'MANUAL',
  origem_id INTEGER,
  numero_documento VARCHAR(30) NOT NULL,
  numero_parcela INTEGER NOT NULL,
  total_parcelas INTEGER NOT NULL,
  fornecedor_id INTEGER,
  cliente_id INTEGER,
  transportadora_id INTEGER,
  descricao VARCHAR(150) NOT NULL,
  data_emissao DATE,
  data_vencimento DATE NOT NULL,
  valor NUMERIC(14, 2) NOT NULL,
  valor_pago NUMERIC(14, 2) NOT NULL DEFAULT 0,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  condicao_pagamento_id INTEGER,
  observacao TEXT,
  criado_em TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP WITH TIME ZONE,
  CONSTRAINT contas_pagar_fornecedor_fk
    FOREIGN KEY (fornecedor_id)
    REFERENCES fornecedores (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_cliente_fk
    FOREIGN KEY (cliente_id)
    REFERENCES clientes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_transportadora_fk
    FOREIGN KEY (transportadora_id)
    REFERENCES transportadoras (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  -- Contraparte: fornecedor (manual/compra), cliente (reembolso de devolução) ou transportadora (frete de terceiro); exatamente um.
  CONSTRAINT contas_pagar_contraparte_ck
    CHECK (num_nonnulls(fornecedor_id, cliente_id, transportadora_id) = 1),
  CONSTRAINT contas_pagar_condicao_pagamento_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  -- Título manual não tem documento; os demais precisam apontar para o documento de origem.
  CONSTRAINT contas_pagar_origem_ck
    CHECK (origem_tipo IN ('MANUAL', 'COMPRA', 'FRETE_COMPRA', 'DEVOLUCAO_VENDA') AND (origem_tipo = 'MANUAL' OR origem_id IS NOT NULL)),
  CONSTRAINT contas_pagar_numero_parcela_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT contas_pagar_total_parcelas_ck
    CHECK (total_parcelas >= numero_parcela),
  CONSTRAINT contas_pagar_valor_ck
    CHECK (valor >= 0),
  CONSTRAINT contas_pagar_valor_pago_ck
    CHECK (valor_pago >= 0 AND valor_pago <= valor)
);

-- Equivalente a UNIQUE NULLS NOT DISTINCT (que exige PG15+).
CREATE UNIQUE INDEX IF NOT EXISTS contas_pagar_parcela_uk
  ON contas_pagar (
    origem_tipo, COALESCE(origem_id, 0), numero_documento, numero_parcela,
    COALESCE(fornecedor_id, 0), COALESCE(cliente_id, 0), COALESCE(transportadora_id, 0)
  );

CREATE INDEX IF NOT EXISTS contas_pagar_origem_idx ON contas_pagar (origem_tipo, origem_id);
CREATE INDEX IF NOT EXISTS contas_pagar_numero_documento_idx ON contas_pagar (numero_documento);
CREATE INDEX IF NOT EXISTS contas_pagar_status_vencimento_idx ON contas_pagar (status, data_vencimento);
