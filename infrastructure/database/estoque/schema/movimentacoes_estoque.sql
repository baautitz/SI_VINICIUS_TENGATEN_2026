SET search_path TO projeto_sistemas;

\ir estoque_tipos.sql

CREATE TABLE IF NOT EXISTS movimentacoes_estoque (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  data_movimentacao TIMESTAMP NOT NULL DEFAULT NOW(),
  tipo_movimentacao tipo_movimentacao_estoque_enum NOT NULL,
  usuario_id INTEGER,
  nfe_id INTEGER,
  observacao TEXT,
  CONSTRAINT movimentacoes_estoque_usuario_fk
    FOREIGN KEY (usuario_id)
    REFERENCES usuarios (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT movimentacoes_estoque_nfe_fk
    FOREIGN KEY (nfe_id)
    REFERENCES nfes (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT movimentacoes_estoque_venda_nfe_ck
    CHECK (
      (tipo_movimentacao <> 'VENDA') OR
      (tipo_movimentacao = 'VENDA' AND nfe_id IS NOT NULL)
    )
);
