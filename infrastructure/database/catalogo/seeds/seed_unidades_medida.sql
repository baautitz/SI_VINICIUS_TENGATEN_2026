SET search_path TO projeto_sistemas;

INSERT INTO unidades_medida (
    sigla,
    descricao,
    categoria,
    permite_decimais,
    ativo
)
VALUES
    ('UN',  'UNIDADE',          'QUANTIDADE', FALSE, TRUE),
    ('KG',  'QUILOGRAMA',       'PESO',       TRUE,  TRUE),
    ('G',   'GRAMA',            'PESO',       TRUE,  TRUE),
    ('L',   'LITRO',            'VOLUME',     TRUE,  TRUE),
    ('ML',  'MILILITRO',        'VOLUME',     TRUE,  TRUE),
    ('M',   'METRO',            'COMPRIMENTO',TRUE,  TRUE),
    ('CM',  'CENTIMETRO',       'COMPRIMENTO',TRUE,  TRUE),
    ('M2',  'METRO QUADRADO',   'AREA',       TRUE,  TRUE),
    ('M3',  'METRO CUBICO',     'VOLUME',     TRUE,  TRUE),
    ('CX',  'CAIXA',            'QUANTIDADE', FALSE, TRUE),
    ('PT',  'POTE',             'QUANTIDADE', FALSE, TRUE),
    ('SC',  'SACO',             'QUANTIDADE', FALSE, TRUE),
    ('PCT', 'PACOTE',           'QUANTIDADE', FALSE, TRUE),
    ('DZ',  'DUZIA',            'QUANTIDADE', FALSE, TRUE),
    ('PC',  'PECA',             'QUANTIDADE', FALSE, TRUE)
ON CONFLICT (sigla) DO NOTHING;