SET search_path TO projeto_sistemas;

-- Vínculo do código do produto no fornecedor (cProd do XML) com o SKU interno.
CREATE TABLE IF NOT EXISTS fornecedores_skus (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  fornecedor_id INTEGER NOT NULL,
  codigo_produto VARCHAR(60) NOT NULL,
  sku VARCHAR(50) NOT NULL,
  CONSTRAINT fornecedores_skus_fornecedor_fk FOREIGN KEY (fornecedor_id) REFERENCES fornecedores (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT fornecedores_skus_sku_fk FOREIGN KEY (sku) REFERENCES skus (sku) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT fornecedores_skus_unique UNIQUE (fornecedor_id, codigo_produto),
  CONSTRAINT fornecedores_skus_codigo_not_empty_ck CHECK (LENGTH(TRIM(codigo_produto)) > 0)
);
