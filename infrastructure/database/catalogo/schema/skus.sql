SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS skus (
  sku VARCHAR(50) PRIMARY KEY,
  produto_id INTEGER NOT NULL,
  gtin_ean VARCHAR(14),
  preco NUMERIC(14, 4) NOT NULL,
  estoque NUMERIC(14, 4) NOT NULL DEFAULT 0,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  CONSTRAINT skus_produto_fk
    FOREIGN KEY (produto_id)
    REFERENCES produtos (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT skus_preco_ck
    CHECK (preco >= 0)
);
