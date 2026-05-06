SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS cidades (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  cidade VARCHAR(255) NOT NULL,
  ddd SMALLINT NOT NULL,
  estado_id INTEGER NOT NULL,
  CONSTRAINT cidades_estado_fk
    FOREIGN KEY (estado_id)
    REFERENCES estados (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT unique_cidade_por_estado UNIQUE (cidade, estado_id)
);
