SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS nfes_informacoes_adicionais (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL UNIQUE,
  informacoes_fisco TEXT,
  informacoes_complementares TEXT,
  CONSTRAINT nfes_informacoes_adicionais_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);
