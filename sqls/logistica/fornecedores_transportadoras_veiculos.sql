SET search_path TO projeto_sistemas;


CREATE TABLE IF NOT EXISTS fornecedores (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_razaosocial VARCHAR(255) NOT NULL,
  cpf_cnpj VARCHAR(40) NOT NULL,
  apelido_nomefantasia VARCHAR(255),
  endereco VARCHAR(255),
  bairro VARCHAR(120),
  telefone VARCHAR(45),
  email VARCHAR(320),
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  observacao TEXT,
  CONSTRAINT fornecedores_nome_not_empty CHECK (LENGTH(TRIM(nome_razaosocial)) > 0),
  CONSTRAINT fornecedores_cpf_cnpj_not_empty CHECK (LENGTH(TRIM(cpf_cnpj)) > 0),
  CONSTRAINT fornecedores_cpf_cnpj_unique UNIQUE (cpf_cnpj)
);

CREATE TABLE IF NOT EXISTS transportadoras (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_razaosocial VARCHAR(255) NOT NULL,
  cpf_cnpj VARCHAR(40) NOT NULL,
  apelido_nomefantasia VARCHAR(255),
  endereco VARCHAR(255),
  bairro VARCHAR(120),
  telefone VARCHAR(45),
  email VARCHAR(320),
  rntrc VARCHAR(20),
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  observacao TEXT,
  CONSTRAINT transportadoras_nome_not_empty CHECK (LENGTH(TRIM(nome_razaosocial)) > 0),
  CONSTRAINT transportadoras_cpf_cnpj_not_empty CHECK (LENGTH(TRIM(cpf_cnpj)) > 0),
  CONSTRAINT transportadoras_cpf_cnpj_unique UNIQUE (cpf_cnpj)
);

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
