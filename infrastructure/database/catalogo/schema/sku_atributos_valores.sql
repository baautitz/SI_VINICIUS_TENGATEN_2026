SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS sku_atributos_valores (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  chave_id INTEGER NOT NULL,
  valor VARCHAR(150) NOT NULL,
  CONSTRAINT sku_atributos_valores_unique UNIQUE (chave_id, valor),
  CONSTRAINT sku_atributos_valores_chave_fk 
    FOREIGN KEY (chave_id) 
    REFERENCES sku_atributos_chaves (id) 
    ON DELETE CASCADE
);
