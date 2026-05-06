SET search_path TO projeto_sistemas;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.tipo_operacao_enum AS ENUM ('ENTRADA', 'SAIDA');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.status_nfe_enum AS ENUM ('RASCUNHO', 'EMITIDA', 'CANCELADA');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.modalidade_frete_enum AS ENUM (
    'EMITENTE',
    'DESTINATARIO',
    'TERCEIROS',
    'REMETENTE_PROPRIO',
    'DESTINATARIO_PROPRIO',
    'SEM_TRANSPORTE'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
  CREATE TYPE projeto_sistemas.indicador_pagamento_enum AS ENUM ('A_VISTA', 'A_PRAZO');
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS nfes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  chave_acesso VARCHAR(44) UNIQUE,
  numero INTEGER NOT NULL,
  serie SMALLINT NOT NULL,
  data_emissao TIMESTAMP NOT NULL,
  data_saida TIMESTAMP,
  emitente_id INTEGER NOT NULL,
  emitente_nome_razaosocial VARCHAR(255) NOT NULL,
  emitente_cpf_cnpj VARCHAR(40) NOT NULL,
  emitente_apelido_nomefantasia VARCHAR(255),
  emitente_endereco VARCHAR(255),
  emitente_bairro VARCHAR(120),
  emitente_telefone VARCHAR(45),
  emitente_email VARCHAR(320),
  cliente_id INTEGER NOT NULL,
  cliente_nome_razaosocial VARCHAR(255) NOT NULL,
  cliente_cpf_cnpj VARCHAR(40) NOT NULL,
  cliente_apelido_nomefantasia VARCHAR(255),
  cliente_endereco VARCHAR(255),
  cliente_bairro VARCHAR(120),
  cliente_telefone VARCHAR(45),
  cliente_email VARCHAR(320),
  tipo_operacao tipo_operacao_enum NOT NULL,
  status_nfe status_nfe_enum NOT NULL DEFAULT 'RASCUNHO',
  transportadora_id INTEGER,
  valor_produtos NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_desconto NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_frete NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_seguro NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_outras_despesas NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_total NUMERIC(14, 2) NOT NULL DEFAULT 0,
  CONSTRAINT nfes_emitente_fk FOREIGN KEY (emitente_id) REFERENCES emitentes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_emitente_snapshot_fk FOREIGN KEY (emitente_id, emitente_cpf_cnpj, emitente_nome_razaosocial) REFERENCES emitentes (id, cpf_cnpj, nome_razaosocial) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_cliente_fk FOREIGN KEY (cliente_id) REFERENCES clientes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_cliente_snapshot_fk FOREIGN KEY (cliente_id, cliente_cpf_cnpj, cliente_nome_razaosocial) REFERENCES clientes (id, cpf_cnpj, nome_razaosocial) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_transportadora_fk FOREIGN KEY (transportadora_id) REFERENCES transportadoras (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_emitente_serie_numero_unique UNIQUE (emitente_id, serie, numero),
  CONSTRAINT nfes_emitente_nome_not_empty_ck CHECK (LENGTH(TRIM(emitente_nome_razaosocial)) > 0),
  CONSTRAINT nfes_emitente_cpf_cnpj_not_empty_ck CHECK (LENGTH(TRIM(emitente_cpf_cnpj)) > 0),
  CONSTRAINT nfes_cliente_nome_not_empty_ck CHECK (LENGTH(TRIM(cliente_nome_razaosocial)) > 0),
  CONSTRAINT nfes_cliente_cpf_cnpj_not_empty_ck CHECK (LENGTH(TRIM(cliente_cpf_cnpj)) > 0),
  CONSTRAINT nfes_chave_acesso_fluxo_ck CHECK (
    (status_nfe = 'RASCUNHO' AND chave_acesso IS NULL)
    OR (status_nfe IN ('EMITIDA', 'CANCELADA') AND chave_acesso ~ '^[0-9]{44}$')
  ),
  CONSTRAINT nfes_valor_produtos_ck CHECK (valor_produtos >= 0),
  CONSTRAINT nfes_valor_desconto_ck CHECK (valor_desconto >= 0),
  CONSTRAINT nfes_valor_frete_ck CHECK (valor_frete >= 0),
  CONSTRAINT nfes_valor_seguro_ck CHECK (valor_seguro >= 0),
  CONSTRAINT nfes_valor_outras_ck CHECK (valor_outras_despesas >= 0),
  CONSTRAINT nfes_valor_total_ck CHECK (valor_total >= 0)
);

CREATE TABLE IF NOT EXISTS nfes_informacoes_adicionais (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL UNIQUE,
  informacoes_fisco TEXT,
  informacoes_complementares TEXT,
  CONSTRAINT nfes_informacoes_adicionais_nfe_fk FOREIGN KEY (nfe_id) REFERENCES nfes (id) ON DELETE NO ACTION ON UPDATE NO ACTION
);

CREATE TABLE IF NOT EXISTS nfes_transportes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL UNIQUE,
  modalidade_frete modalidade_frete_enum NOT NULL,
  transportadora_nome_razaosocial VARCHAR(255),
  transportadora_cpf_cnpj VARCHAR(40),
  veiculo_id INTEGER,
  veiculo_placa VARCHAR(10),
  veiculo_uf CHAR(2),
  veiculo_rntrc VARCHAR(20),
  quantidade_volumes INTEGER,
  especie_volume VARCHAR(60),
  marca_volume VARCHAR(60),
  numeracao_volume VARCHAR(60),
  peso_bruto NUMERIC(14, 3),
  peso_liquido NUMERIC(14, 3),
  CONSTRAINT nfes_transportes_nfe_fk FOREIGN KEY (nfe_id) REFERENCES nfes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_transportes_veiculo_fk FOREIGN KEY (veiculo_id) REFERENCES veiculos (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_transportes_transportadora_nome_not_empty_ck CHECK (transportadora_nome_razaosocial IS NULL OR LENGTH(TRIM(transportadora_nome_razaosocial)) > 0),
  CONSTRAINT nfes_transportes_transportadora_cpf_cnpj_not_empty_ck CHECK (transportadora_cpf_cnpj IS NULL OR LENGTH(TRIM(transportadora_cpf_cnpj)) > 0),
  CONSTRAINT nfes_transportes_veiculo_uf_formato_ck CHECK (veiculo_uf IS NULL OR veiculo_uf ~ '^[A-Z]{2}$'),
  CONSTRAINT nfes_transportes_qtde_volumes_ck CHECK (quantidade_volumes IS NULL OR quantidade_volumes >= 0),
  CONSTRAINT nfes_transportes_peso_bruto_ck CHECK (peso_bruto IS NULL OR peso_bruto >= 0),
  CONSTRAINT nfes_transportes_peso_liquido_ck CHECK (peso_liquido IS NULL OR peso_liquido >= 0)
);

CREATE TABLE IF NOT EXISTS nfes_produtos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL,
  numero_item INTEGER NOT NULL,
  sku_id INTEGER NOT NULL,
  descricao_item VARCHAR(255) NOT NULL,
  unidade_medida_id INTEGER NOT NULL,
  quantidade NUMERIC(14, 4) NOT NULL,
  valor_unitario NUMERIC(14, 4) NOT NULL,
  valor_desconto NUMERIC(14, 4) NOT NULL DEFAULT 0,
  valor_total NUMERIC(14, 4) NOT NULL,
  CONSTRAINT nfes_produtos_nfe_fk FOREIGN KEY (nfe_id) REFERENCES nfes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_produtos_sku_fk FOREIGN KEY (sku_id) REFERENCES skus (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_produtos_unidade_fk FOREIGN KEY (unidade_medida_id) REFERENCES unidades_medida (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_produtos_numero_item_unique UNIQUE (nfe_id, numero_item),
  CONSTRAINT nfes_produtos_numero_item_ck CHECK (numero_item > 0),
  CONSTRAINT nfes_produtos_quantidade_ck CHECK (quantidade > 0),
  CONSTRAINT nfes_produtos_valor_unitario_ck CHECK (valor_unitario >= 0),
  CONSTRAINT nfes_produtos_valor_desconto_ck CHECK (valor_desconto >= 0),
  CONSTRAINT nfes_produtos_valor_total_ck CHECK (valor_total >= 0)
);

CREATE TABLE IF NOT EXISTS nfes_pagamentos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  nfe_id INTEGER NOT NULL,
  metodo_pagamento_id INTEGER NOT NULL,
  indicador_pagamento indicador_pagamento_enum NOT NULL,
  valor_pagamento NUMERIC(14, 2) NOT NULL,
  CONSTRAINT nfes_pagamentos_nfe_fk FOREIGN KEY (nfe_id) REFERENCES nfes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_pagamentos_metodo_fk FOREIGN KEY (metodo_pagamento_id) REFERENCES metodos_pagamento (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_pagamentos_valor_ck CHECK (valor_pagamento >= 0)
);
