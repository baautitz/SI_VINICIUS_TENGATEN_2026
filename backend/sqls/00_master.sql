\set ON_ERROR_STOP on

BEGIN;

CREATE SCHEMA IF NOT EXISTS projeto_sistemas;
SET search_path TO projeto_sistemas;

\ir shared/unidades_medida/schema/unidades_medida.sql
\ir localizacao/schema/paises_estados_cidades_bairros.sql

\ir acesso/schema/usuarios.sql
\ir acesso/schema/sessoes.sql
\ir parceiros/schema/clientes_emitentes.sql

\ir logistica/schema/fornecedores_transportadoras_veiculos.sql
\ir catalogo/schema/produtos.sql
\ir pagamentos/schema/metodos_condicoes_parcelas.sql
\ir nfe/schema/nfes.sql
\ir estoque/schema/movimentacoes_vendas.sql
\ir financeiro/schema/contas_pagar_receber.sql
\ir auditoria/schema/auditorias.sql

\ir shared/unidades_medida/seeds/seed_unidades_medida.sql
\ir localizacao/seeds/seed_brasil.sql
\ir pagamentos/seeds/seed_metodos_pagamento.sql

COMMIT;
