SET search_path TO projeto_sistemas;


CREATE TABLE IF NOT EXISTS clientes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_razaosocial VARCHAR(255) NOT NULL,
  cpf_cnpj VARCHAR(40) NOT NULL,
  rg_ie VARCHAR(30),
  apelido_nomefantasia VARCHAR(255),
  endereco VARCHAR(255),
  bairro VARCHAR(120),
  telefone VARCHAR(45),
  email VARCHAR(320),
  limite_credito NUMERIC(14, 2) NOT NULL DEFAULT 0,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  observacao TEXT,
  CONSTRAINT clientes_nome_not_empty CHECK (LENGTH(TRIM(nome_razaosocial)) > 0),
  CONSTRAINT clientes_cpf_cnpj_not_empty CHECK (LENGTH(TRIM(cpf_cnpj)) > 0),
  CONSTRAINT clientes_cpf_cnpj_unique UNIQUE (cpf_cnpj),
  CONSTRAINT clientes_id_cpf_nome_unique UNIQUE (id, cpf_cnpj, nome_razaosocial),
  CONSTRAINT clientes_limite_credito_ck
    CHECK (limite_credito >= 0)
);

CREATE TABLE IF NOT EXISTS emitentes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_razaosocial VARCHAR(255) NOT NULL,
  cpf_cnpj VARCHAR(40) NOT NULL,
  apelido_nomefantasia VARCHAR(255),
  endereco VARCHAR(255),
  bairro VARCHAR(120),
  telefone VARCHAR(45),
  email VARCHAR(320),
  rg_ie VARCHAR(30),
  inscricao_municipal VARCHAR(30),
  regime_tributario VARCHAR(60),
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  observacao TEXT,
  CONSTRAINT emitentes_nome_not_empty CHECK (LENGTH(TRIM(nome_razaosocial)) > 0),
  CONSTRAINT emitentes_cpf_cnpj_not_empty CHECK (LENGTH(TRIM(cpf_cnpj)) > 0),
  CONSTRAINT emitentes_cpf_cnpj_unique UNIQUE (cpf_cnpj),
  CONSTRAINT emitentes_id_cpf_nome_unique UNIQUE (id, cpf_cnpj, nome_razaosocial)
);
