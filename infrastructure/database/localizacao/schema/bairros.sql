SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS bairros (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  bairro VARCHAR(100) NOT NULL,
  cidade_id INTEGER NOT NULL,
  CONSTRAINT bairros_cidade_fk
    FOREIGN KEY (cidade_id)
    REFERENCES cidades (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT unique_bairro_por_cidade UNIQUE (bairro, cidade_id)
);
