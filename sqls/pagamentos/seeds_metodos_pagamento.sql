SET search_path TO projeto_sistemas;


INSERT INTO metodos_pagamento (codigo, descricao, ativo)
VALUES
  ('01', 'Dinheiro', TRUE),
  ('02', 'Cheque', TRUE),
  ('03', 'Cartão de Crédito', TRUE),
  ('04', 'Cartão de Débito', TRUE),
  ('05', 'Crédito Loja', TRUE),
  ('15', 'Boleto', TRUE),
  ('16', 'Depósito Bancário', TRUE),
  ('17', 'Pix', TRUE),
  ('90', 'Sem Pagamento', TRUE),
  ('99', 'Outras', TRUE)
ON CONFLICT (codigo) DO NOTHING;
