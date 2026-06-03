SET search_path TO projeto_sistemas;

INSERT INTO unidades_medida (sigla, descricao, categoria, ativo)
VALUES
  ('UN', 'UNIDADE', 'QUANTIDADE', TRUE),
  ('KG', 'QUILOGRAMA', 'PESO', TRUE),
  ('G', 'GRAMA', 'PESO', TRUE),
  ('L', 'LITRO', 'VOLUME', TRUE),
  ('ML', 'MILILITRO', 'VOLUME', TRUE),
  ('M', 'METRO', 'COMPRIMENTO', TRUE),
  ('CM', 'CENTIMETRO', 'COMPRIMENTO', TRUE),
  ('M2', 'METRO QUADRADO', 'AREA', TRUE),
  ('M3', 'METRO CUBICO', 'VOLUME', TRUE),
  ('CX', 'CAIXA', 'QUANTIDADE', TRUE),
  ('PT', 'POTE', 'QUANTIDADE', TRUE),
  ('SC', 'SACO', 'QUANTIDADE', TRUE),
  ('PCT', 'PACOTE', 'QUANTIDADE', TRUE),
  ('DZ', 'DUZIA', 'QUANTIDADE', TRUE),
  ('PC', 'PECA', 'QUANTIDADE', TRUE)
ON CONFLICT (sigla) DO NOTHING;
