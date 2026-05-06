SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS fornecedores (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_razaosocial VARCHAR(255) NOT NULL,
  cpf_cnpj VARCHAR(40) NOT NULL,
  rg_ie VARCHAR(30),
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
