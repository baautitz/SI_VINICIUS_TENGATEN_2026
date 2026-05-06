SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS estados (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  pais_id INTEGER NOT NULL,
  estado VARCHAR(255) NOT NULL,
  uf CHAR(2) NOT NULL,
  CONSTRAINT estados_pais_fk
    FOREIGN KEY (pais_id)
    REFERENCES paises (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT unique_uf_por_pais UNIQUE (pais_id, uf)
);
