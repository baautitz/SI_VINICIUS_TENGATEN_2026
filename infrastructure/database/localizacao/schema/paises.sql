SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS paises (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  ddi VARCHAR(5) NOT NULL,
  codigo_iso_pais CHAR(3) NOT NULL UNIQUE,
  codigo_iso_moeda CHAR(3) NOT NULL,
  simbolo_moeda VARCHAR(5) NOT NULL,
  pais VARCHAR(60) NOT NULL UNIQUE
);
