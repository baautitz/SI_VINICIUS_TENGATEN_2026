SET search_path TO projeto_sistemas;


INSERT INTO paises (ddi, sigla_iso, moeda, simbolo_moeda, pais)
VALUES
  ('55', 'BRA', 'BRL', 'R$', 'Brasil')
ON CONFLICT (sigla_iso) DO NOTHING;

INSERT INTO estados (pais_id, estado, uf)
SELECT id, 'Acre', 'AC' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Alagoas', 'AL' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Amapá', 'AP' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Amazonas', 'AM' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Bahia', 'BA' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Ceará', 'CE' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Distrito Federal', 'DF' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Espírito Santo', 'ES' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Goiás', 'GO' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Maranhão', 'MA' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Mato Grosso', 'MT' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Mato Grosso do Sul', 'MS' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Minas Gerais', 'MG' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Pará', 'PA' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Paraíba', 'PB' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Paraná', 'PR' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Pernambuco', 'PE' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Piauí', 'PI' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Rio de Janeiro', 'RJ' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Rio Grande do Norte', 'RN' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Rio Grande do Sul', 'RS' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Rondônia', 'RO' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Roraima', 'RR' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Santa Catarina', 'SC' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'São Paulo', 'SP' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Sergipe', 'SE' FROM paises WHERE pais = 'Brasil' UNION ALL
SELECT id, 'Tocantins', 'TO' FROM paises WHERE pais = 'Brasil'
ON CONFLICT (pais_id, uf) DO NOTHING;
