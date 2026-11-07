SET search_path TO projeto_sistemas;

-- Imutável: devolução de compra registrada não é alterada nem excluída; só pode ser estornada (status ESTORNADA).
CREATE TABLE IF NOT EXISTS devolucoes_compra (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  compra_id INTEGER NOT NULL,
  data_devolucao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  motivo VARCHAR(500) NOT NULL,
  valor_total NUMERIC(14, 2) NOT NULL,
  status VARCHAR(10) NOT NULL DEFAULT 'ATIVA',
  estornado_em TIMESTAMP WITH TIME ZONE,
  motivo_estorno VARCHAR(255),
  CONSTRAINT devolucoes_compra_compra_fk
    FOREIGN KEY (compra_id)
    REFERENCES compras (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_compra_motivo_ck
    CHECK (LENGTH(TRIM(motivo)) >= 5),
  CONSTRAINT devolucoes_compra_valor_total_ck
    CHECK (valor_total >= 0),
  CONSTRAINT devolucoes_compra_status_ck
    CHECK (status IN ('ATIVA', 'ESTORNADA') AND ((status = 'ESTORNADA') = (estornado_em IS NOT NULL)))
);

CREATE INDEX IF NOT EXISTS devolucoes_compra_compra_idx ON devolucoes_compra (compra_id);

CREATE TABLE IF NOT EXISTS devolucoes_compra_itens (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  devolucao_compra_id INTEGER NOT NULL,
  compra_item_id INTEGER NOT NULL,
  sku VARCHAR(50) NOT NULL,
  quantidade NUMERIC(14, 4) NOT NULL,
  valor_unitario NUMERIC(14, 4) NOT NULL,
  custo_unitario NUMERIC(18, 10) NOT NULL DEFAULT 0,
  CONSTRAINT devolucoes_compra_itens_devolucao_fk
    FOREIGN KEY (devolucao_compra_id)
    REFERENCES devolucoes_compra (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_compra_itens_compra_item_fk
    FOREIGN KEY (compra_item_id)
    REFERENCES compras_itens (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_compra_itens_sku_fk
    FOREIGN KEY (sku)
    REFERENCES skus (sku)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_compra_itens_item_unique
    UNIQUE (devolucao_compra_id, compra_item_id),
  CONSTRAINT devolucoes_compra_itens_quantidade_ck
    CHECK (quantidade > 0),
  CONSTRAINT devolucoes_compra_itens_valor_unitario_ck
    CHECK (valor_unitario >= 0),
  CONSTRAINT devolucoes_compra_itens_custo_unitario_ck
    CHECK (custo_unitario >= 0)
);

CREATE INDEX IF NOT EXISTS devolucoes_compra_itens_compra_item_idx ON devolucoes_compra_itens (compra_item_id);
