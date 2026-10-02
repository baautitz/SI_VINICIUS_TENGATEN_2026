SET search_path TO projeto_sistemas;

\ir estoque_tipos.sql

CREATE TABLE IF NOT EXISTS balancos (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  data_abertura TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
  data_fechamento TIMESTAMP WITH TIME ZONE,
  status status_balanco_enum NOT NULL DEFAULT 'ABERTO',
  usuario_id INTEGER,
  observacao TEXT,
  CONSTRAINT balancos_usuario_fk
    FOREIGN KEY (usuario_id)
    REFERENCES usuarios (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION
);

CREATE TABLE IF NOT EXISTS balancos_itens (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  balanco_id INTEGER NOT NULL,
  sku VARCHAR(50) NOT NULL,
  quantidade_sistema NUMERIC(14, 4) NOT NULL,
  quantidade_contada NUMERIC(14, 4),
  CONSTRAINT balancos_itens_balanco_fk
    FOREIGN KEY (balanco_id)
    REFERENCES balancos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT balancos_itens_sku_fk
    FOREIGN KEY (sku)
    REFERENCES skus (sku)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT balancos_itens_unico_uk UNIQUE (balanco_id, sku),
  CONSTRAINT balancos_itens_contada_ck CHECK (quantidade_contada IS NULL OR quantidade_contada >= 0)
);
