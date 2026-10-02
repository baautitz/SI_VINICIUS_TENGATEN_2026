SET search_path TO projeto_sistemas;

\ir estoque_tipos.sql

-- Razão imutável: movimentação lançada nunca é alterada; estorno é uma nova
-- movimentação inversa (origem ESTORNO, origem_id = movimentação original).
CREATE TABLE IF NOT EXISTS movimentacoes_estoque (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  data_movimentacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  tipo_movimentacao tipo_movimentacao_estoque_enum NOT NULL,
  origem_tipo origem_movimentacao_estoque_enum NOT NULL,
  origem_id INTEGER,
  usuario_id INTEGER,
  motivo VARCHAR(500),
  observacao TEXT,
  CONSTRAINT movimentacoes_estoque_usuario_fk
    FOREIGN KEY (usuario_id)
    REFERENCES usuarios (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT movimentacoes_estoque_origem_ck
    CHECK (origem_tipo = 'MANUAL' OR origem_id IS NOT NULL),
  CONSTRAINT movimentacoes_estoque_motivo_ck
    CHECK (origem_tipo NOT IN ('MANUAL', 'ESTORNO') OR LENGTH(TRIM(COALESCE(motivo, ''))) >= 5)
);

CREATE INDEX IF NOT EXISTS movimentacoes_estoque_origem_idx
  ON movimentacoes_estoque (origem_tipo, origem_id);

-- Uma movimentação só pode ser estornada uma vez.
CREATE UNIQUE INDEX IF NOT EXISTS movimentacoes_estoque_estorno_uk
  ON movimentacoes_estoque (origem_id)
  WHERE origem_tipo = 'ESTORNO';
