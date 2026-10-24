SET search_path TO projeto_sistemas;

-- Quanto cada devolução abateu de cada parcela a receber; estornado_em marca o registro sem apagá-lo.
CREATE TABLE IF NOT EXISTS devolucao_abatimentos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  devolucao_id INTEGER NOT NULL,
  conta_receber_id INTEGER NOT NULL,
  valor NUMERIC(14, 2) NOT NULL,
  estornado_em TIMESTAMP WITH TIME ZONE,
  CONSTRAINT devolucao_abatimentos_devolucao_fk
    FOREIGN KEY (devolucao_id)
    REFERENCES devolucoes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucao_abatimentos_conta_receber_fk
    FOREIGN KEY (conta_receber_id)
    REFERENCES contas_receber (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucao_abatimentos_valor_ck
    CHECK (valor > 0)
);

CREATE INDEX IF NOT EXISTS devolucao_abatimentos_devolucao_idx ON devolucao_abatimentos (devolucao_id);
CREATE INDEX IF NOT EXISTS devolucao_abatimentos_conta_receber_idx ON devolucao_abatimentos (conta_receber_id);
