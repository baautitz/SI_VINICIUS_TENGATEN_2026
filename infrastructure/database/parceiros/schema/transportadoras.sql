SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS transportadoras (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  tipo_pessoa tipo_pessoa NOT NULL,
  nome_razaosocial VARCHAR(150) NOT NULL,
  cpf_cnpj VARCHAR(20) NOT NULL,
  rg_ie VARCHAR(20),
  apelido_nomefantasia VARCHAR(100),
  logradouro VARCHAR(150),
  numero VARCHAR(20),
  sexo CHAR(1),
  data_nascimento DATE,
  bairro_id INTEGER,
  nacionalidade_id INTEGER NOT NULL,
  telefone VARCHAR(20),
  email VARCHAR(254),
  rntrc VARCHAR(20),
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  criado_em TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP WITH TIME ZONE,
  observacao TEXT,
  CONSTRAINT transportadoras_bairro_fk FOREIGN KEY (bairro_id) REFERENCES bairros (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT transportadoras_nacionalidade_fk FOREIGN KEY (nacionalidade_id) REFERENCES paises (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT transportadoras_nome_not_empty CHECK (LENGTH(TRIM(nome_razaosocial)) > 0),
  CONSTRAINT transportadoras_cpf_cnpj_not_empty CHECK (LENGTH(TRIM(cpf_cnpj)) > 0),
  CONSTRAINT transportadoras_cpf_cnpj_unique UNIQUE (cpf_cnpj),
  CONSTRAINT transportadoras_sexo_ck
    CHECK (sexo IS NULL OR (tipo_pessoa = 'FISICA' AND sexo IN ('M', 'F', 'O')))
);
