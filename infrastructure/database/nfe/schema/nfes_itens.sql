SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS nfes_itens (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL,
  numero_item INTEGER NOT NULL,
  sku_id INTEGER NOT NULL,
  descricao_item VARCHAR(255) NOT NULL,
  unidade_medida_id INTEGER NOT NULL,
  quantidade NUMERIC(14, 4) NOT NULL,
  valor_unitario NUMERIC(14, 4) NOT NULL,
  valor_desconto NUMERIC(14, 4) NOT NULL DEFAULT 0,
  valor_total NUMERIC(14, 4) NOT NULL,
  CONSTRAINT nfes_itens_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT nfes_itens_sku_fk
    FOREIGN KEY (sku_id)
    REFERENCES skus (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT nfes_itens_unidade_fk
    FOREIGN KEY (unidade_medida_id)
    REFERENCES unidades_medida (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT nfes_itens_numero_item_unique
    UNIQUE (nfe_id, numero_item),
  CONSTRAINT nfes_itens_numero_item_ck
    CHECK (numero_item > 0),
  CONSTRAINT nfes_itens_quantidade_ck
    CHECK (quantidade > 0),
  CONSTRAINT nfes_itens_valor_unitario_ck
    CHECK (valor_unitario >= 0),
  CONSTRAINT nfes_itens_valor_desconto_ck
    CHECK (valor_desconto >= 0),
  CONSTRAINT nfes_itens_valor_total_ck
    CHECK (valor_total >= 0)
);
