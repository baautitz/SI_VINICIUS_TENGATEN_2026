SET search_path TO projeto_sistemas;


CREATE TABLE IF NOT EXISTS paises (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  ddi VARCHAR(5) NOT NULL,
  sigla_iso CHAR(3) NOT NULL UNIQUE,
  moeda CHAR(3) NOT NULL,
  simbolo_moeda VARCHAR(10) NOT NULL,
  pais VARCHAR(255) NOT NULL UNIQUE
);

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

CREATE TABLE IF NOT EXISTS bairros (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  bairro VARCHAR(255) NOT NULL,
  cidade_id INTEGER NOT NULL,
  CONSTRAINT bairros_cidade_fk
    FOREIGN KEY (cidade_id)
    REFERENCES cidades (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT unique_bairro_por_cidade UNIQUE (bairro, cidade_id)
);
