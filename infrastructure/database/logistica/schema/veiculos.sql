SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS veiculos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  transportadora_id INTEGER,
  placa VARCHAR(10) NOT NULL,
  uf CHAR(2) NOT NULL,
  rntrc VARCHAR(20),
  renavam VARCHAR(20),
  tipo_veiculo VARCHAR(60),
  marca_modelo VARCHAR(120),
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  observacao TEXT,
  CONSTRAINT veiculos_transportadora_fk
    FOREIGN KEY (transportadora_id)
    REFERENCES transportadoras (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT veiculos_placa_not_empty CHECK (LENGTH(TRIM(placa)) > 0),
  CONSTRAINT veiculos_uf_formato_ck CHECK (uf ~ '^[A-Z]{2}$')
);
