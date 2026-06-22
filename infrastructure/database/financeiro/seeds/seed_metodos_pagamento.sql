SET search_path TO projeto_sistemas;


INSERT INTO metodos_pagamento (codigo, descricao, ativo)
VALUES
  ('01', 'DINHEIRO', TRUE),
  ('02', 'CHEQUE', TRUE),
  ('03', 'CARTÃO DE CRÉDITO', TRUE),
  ('04', 'CARTÃO DE DÉBITO', TRUE),
  ('05', 'CRÉDITO LOJA', TRUE),
  ('15', 'BOLETO', TRUE),
  ('16', 'DEPÓSITO BANCÁRIO', TRUE),
  ('17', 'PIX', TRUE),
  ('90', 'SEM PAGAMENTO', TRUE),
  ('99', 'OUTRAS', TRUE)
ON CONFLICT (codigo) DO UPDATE SET
  descricao = EXCLUDED.descricao,
  ativo = EXCLUDED.ativo;
