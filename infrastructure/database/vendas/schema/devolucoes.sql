SET search_path TO projeto_sistemas;

-- Imutável: devolução registrada não é alterada nem excluída.
CREATE TABLE IF NOT EXISTS devolucoes (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  venda_id INTEGER NOT NULL,
  data_devolucao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  motivo VARCHAR(500) NOT NULL,
  valor_total NUMERIC(14, 2) NOT NULL,
  CONSTRAINT devolucoes_venda_fk
    FOREIGN KEY (venda_id)
    REFERENCES vendas (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT devolucoes_motivo_ck
    CHECK (LENGTH(TRIM(motivo)) >= 5),
  CONSTRAINT devolucoes_valor_total_ck
    CHECK (valor_total >= 0)
);

CREATE INDEX IF NOT EXISTS devolucoes_venda_idx ON devolucoes (venda_id);
