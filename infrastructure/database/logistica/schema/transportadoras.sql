SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS transportadoras (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_razaosocial VARCHAR(150) NOT NULL,
  cpf_cnpj VARCHAR(20) NOT NULL,
  rg_ie VARCHAR(20),
  apelido_nomefantasia VARCHAR(100),
  endereco VARCHAR(150),
  bairro VARCHAR(100),
  telefone VARCHAR(20),
  email VARCHAR(254),
  rntrc VARCHAR(20),
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  observacao TEXT,
  CONSTRAINT transportadoras_nome_not_empty CHECK (LENGTH(TRIM(nome_razaosocial)) > 0),
  CONSTRAINT transportadoras_cpf_cnpj_not_empty CHECK (LENGTH(TRIM(cpf_cnpj)) > 0),
  CONSTRAINT transportadoras_cpf_cnpj_unique UNIQUE (cpf_cnpj)
);
