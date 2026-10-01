SET search_path TO projeto_sistemas;


INSERT INTO metodos_pagamento (codigo, descricao, ativo, permite_troco)
VALUES
  ('01', 'DINHEIRO', TRUE, TRUE),
  ('02', 'CHEQUE', TRUE, FALSE),
  ('03', 'CARTÃO DE CRÉDITO', TRUE, FALSE),
  ('04', 'CARTÃO DE DÉBITO', TRUE, FALSE),
  ('05', 'CRÉDITO LOJA', TRUE, FALSE),
  ('15', 'BOLETO', TRUE, FALSE),
  ('16', 'DEPÓSITO BANCÁRIO', TRUE, FALSE),
  ('17', 'PIX', TRUE, FALSE),
  ('90', 'SEM PAGAMENTO', TRUE, FALSE),
  ('99', 'OUTRAS', TRUE, FALSE)
ON CONFLICT (codigo) DO UPDATE SET
  descricao = EXCLUDED.descricao,
  ativo = EXCLUDED.ativo,
  permite_troco = EXCLUDED.permite_troco;
