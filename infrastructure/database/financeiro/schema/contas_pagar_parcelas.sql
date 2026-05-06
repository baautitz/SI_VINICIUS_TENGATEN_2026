SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS contas_pagar_parcelas (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  conta_pagar_id INTEGER NOT NULL,
  numero_parcela INTEGER NOT NULL,
  data_vencimento DATE,
  valor_parcela NUMERIC(14, 2) NOT NULL,
  valor_pago NUMERIC(14, 2) NOT NULL DEFAULT 0,
  status status_titulo_financeiro_enum NOT NULL DEFAULT 'ABERTO',
  CONSTRAINT contas_pagar_parcelas_conta_fk
    FOREIGN KEY (conta_pagar_id)
    REFERENCES contas_pagar (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT contas_pagar_parcelas_numero_unique
    UNIQUE (conta_pagar_id, numero_parcela),
  CONSTRAINT contas_pagar_parcelas_numero_ck
    CHECK (numero_parcela > 0),
  CONSTRAINT contas_pagar_parcelas_valor_parcela_ck
    CHECK (valor_parcela >= 0),
  CONSTRAINT contas_pagar_parcelas_valor_pago_ck
    CHECK (valor_pago >= 0)
);
