SET search_path TO projeto_sistemas;

-- Compra nasce já recebida (estoque + contas a pagar na mesma transação): não há status.
CREATE TABLE IF NOT EXISTS compras (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  emitente_id INTEGER NOT NULL,
  fornecedor_id INTEGER NOT NULL,
  data_compra TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  valor_nota NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_frete NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_seguro NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_outras_despesas NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_desconto NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_frete_terceiro NUMERIC(14, 2) NOT NULL DEFAULT 0,
  transportadora_id INTEGER,
  valor_total NUMERIC(14, 2) NOT NULL DEFAULT 0,
  observacao TEXT,
  condicao_pagamento_id INTEGER,
  CONSTRAINT compras_emitente_fk FOREIGN KEY (emitente_id) REFERENCES emitentes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT compras_fornecedor_fk FOREIGN KEY (fornecedor_id) REFERENCES fornecedores (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT compras_transportadora_fk FOREIGN KEY (transportadora_id) REFERENCES transportadoras (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT compras_condicao_pagamento_fk FOREIGN KEY (condicao_pagamento_id) REFERENCES condicoes_pagamentos (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT compras_valores_ck CHECK (
    valor_nota >= 0 AND valor_frete >= 0 AND valor_seguro >= 0 AND valor_outras_despesas >= 0
    AND valor_desconto >= 0 AND valor_frete_terceiro >= 0 AND valor_total >= 0
  ),
  CONSTRAINT compras_frete_terceiro_transportadora_ck CHECK (valor_frete_terceiro = 0 OR transportadora_id IS NOT NULL)
);
