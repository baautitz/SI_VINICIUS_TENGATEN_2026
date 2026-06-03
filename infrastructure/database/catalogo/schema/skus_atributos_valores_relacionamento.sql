SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS skus_atributos_valores_relacionamento (
  sku VARCHAR(50) NOT NULL,
  valor_id INTEGER NOT NULL,
  CONSTRAINT skus_atributos_valores_relacionamento_pk PRIMARY KEY (sku, valor_id),
  CONSTRAINT skus_atributos_valores_relacionamento_sku_fk 
    FOREIGN KEY (sku) 
    REFERENCES skus (sku) 
    ON DELETE CASCADE,
  CONSTRAINT skus_atributos_valores_relacionamento_valor_fk 
    FOREIGN KEY (valor_id) 
    REFERENCES sku_atributos_valores (id) 
    ON DELETE CASCADE
);
