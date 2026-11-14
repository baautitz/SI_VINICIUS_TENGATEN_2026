SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS nfes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  chave_acesso VARCHAR(44),
  numero INTEGER NOT NULL,
  serie SMALLINT NOT NULL,
  data_emissao TIMESTAMP WITH TIME ZONE NOT NULL,
  data_saida TIMESTAMP WITH TIME ZONE,
  emitente_id INTEGER NOT NULL,
  emitente_nome_razaosocial VARCHAR(150) NOT NULL,
  emitente_cpf_cnpj VARCHAR(14) NOT NULL,
  emitente_rg_ie VARCHAR(15),
  emitente_apelido_nomefantasia VARCHAR(100),
  emitente_endereco VARCHAR(150),
  emitente_bairro VARCHAR(100),
  emitente_telefone VARCHAR(20),
  emitente_email VARCHAR(254),
  -- Contraparte: cliente (SAIDA) ou fornecedor (ENTRADA); os snapshots cliente_* guardam os dados de quem for.
  cliente_id INTEGER,
  fornecedor_id INTEGER,
  cliente_nome_razaosocial VARCHAR(150) NOT NULL,
  cliente_cpf_cnpj VARCHAR(14) NOT NULL,
  cliente_rg_ie VARCHAR(15),
  cliente_apelido_nomefantasia VARCHAR(100),
  cliente_endereco VARCHAR(150),
  cliente_bairro VARCHAR(100),
  cliente_telefone VARCHAR(20),
  cliente_email VARCHAR(254),
  tipo_operacao tipo_operacao_enum NOT NULL,
  status_nfe status_nfe_enum NOT NULL DEFAULT 'RASCUNHO',
  transportadora_id INTEGER,
  venda_id INTEGER,
  compra_id INTEGER,
  -- NF-e de devolução (e de estorno da devolução): emitida pela loja a partir de uma Devolução, referenciando a nota anterior.
  finalidade finalidade_nfe_enum NOT NULL DEFAULT 'NORMAL',
  nfe_referenciada_id INTEGER,
  devolucao_id INTEGER,
  devolucao_compra_id INTEGER,
  -- NF-e de baixa de estoque (e de estorno da baixa): emitida pela loja para si mesma, vinculada à Movimentação.
  movimentacao_estoque_id INTEGER,
  justificativa_cancelamento VARCHAR(255),
  data_cancelamento TIMESTAMP WITH TIME ZONE,
  -- XML original da NF-e de entrada importada, guardado sem alteração.
  xml_documento TEXT,
  valor_produtos NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_desconto NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_frete NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_seguro NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_outras_despesas NUMERIC(14, 2) NOT NULL DEFAULT 0,
  valor_total NUMERIC(14, 2) NOT NULL DEFAULT 0,
  CONSTRAINT nfes_emitente_fk FOREIGN KEY (emitente_id) REFERENCES emitentes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_cliente_fk FOREIGN KEY (cliente_id) REFERENCES clientes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_fornecedor_fk FOREIGN KEY (fornecedor_id) REFERENCES fornecedores (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_transportadora_fk FOREIGN KEY (transportadora_id) REFERENCES transportadoras (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
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
  CONSTRAINT nfes_valor_total_ck CHECK (valor_total >= 0),
  CONSTRAINT nfes_venda_fk FOREIGN KEY (venda_id) REFERENCES vendas (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_compra_fk FOREIGN KEY (compra_id) REFERENCES compras (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_nfe_referenciada_fk FOREIGN KEY (nfe_referenciada_id) REFERENCES nfes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_devolucao_fk FOREIGN KEY (devolucao_id) REFERENCES devolucoes (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_devolucao_compra_fk FOREIGN KEY (devolucao_compra_id) REFERENCES devolucoes_compra (id) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT nfes_movimentacao_estoque_fk FOREIGN KEY (movimentacao_estoque_id) REFERENCES movimentacoes_estoque (id) ON DELETE NO ACTION ON UPDATE NO ACTION
);

-- Migração de bancos já criados (CREATE TABLE IF NOT EXISTS não altera tabela existente).
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS fornecedor_id INTEGER;
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS compra_id INTEGER;
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS justificativa_cancelamento VARCHAR(255);
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS data_cancelamento TIMESTAMP WITH TIME ZONE;
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS xml_documento TEXT;
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS finalidade finalidade_nfe_enum NOT NULL DEFAULT 'NORMAL';
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS nfe_referenciada_id INTEGER;
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS devolucao_id INTEGER;
ALTER TABLE nfes ADD COLUMN IF NOT EXISTS devolucao_compra_id INTEGER;
ALTER TABLE nfes ALTER COLUMN cliente_id DROP NOT NULL;
ALTER TABLE nfes DROP CONSTRAINT IF EXISTS nfes_emitente_serie_numero_unique;
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'nfes_fornecedor_fk') THEN
    ALTER TABLE nfes ADD CONSTRAINT nfes_fornecedor_fk FOREIGN KEY (fornecedor_id) REFERENCES fornecedores (id);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'nfes_compra_fk') THEN
    ALTER TABLE nfes ADD CONSTRAINT nfes_compra_fk FOREIGN KEY (compra_id) REFERENCES compras (id);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'nfes_nfe_referenciada_fk') THEN
    ALTER TABLE nfes ADD CONSTRAINT nfes_nfe_referenciada_fk FOREIGN KEY (nfe_referenciada_id) REFERENCES nfes (id);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'nfes_devolucao_fk') THEN
    ALTER TABLE nfes ADD CONSTRAINT nfes_devolucao_fk FOREIGN KEY (devolucao_id) REFERENCES devolucoes (id);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'nfes_devolucao_compra_fk') THEN
    ALTER TABLE nfes ADD CONSTRAINT nfes_devolucao_compra_fk FOREIGN KEY (devolucao_compra_id) REFERENCES devolucoes_compra (id);
  END IF;
