SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS contas_pagar (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  fornecedor_id INTEGER,
  cliente_id INTEGER,
  origem_tipo origem_titulo_financeiro_enum NOT NULL DEFAULT 'MANUAL',
  origem_id INTEGER,
  descricao VARCHAR(150) NOT NULL,
  data_emissao DATE,
  data_vencimento DATE,
  valor_original NUMERIC(14, 2) NOT NULL,
  valor_saldo NUMERIC(14, 2) NOT NULL,
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
  -- Contraparte: fornecedor (manual/compra) ou cliente (reembolso de devolução), nunca ambos.
  CONSTRAINT contas_pagar_contraparte_ck
    CHECK ((fornecedor_id IS NULL) <> (cliente_id IS NULL)),
  CONSTRAINT contas_pagar_condicao_pagamento_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  -- Título manual não tem documento; os demais precisam apontar para o documento de origem.
  CONSTRAINT contas_pagar_origem_ck
    CHECK (origem_tipo IN ('MANUAL', 'COMPRA', 'DEVOLUCAO_VENDA') AND (origem_tipo = 'MANUAL' OR origem_id IS NOT NULL)),
  CONSTRAINT contas_pagar_valor_original_ck
    CHECK (valor_original >= 0),
  CONSTRAINT contas_pagar_valor_saldo_ck
    CHECK (valor_saldo >= 0)
);
