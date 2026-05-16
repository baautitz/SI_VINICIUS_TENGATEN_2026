SET search_path TO projeto_sistemas;


CREATE TABLE IF NOT EXISTS unidades_medida (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  sigla VARCHAR(10) NOT NULL UNIQUE,
  descricao VARCHAR(100) NOT NULL,
  categoria VARCHAR(50) NOT NULL,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  CONSTRAINT unidades_medida_sigla_not_empty CHECK (LENGTH(TRIM(sigla)) > 0),
  CONSTRAINT unidades_medida_descricao_not_empty CHECK (LENGTH(TRIM(descricao)) > 0),
  CONSTRAINT unidades_medida_categoria_not_empty CHECK (LENGTH(TRIM(categoria)) > 0)
);
