SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS devolucoes_itens (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  devolucao_id INTEGER NOT NULL,
  venda_item_id INTEGER NOT NULL,
  sku VARCHAR(50) NOT NULL,
  quantidade NUMERIC(14, 4) NOT NULL,
  valor_unitario NUMERIC(14, 4) NOT NULL,
  custo_unitario NUMERIC(14, 4) NOT NULL DEFAULT 0,
  CONSTRAINT devolucoes_itens_devolucao_fk
    FOREIGN KEY (devolucao_id)
    REFERENCES devolucoes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_itens_venda_item_fk
    FOREIGN KEY (venda_item_id)
    REFERENCES vendas_itens (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_itens_sku_fk
    FOREIGN KEY (sku)
    REFERENCES skus (sku)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_itens_item_unique
    UNIQUE (devolucao_id, venda_item_id),
  CONSTRAINT devolucoes_itens_quantidade_ck
    CHECK (quantidade > 0),
  CONSTRAINT devolucoes_itens_valor_unitario_ck
    CHECK (valor_unitario >= 0),
  CONSTRAINT devolucoes_itens_custo_unitario_ck
    CHECK (custo_unitario >= 0)
);

CREATE INDEX IF NOT EXISTS devolucoes_itens_venda_item_idx ON devolucoes_itens (venda_item_id);
