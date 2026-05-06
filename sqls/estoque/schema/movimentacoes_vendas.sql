SET search_path TO projeto_sistemas;


CREATE TYPE IF NOT EXISTS projeto_sistemas.tipo_movimentacao_estoque_enum AS ENUM (
  'ENTRADA',
  'SAIDA',
  'AJUSTE',
  'VENDA'
);

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

CREATE TABLE IF NOT EXISTS movimentacoes_estoque_itens (
  id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  movimentacao_estoque_id INTEGER NOT NULL,
  sku_id INTEGER NOT NULL,
  quantidade NUMERIC(14, 4) NOT NULL,
  custo_unitario NUMERIC(14, 4) NOT NULL DEFAULT 0,
  CONSTRAINT movimentacoes_estoque_itens_mov_fk
    FOREIGN KEY (movimentacao_estoque_id)
    REFERENCES movimentacoes_estoque (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT movimentacoes_estoque_itens_sku_fk
    FOREIGN KEY (sku_id)
    REFERENCES skus (id)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT movimentacoes_estoque_itens_quantidade_ck
    CHECK (quantidade > 0),
  CONSTRAINT movimentacoes_estoque_itens_custo_ck
    CHECK (custo_unitario >= 0)
);

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
  CONSTRAINT vendas_conta_receber_fk
    FOREIGN KEY (conta_receber_id)
    REFERENCES contas_receber (id)
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
