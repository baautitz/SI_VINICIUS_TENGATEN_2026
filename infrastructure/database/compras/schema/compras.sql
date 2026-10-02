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

-- Estorno: a compra permanece como registro, marcada como estornada (motivo, data e quem).
ALTER TABLE compras ADD COLUMN IF NOT EXISTS estornada_em TIMESTAMP WITH TIME ZONE;
ALTER TABLE compras ADD COLUMN IF NOT EXISTS motivo_estorno VARCHAR(255);
ALTER TABLE compras ADD COLUMN IF NOT EXISTS usuario_estorno_id INTEGER;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'compras_usuario_estorno_fk') THEN
    ALTER TABLE compras ADD CONSTRAINT compras_usuario_estorno_fk FOREIGN KEY (usuario_estorno_id) REFERENCES usuarios (id);
  END IF;
END $$;
ALTER TABLE compras DROP CONSTRAINT IF EXISTS compras_estorno_ck;
ALTER TABLE compras ADD CONSTRAINT compras_estorno_ck CHECK (
  (estornada_em IS NULL AND motivo_estorno IS NULL AND usuario_estorno_id IS NULL)
  OR (estornada_em IS NOT NULL AND LENGTH(TRIM(COALESCE(motivo_estorno, ''))) >= 5)
);
