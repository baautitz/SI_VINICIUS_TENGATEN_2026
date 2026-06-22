SET search_path TO projeto_sistemas;

CREATE TABLE IF NOT EXISTS metodos_pagamento (
  codigo VARCHAR(10) NOT NULL PRIMARY KEY,
  descricao VARCHAR(100) NOT NULL,
  ativo BOOLEAN NOT NULL DEFAULT TRUE
);
