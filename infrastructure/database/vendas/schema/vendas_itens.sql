SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS vendas_itens (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  venda_id INTEGER NOT NULL,
  sku VARCHAR(50) NOT NULL,
  quantidade NUMERIC(14, 4) NOT NULL,
  valor_unitario NUMERIC(14, 4) NOT NULL,
  valor_desconto NUMERIC(14, 4) NOT NULL DEFAULT 0,
  valor_total NUMERIC(14, 4) NOT NULL,
  CONSTRAINT vendas_itens_venda_fk
    FOREIGN KEY (venda_id)
    REFERENCES vendas (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT vendas_itens_sku_fk
    FOREIGN KEY (sku)
    REFERENCES skus (sku)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT vendas_itens_quantidade_ck
    CHECK (quantidade > 0),
  CONSTRAINT vendas_itens_valor_unitario_ck
    CHECK (valor_unitario >= 0),
  CONSTRAINT vendas_itens_valor_desconto_ck
    CHECK (valor_desconto >= 0),
  CONSTRAINT vendas_itens_valor_total_ck
    CHECK (valor_total >= 0)
);