END $$;

-- Recriado a cada carga: o CHECK antigo exigia venda_id na SAIDA (e era NOT VALID, reavaliado no UPDATE de cancelar).
ALTER TABLE nfes DROP CONSTRAINT IF EXISTS nfes_origem_ck;
ALTER TABLE nfes ADD CONSTRAINT nfes_origem_ck CHECK (
  (
    finalidade = 'NORMAL' AND nfe_referenciada_id IS NULL AND devolucao_id IS NULL AND devolucao_compra_id IS NULL AND movimentacao_estoque_id IS NULL
    AND (
      (tipo_operacao = 'SAIDA' AND compra_id IS NULL AND cliente_id IS NOT NULL AND fornecedor_id IS NULL)
      OR (tipo_operacao = 'ENTRADA' AND compra_id IS NOT NULL AND venda_id IS NULL AND fornecedor_id IS NOT NULL AND cliente_id IS NULL)
    )
  )
  OR (
    -- Devolução de venda tem o cliente como contraparte; devolução de compra, o fornecedor. Nunca ligada a venda/compra.
    finalidade IN ('DEVOLUCAO', 'ESTORNO_DEVOLUCAO') AND nfe_referenciada_id IS NOT NULL AND venda_id IS NULL AND compra_id IS NULL AND movimentacao_estoque_id IS NULL
    AND (
      (cliente_id IS NOT NULL AND fornecedor_id IS NULL AND devolucao_id IS NOT NULL AND devolucao_compra_id IS NULL)
      OR (fornecedor_id IS NOT NULL AND cliente_id IS NULL AND devolucao_compra_id IS NOT NULL AND devolucao_id IS NULL)
    )
  )
  OR (
    -- Baixa de estoque: a loja é a destinatária (sem cliente nem fornecedor), ligada só à Movimentação.
    -- A baixa é saída e não referencia nota; o estorno da baixa é entrada e referencia a nota de baixa.
    finalidade IN ('BAIXA_ESTOQUE', 'ESTORNO_BAIXA_ESTOQUE') AND movimentacao_estoque_id IS NOT NULL
    AND venda_id IS NULL AND compra_id IS NULL AND cliente_id IS NULL AND fornecedor_id IS NULL
    AND devolucao_id IS NULL AND devolucao_compra_id IS NULL
    AND (
      (finalidade = 'BAIXA_ESTOQUE' AND tipo_operacao = 'SAIDA' AND nfe_referenciada_id IS NULL)
      OR (finalidade = 'ESTORNO_BAIXA_ESTOQUE' AND tipo_operacao = 'ENTRADA' AND nfe_referenciada_id IS NOT NULL)
    )
  )
);

-- Numeração é nossa só na SAIDA; na ENTRADA vale a chave de acesso original, única entre as notas não canceladas
-- (compra estornada cancela a NF-e de entrada e libera a reimportação do mesmo XML).
ALTER TABLE nfes DROP CONSTRAINT IF EXISTS nfes_chave_acesso_key;
CREATE UNIQUE INDEX IF NOT EXISTS nfes_chave_acesso_ativa_uq
  ON nfes (chave_acesso) WHERE status_nfe <> 'CANCELADA';
-- A numeração também vale para as notas de devolução/estorno, que a loja emite mesmo no sentido de entrada.
DROP INDEX IF EXISTS nfes_saida_emitente_serie_numero_uq;
CREATE UNIQUE INDEX IF NOT EXISTS nfes_saida_emitente_serie_numero_uq
  ON nfes (emitente_id, serie, numero) WHERE tipo_operacao = 'SAIDA' OR finalidade <> 'NORMAL';
CREATE UNIQUE INDEX IF NOT EXISTS nfes_devolucao_finalidade_uq
  ON nfes (devolucao_id, finalidade) WHERE devolucao_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS nfes_devolucao_compra_finalidade_uq
  ON nfes (devolucao_compra_id, finalidade) WHERE devolucao_compra_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS nfes_movimentacao_estoque_finalidade_uq
  ON nfes (movimentacao_estoque_id, finalidade) WHERE movimentacao_estoque_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS nfes_venda_emitida_uq
  ON nfes (venda_id) WHERE status_nfe = 'EMITIDA';
CREATE UNIQUE INDEX IF NOT EXISTS nfes_compra_uq
  ON nfes (compra_id) WHERE compra_id IS NOT NULL;
