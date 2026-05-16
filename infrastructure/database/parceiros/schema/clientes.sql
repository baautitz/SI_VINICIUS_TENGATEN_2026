SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS clientes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nome_razaosocial VARCHAR(150) NOT NULL,
  cpf_cnpj VARCHAR(20) NOT NULL,
  rg_ie VARCHAR(20),
  apelido_nomefantasia VARCHAR(100),
  endereco VARCHAR(150),
  bairro_id INTEGER,
  telefone VARCHAR(20),
  email VARCHAR(254),
  limite_credito NUMERIC(14, 2) NOT NULL DEFAULT 0,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP,
  observacao TEXT,
  CONSTRAINT clientes_bairro_fk FOREIGN KEY (bairro_id) REFERENCES bairros (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT clientes_nome_not_empty CHECK (LENGTH(TRIM(nome_razaosocial)) > 0),
  CONSTRAINT clientes_cpf_cnpj_not_empty CHECK (LENGTH(TRIM(cpf_cnpj)) > 0),
  CONSTRAINT clientes_cpf_cnpj_unique UNIQUE (cpf_cnpj),
  CONSTRAINT clientes_id_cpf_nome_unique UNIQUE (id, cpf_cnpj, nome_razaosocial),
  CONSTRAINT clientes_limite_credito_ck
    CHECK (limite_credito >= 0)
);
