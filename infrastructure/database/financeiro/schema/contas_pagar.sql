SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS contas_pagar (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  fornecedor_id INTEGER,
  cliente_id INTEGER,
  transportadora_id INTEGER,
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
    CHECK (origem_tipo IN ('MANUAL', 'COMPRA', 'DEVOLUCAO_VENDA') AND (origem_tipo = 'MANUAL' OR origem_id IS NOT NULL)),
  CONSTRAINT contas_pagar_valor_original_ck
    CHECK (valor_original >= 0),
  CONSTRAINT contas_pagar_valor_saldo_ck
    CHECK (valor_saldo >= 0)
);

-- Migração de bancos já criados.
ALTER TABLE contas_pagar ADD COLUMN IF NOT EXISTS transportadora_id INTEGER;
ALTER TABLE contas_pagar DROP CONSTRAINT IF EXISTS contas_pagar_contraparte_ck;
ALTER TABLE contas_pagar ADD CONSTRAINT contas_pagar_contraparte_ck
  CHECK (num_nonnulls(fornecedor_id, cliente_id, transportadora_id) = 1);
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'contas_pagar_transportadora_fk') THEN
    ALTER TABLE contas_pagar ADD CONSTRAINT contas_pagar_transportadora_fk FOREIGN KEY (transportadora_id) REFERENCES transportadoras (id);
  END IF;
END $$;
