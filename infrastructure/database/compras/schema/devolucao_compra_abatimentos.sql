SET search_path TO projeto_sistemas;

-- Quanto cada devolução de compra abateu de cada parcela a pagar; estornado_em marca o registro sem apagá-lo.
CREATE TABLE IF NOT EXISTS devolucao_compra_abatimentos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  devolucao_compra_id INTEGER NOT NULL,
  conta_pagar_id INTEGER NOT NULL,
  valor NUMERIC(14, 2) NOT NULL,
  estornado_em TIMESTAMP WITH TIME ZONE,
  CONSTRAINT devolucao_compra_abatimentos_devolucao_fk
    FOREIGN KEY (devolucao_compra_id)
    REFERENCES devolucoes_compra (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucao_compra_abatimentos_conta_pagar_fk
    FOREIGN KEY (conta_pagar_id)
    REFERENCES contas_pagar (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucao_compra_abatimentos_valor_ck
    CHECK (valor > 0)
);

CREATE INDEX IF NOT EXISTS devolucao_compra_abatimentos_devolucao_idx ON devolucao_compra_abatimentos (devolucao_compra_id);
CREATE INDEX IF NOT EXISTS devolucao_compra_abatimentos_conta_pagar_idx ON devolucao_compra_abatimentos (conta_pagar_id);
