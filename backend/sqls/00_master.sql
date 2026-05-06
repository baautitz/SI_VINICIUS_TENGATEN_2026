\set ON_ERROR_STOP on

BEGIN;

\i ./shared/unidades_medida/schema/unidades_medida.sql
\i ./localizacao/schema/paises_estados_cidades_bairros.sql

\i ./acesso/schema/usuarios.sql
\i ./acesso/schema/sessoes.sql
\i ./parceiros/schema/clientes_emitentes.sql

\i ./logistica/schema/fornecedores_transportadoras_veiculos.sql
\i ./catalogo/schema/produtos.sql
\i ./pagamentos/schema/metodos_condicoes_parcelas.sql
\i ./financeiro/schema/contas_pagar_receber.sql
\i ./estoque/schema/movimentacoes_vendas.sql
\i ./nfe/schema/nfes.sql
\i ./auditoria/schema/auditorias.sql

\i ./shared/unidades_medida/seeds/seed_unidades_medida.sql
\i ./localizacao/seeds/seed_brasil.sql
\i ./pagamentos/seeds/seed_metodos_pagamento.sql

COMMIT;
