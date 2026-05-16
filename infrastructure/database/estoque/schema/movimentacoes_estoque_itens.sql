SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS movimentacoes_estoque_itens (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  movimentacao_estoque_id INTEGER NOT NULL,
  sku VARCHAR(50) NOT NULL,
  quantidade NUMERIC(14, 4) NOT NULL,
  custo_unitario NUMERIC(14, 4) NOT NULL DEFAULT 0,
  CONSTRAINT movimentacoes_estoque_itens_mov_fk
    FOREIGN KEY (movimentacao_estoque_id)
    REFERENCES movimentacoes_estoque (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT movimentacoes_estoque_itens_sku_fk
    FOREIGN KEY (sku)
    REFERENCES skus (sku)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT movimentacoes_estoque_itens_quantidade_ck
    CHECK (quantidade > 0),
  CONSTRAINT movimentacoes_estoque_itens_custo_ck
    CHECK (custo_unitario >= 0)
);
