SET search_path TO projeto_sistemas;

-- Uma linha por parcela; numero_documento agrupa as parcelas de um mesmo lançamento.
CREATE TABLE IF NOT EXISTS contas_pagar (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  origem_tipo origem_titulo_financeiro_enum NOT NULL DEFAULT 'MANUAL',
  origem_id INTEGER,
  numero_documento VARCHAR(30) NOT NULL,
  numero_parcela INTEGER NOT NULL,
  total_parcelas INTEGER NOT NULL,
  fornecedor_id INTEGER,
  cliente_id INTEGER,
  transportadora_id INTEGER,
  descricao VARCHAR(150) NOT NULL,
  data_emissao DATE,
  data_vencimento DATE NOT NULL,
  valor NUMERIC(14, 2) NOT NULL,
  valor_pago NUMERIC(14, 2) NOT NULL DEFAULT 0,
  -- Baixa não monetária por devolução de compra; valor original nunca muda.
  valor_abatido NUMERIC(14, 2) NOT NULL DEFAULT 0,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  condicao_pagamento_id INTEGER,
  observacao TEXT,
  criado_em TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP WITH TIME ZONE,
  -- Texto da pesquisa (sem acento/caixa), mantido por trigger; ver contas_pagar_atualizar_texto_pesquisa.
  texto_pesquisa TEXT NOT NULL DEFAULT '',
  CONSTRAINT contas_pagar_fornecedor_fk
    FOREIGN KEY (fornecedor_id)
    REFERENCES fornecedores (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_cliente_fk
    FOREIGN KEY (cliente_id)
    REFERENCES clientes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_transportadora_fk
    FOREIGN KEY (transportadora_id)
    REFERENCES transportadoras (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  -- Contraparte: fornecedor (manual/compra), cliente (reembolso de devolução) ou transportadora (frete de terceiro); exatamente um.
  CONSTRAINT contas_pagar_contraparte_ck
    CHECK (num_nonnulls(fornecedor_id, cliente_id, transportadora_id) = 1),
  CONSTRAINT contas_pagar_condicao_pagamento_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  -- Título manual não tem documento; os demais precisam apontar para o documento de origem.
  CONSTRAINT contas_pagar_origem_ck
    CHECK (origem_tipo IN ('MANUAL', 'COMPRA', 'FRETE_COMPRA', 'DEVOLUCAO_VENDA') AND (origem_tipo = 'MANUAL' OR origem_id IS NOT NULL)),
  CONSTRAINT contas_pagar_numero_parcela_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT contas_pagar_total_parcelas_ck
    CHECK (total_parcelas >= numero_parcela),
  CONSTRAINT contas_pagar_valor_ck
    CHECK (valor >= 0),
  CONSTRAINT contas_pagar_valor_pago_ck
    CHECK (valor_pago >= 0 AND valor_abatido >= 0 AND valor_pago + valor_abatido <= valor)
);

-- Equivalente a UNIQUE NULLS NOT DISTINCT (que exige PG15+).
CREATE UNIQUE INDEX IF NOT EXISTS contas_pagar_parcela_uk
  ON contas_pagar (
    origem_tipo, COALESCE(origem_id, 0), numero_documento, numero_parcela,
    COALESCE(fornecedor_id, 0), COALESCE(cliente_id, 0), COALESCE(transportadora_id, 0)
  );

CREATE INDEX IF NOT EXISTS contas_pagar_origem_idx ON contas_pagar (origem_tipo, origem_id);
CREATE INDEX IF NOT EXISTS contas_pagar_numero_documento_idx ON contas_pagar (numero_documento);
CREATE INDEX IF NOT EXISTS contas_pagar_status_vencimento_idx ON contas_pagar (status, data_vencimento);

-- Texto pesquisável precomputado (mesmos campos que a pesquisa usava) para o índice trigram; refeito a cada
-- gravação do título e quando fornecedor, cliente ou transportadora mudam.
CREATE OR REPLACE FUNCTION contas_pagar_atualizar_texto_pesquisa() RETURNS trigger AS $$
BEGIN
  NEW.texto_pesquisa := unaccent(lower(concat_ws(' ',
    NEW.descricao, NEW.numero_documento, NEW.observacao,
    (SELECT concat_ws(' ', f.nome_razaosocial, f.apelido_nomefantasia, f.cpf_cnpj) FROM fornecedores f WHERE f.id = NEW.fornecedor_id),
    (SELECT concat_ws(' ', c.nome_razaosocial, c.cpf_cnpj) FROM clientes c WHERE c.id = NEW.cliente_id),
    (SELECT t.nome_razaosocial FROM transportadoras t WHERE t.id = NEW.transportadora_id),
    to_char(NEW.data_vencimento, 'DD/MM/YYYY'), to_char(NEW.data_emissao, 'DD/MM/YYYY'),
    CASE NEW.status WHEN 'ABERTO' THEN 'aberto' WHEN 'PARCIAL' THEN 'parcial' WHEN 'PAGO' THEN 'pago' WHEN 'CANCELADO' THEN 'cancelado' END,
    CASE NEW.origem_tipo WHEN 'MANUAL' THEN 'manual' WHEN 'VENDA' THEN 'venda' WHEN 'COMPRA' THEN 'compra'
      WHEN 'FRETE_COMPRA' THEN 'frete compra' WHEN 'DEVOLUCAO_VENDA' THEN 'devolucao venda' WHEN 'DEVOLUCAO_COMPRA' THEN 'devolucao compra' END)));
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS contas_pagar_texto_pesquisa_tg ON contas_pagar;
CREATE TRIGGER contas_pagar_texto_pesquisa_tg
  BEFORE INSERT OR UPDATE ON contas_pagar
  FOR EACH ROW EXECUTE FUNCTION contas_pagar_atualizar_texto_pesquisa();

CREATE OR REPLACE FUNCTION contas_pagar_refazer_texto_pesquisa() RETURNS trigger AS $$
BEGIN
  -- O UPDATE dispara o trigger BEFORE da própria conta, que recalcula o texto.
  IF TG_TABLE_NAME = 'fornecedores' THEN
    UPDATE contas_pagar SET texto_pesquisa = '' WHERE fornecedor_id = NEW.id;
  ELSIF TG_TABLE_NAME = 'clientes' THEN
    UPDATE contas_pagar SET texto_pesquisa = '' WHERE cliente_id = NEW.id;
  ELSE
    UPDATE contas_pagar SET texto_pesquisa = '' WHERE transportadora_id = NEW.id;
  END IF;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS fornecedores_contas_pagar_texto_tg ON fornecedores;
CREATE TRIGGER fornecedores_contas_pagar_texto_tg
  AFTER UPDATE OF nome_razaosocial, apelido_nomefantasia, cpf_cnpj ON fornecedores
  FOR EACH ROW EXECUTE FUNCTION contas_pagar_refazer_texto_pesquisa();

DROP TRIGGER IF EXISTS clientes_contas_pagar_texto_tg ON clientes;
CREATE TRIGGER clientes_contas_pagar_texto_tg
  AFTER UPDATE OF nome_razaosocial, cpf_cnpj ON clientes
  FOR EACH ROW EXECUTE FUNCTION contas_pagar_refazer_texto_pesquisa();

DROP TRIGGER IF EXISTS transportadoras_contas_pagar_texto_tg ON transportadoras;
CREATE TRIGGER transportadoras_contas_pagar_texto_tg
  AFTER UPDATE OF nome_razaosocial ON transportadoras
  FOR EACH ROW EXECUTE FUNCTION contas_pagar_refazer_texto_pesquisa();

-- O valor é pesquisado por igualdade (não entra no texto: trecho de dígitos seria ruído).
CREATE INDEX IF NOT EXISTS contas_pagar_valor_idx ON contas_pagar (valor);
CREATE INDEX IF NOT EXISTS contas_pagar_texto_pesquisa_idx ON contas_pagar USING gin (texto_pesquisa public.gin_trgm_ops);
