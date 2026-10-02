SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS skus (
  sku VARCHAR(50) PRIMARY KEY,
  produto_id INTEGER NOT NULL,
  gtin_ean VARCHAR(14),
  preco NUMERIC(14, 4) NOT NULL,
  estoque NUMERIC(14, 4) NOT NULL DEFAULT 0,
  custo_medio NUMERIC(18, 10) NOT NULL DEFAULT 0,
  custo_ultima_compra NUMERIC(18, 10) NOT NULL DEFAULT 0,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  CONSTRAINT skus_produto_fk
    FOREIGN KEY (produto_id)
    REFERENCES produtos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT skus_preco_ck
    CHECK (preco >= 0),
  CONSTRAINT skus_custo_medio_ck
    CHECK (custo_medio >= 0),
  CONSTRAINT skus_custo_ultima_compra_ck
    CHECK (custo_ultima_compra >= 0)
);

-- Migração: custo com 10 casas (C1).
ALTER TABLE skus ALTER COLUMN custo_medio TYPE NUMERIC(18, 10);
ALTER TABLE skus ALTER COLUMN custo_ultima_compra TYPE NUMERIC(18, 10);
