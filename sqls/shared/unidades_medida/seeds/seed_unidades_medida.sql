SET search_path TO projeto_sistemas;


INSERT INTO unidades_medida (sigla, descricao, categoria, ativo)
VALUES
  ('UN', 'Unidade', 'Quantidade', TRUE),
  ('KG', 'Quilograma', 'Peso', TRUE),
  ('G', 'Grama', 'Peso', TRUE),
  ('L', 'Litro', 'Volume', TRUE),
  ('ML', 'Mililitro', 'Volume', TRUE),
  ('M', 'Metro', 'Comprimento', TRUE),
  ('CM', 'Centimetro', 'Comprimento', TRUE),
  ('M2', 'Metro Quadrado', 'Area', TRUE),
  ('M3', 'Metro Cubico', 'Volume', TRUE),
  ('CX', 'Caixa', 'Quantidade', TRUE),
  ('PT', 'Pote', 'Quantidade', TRUE),
  ('SC', 'Saco', 'Quantidade', TRUE),
  ('PCT', 'Pacote', 'Quantidade', TRUE),
  ('DZ', 'Duzia', 'Quantidade', TRUE),
  ('PC', 'Peca', 'Quantidade', TRUE)
ON CONFLICT (sigla) DO NOTHING;
