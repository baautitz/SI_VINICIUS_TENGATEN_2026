SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS nfes_transportes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL UNIQUE,
  modalidade_frete modalidade_frete_enum NOT NULL,
  transportadora_nome_razaosocial VARCHAR(150),
  transportadora_cpf_cnpj VARCHAR(20),
  transportadora_rg_ie VARCHAR(20),
  veiculo_id INTEGER,
  veiculo_placa VARCHAR(10),
  veiculo_uf CHAR(2),
  veiculo_rntrc VARCHAR(20),
  quantidade_volumes INTEGER,
  especie_volume VARCHAR(50),
  marca_volume VARCHAR(50),
  numeracao_volume VARCHAR(50),
  peso_bruto NUMERIC(14, 3),
  peso_liquido NUMERIC(14, 3),
  CONSTRAINT nfes_transportes_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT nfes_transportes_veiculo_fk
    FOREIGN KEY (veiculo_id)
    REFERENCES veiculos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT nfes_transportes_transportadora_nome_not_empty_ck
    CHECK (transportadora_nome_razaosocial IS NULL OR LENGTH(TRIM(transportadora_nome_razaosocial)) > 0),
  CONSTRAINT nfes_transportes_transportadora_cpf_cnpj_not_empty_ck
    CHECK (transportadora_cpf_cnpj IS NULL OR LENGTH(TRIM(transportadora_cpf_cnpj)) > 0),
  CONSTRAINT nfes_transportes_veiculo_uf_formato_ck
    CHECK (veiculo_uf IS NULL OR veiculo_uf ~ '^[A-Z]{2}$'),
  CONSTRAINT nfes_transportes_qtde_volumes_ck
    CHECK (quantidade_volumes IS NULL OR quantidade_volumes >= 0),
  CONSTRAINT nfes_transportes_peso_bruto_ck
    CHECK (peso_bruto IS NULL OR peso_bruto >= 0),
  CONSTRAINT nfes_transportes_peso_liquido_ck
    CHECK (peso_liquido IS NULL OR peso_liquido >= 0)
);
