SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS skus_atributos_valores (
  sku VARCHAR(50) NOT NULL,
  chave_id INTEGER NOT NULL,
  valor VARCHAR(150) NOT NULL,
  CONSTRAINT skus_atributos_valores_pk PRIMARY KEY (sku, chave_id),
  CONSTRAINT skus_atributos_valores_sku_fk FOREIGN KEY (sku) REFERENCES skus (sku) ON DELETE CASCADE,
  CONSTRAINT skus_atributos_valores_chave_fk FOREIGN KEY (chave_id) REFERENCES sku_atributos_chaves (id) ON DELETE CASCADE
);
