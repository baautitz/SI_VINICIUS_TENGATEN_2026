SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS contas_receber_parcelas (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  conta_receber_id INTEGER NOT NULL,
  numero_parcela INTEGER NOT NULL,
  data_vencimento DATE,
  valor_parcela NUMERIC(14, 2) NOT NULL,
  valor_recebido NUMERIC(14, 2) NOT NULL DEFAULT 0,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  CONSTRAINT contas_receber_parcelas_conta_fk
    FOREIGN KEY (conta_receber_id)
    REFERENCES contas_receber (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_receber_parcelas_numero_unique
    UNIQUE (conta_receber_id, numero_parcela),
  CONSTRAINT contas_receber_parcelas_numero_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT contas_receber_parcelas_valor_parcela_ck
    CHECK (valor_parcela >= 0),
  CONSTRAINT contas_receber_parcelas_valor_recebido_ck
    CHECK (valor_recebido >= 0)
);
