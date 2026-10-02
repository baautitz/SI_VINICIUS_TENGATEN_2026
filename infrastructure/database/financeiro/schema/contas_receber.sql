SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS contas_receber (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  cliente_id INTEGER NOT NULL,
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
  CONSTRAINT contas_receber_cliente_fk
    FOREIGN KEY (cliente_id)
    REFERENCES clientes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_condicao_pagamento_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  -- Título manual não tem documento; os demais precisam apontar para o documento de origem.
  CONSTRAINT contas_receber_origem_ck
    CHECK (origem_tipo IN ('MANUAL', 'VENDA') AND (origem_tipo = 'MANUAL' OR origem_id IS NOT NULL)),
  CONSTRAINT contas_receber_valor_original_ck
    CHECK (valor_original >= 0),
  CONSTRAINT contas_receber_valor_saldo_ck
    CHECK (valor_saldo >= 0)
);
