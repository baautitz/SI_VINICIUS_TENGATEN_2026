SET search_path TO projeto_sistemas;

-- Uma linha por parcela; numero_documento agrupa as parcelas de um mesmo lançamento.
CREATE TABLE IF NOT EXISTS contas_receber (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  origem_tipo origem_titulo_financeiro_enum NOT NULL DEFAULT 'MANUAL',
  origem_id INTEGER,
  numero_documento VARCHAR(30) NOT NULL,
  numero_parcela INTEGER NOT NULL,
  total_parcelas INTEGER NOT NULL,
  cliente_id INTEGER NOT NULL,
  descricao VARCHAR(150) NOT NULL,
  data_emissao DATE,
  data_vencimento DATE NOT NULL,
  valor NUMERIC(14, 2) NOT NULL,
  valor_pago NUMERIC(14, 2) NOT NULL DEFAULT 0,
  -- Baixa não monetária por devolução; valor original nunca muda.
  valor_abatido NUMERIC(14, 2) NOT NULL DEFAULT 0,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  condicao_pagamento_id INTEGER,
  observacao TEXT,
  criado_em TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  atualizado_em TIMESTAMP WITH TIME ZONE,
  -- Texto da pesquisa (sem acento/caixa), mantido por trigger; ver contas_receber_atualizar_texto_pesquisa.
  texto_pesquisa TEXT NOT NULL DEFAULT '',
  CONSTRAINT contas_receber_cliente_fk
    FOREIGN KEY (cliente_id)
    REFERENCES clientes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_condicao_pagamento_fk
    FOREIGN KEY (condicao_pagamento_id)
    REFERENCES condicoes_pagamentos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  -- Título manual não tem documento; os demais precisam apontar para o documento de origem.
  CONSTRAINT contas_receber_origem_ck
    CHECK (origem_tipo IN ('MANUAL', 'VENDA') AND (origem_tipo = 'MANUAL' OR origem_id IS NOT NULL)),
  CONSTRAINT contas_receber_numero_parcela_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT contas_receber_total_parcelas_ck
    CHECK (total_parcelas >= numero_parcela),
  CONSTRAINT contas_receber_valor_ck
    CHECK (valor >= 0),
  CONSTRAINT contas_receber_valor_pago_ck
    CHECK (valor_pago >= 0 AND valor_abatido >= 0 AND valor_pago + valor_abatido <= valor)
);

-- Equivalente a UNIQUE NULLS NOT DISTINCT (que exige PG15+).
CREATE UNIQUE INDEX IF NOT EXISTS contas_receber_parcela_uk
  ON contas_receber (origem_tipo, COALESCE(origem_id, 0), numero_documento, numero_parcela, cliente_id);

CREATE INDEX IF NOT EXISTS contas_receber_origem_idx ON contas_receber (origem_tipo, origem_id);
CREATE INDEX IF NOT EXISTS contas_receber_numero_documento_idx ON contas_receber (numero_documento);
CREATE INDEX IF NOT EXISTS contas_receber_status_vencimento_idx ON contas_receber (status, data_vencimento);

-- Texto pesquisável precomputado (mesmos campos que a pesquisa usava) para o índice trigram; refeito a cada
-- gravação do título e quando o cliente muda.
CREATE OR REPLACE FUNCTION contas_receber_atualizar_texto_pesquisa() RETURNS trigger AS $$
BEGIN
  NEW.texto_pesquisa := unaccent(lower(concat_ws(' ',
    NEW.descricao, NEW.numero_documento, NEW.observacao,
    (SELECT concat_ws(' ', c.nome_razaosocial, c.apelido_nomefantasia, c.cpf_cnpj) FROM clientes c WHERE c.id = NEW.cliente_id),
    NEW.valor::text, to_char(NEW.data_vencimento, 'DD/MM/YYYY'), to_char(NEW.data_emissao, 'DD/MM/YYYY'),
    NEW.status::text, NEW.origem_tipo::text)));
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS contas_receber_texto_pesquisa_tg ON contas_receber;
CREATE TRIGGER contas_receber_texto_pesquisa_tg
  BEFORE INSERT OR UPDATE ON contas_receber
  FOR EACH ROW EXECUTE FUNCTION contas_receber_atualizar_texto_pesquisa();

CREATE OR REPLACE FUNCTION contas_receber_refazer_texto_pesquisa() RETURNS trigger AS $$
BEGIN
  -- O UPDATE dispara o trigger BEFORE da própria conta, que recalcula o texto.
  UPDATE contas_receber SET texto_pesquisa = '' WHERE cliente_id = NEW.id;
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS clientes_contas_receber_texto_tg ON clientes;
CREATE TRIGGER clientes_contas_receber_texto_tg
  AFTER UPDATE OF nome_razaosocial, apelido_nomefantasia, cpf_cnpj ON clientes
  FOR EACH ROW EXECUTE FUNCTION contas_receber_refazer_texto_pesquisa();

CREATE INDEX IF NOT EXISTS contas_receber_texto_pesquisa_idx ON contas_receber USING gin (texto_pesquisa public.gin_trgm_ops);
