SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS vendas (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  emitente_id INTEGER NOT NULL,
  cliente_id INTEGER NOT NULL,
  data_venda TIMESTAMP NOT NULL DEFAULT NOW(),
  movimentacao_estoque_id INTEGER NOT NULL UNIQUE,
  conta_receber_id INTEGER NOT NULL UNIQUE,
  nfe_id INTEGER NOT NULL UNIQUE,
  valor_total NUMERIC(14, 2) NOT NULL DEFAULT 0,
  observacao TEXT,
  CONSTRAINT vendas_emitente_fk
    FOREIGN KEY (emitente_id)
    REFERENCES emitentes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT vendas_cliente_fk
    FOREIGN KEY (cliente_id)
    REFERENCES clientes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT vendas_movimentacao_fk
    FOREIGN KEY (movimentacao_estoque_id)
    REFERENCES movimentacoes_estoque (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT vendas_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT vendas_valor_total_ck
    CHECK (valor_total >= 0)
);
