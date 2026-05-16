\set ON_ERROR_STOP on

BEGIN;

CREATE SCHEMA IF NOT EXISTS projeto_sistemas;
SET search_path TO projeto_sistemas;

\ir unidades_medida/schema/unidades_medida.sql

\ir localizacao/schema/paises.sql
\ir localizacao/schema/estados.sql
\ir localizacao/schema/cidades.sql
\ir localizacao/schema/bairros.sql

\ir acesso/schema/usuarios.sql
\ir acesso/schema/sessoes.sql

\ir parceiros/schema/clientes.sql
\ir parceiros/schema/emitentes.sql

\ir logistica/schema/fornecedores.sql
\ir logistica/schema/transportadoras.sql
\ir logistica/schema/veiculos.sql

\ir catalogo/schema/categorias.sql
\ir catalogo/schema/marcas.sql
\ir catalogo/schema/produtos.sql
\ir catalogo/schema/skus.sql
\ir catalogo/schema/sku_atributos_chaves.sql
\ir catalogo/schema/skus_atributos_valores.sql

\ir pagamentos/schema/metodos_pagamento.sql
\ir pagamentos/schema/condicoes_pagamentos.sql
\ir pagamentos/schema/condicoes_pagamentos_parcelas.sql

\ir vendas/schema/vendas.sql

\ir nfe/schema/nfe_tipos.sql
\ir nfe/schema/nfes_table.sql
\ir nfe/schema/nfes_informacoes_adicionais.sql
\ir nfe/schema/nfes_transportes.sql
\ir nfe/schema/nfes_itens.sql
\ir nfe/schema/nfes_pagamentos.sql

\ir estoque/schema/movimentacoes_estoque.sql
\ir estoque/schema/movimentacoes_estoque_itens.sql

\ir financeiro/schema/financeiro_tipos.sql
\ir financeiro/schema/contas_pagar.sql
\ir financeiro/schema/contas_pagar_parcelas.sql
\ir financeiro/schema/contas_receber.sql
\ir financeiro/schema/contas_receber_parcelas.sql

\ir auditoria/schema/auditorias.sql

\ir unidades_medida/seeds/seed_unidades_medida.sql
\ir localizacao/seeds/seed_brasil.sql
\ir pagamentos/seeds/seed_metodos_pagamento.sql

COMMIT;
