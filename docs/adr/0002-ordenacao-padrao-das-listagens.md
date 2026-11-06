# Ordenação padrão das listagens

Toda listagem abre com um critério explícito e um desempate estável (o `id`), em vez da ordem herdada do acaso.

- **Operações** (Compras, Vendas, Movimentações, Balanços, NF-es): mais recente primeiro.
- **Contas a pagar e a receber**: pendentes e vencidas primeiro, depois as pagas; vencimento crescente dentro de cada grupo. Com termo de pesquisa, a relevância da pesquisa manda.
- **Parceiros, Catálogo e Referência**: alfabética por nome; SKUs pelo código e Veículos pela placa.

## Alternativa descartada

Deixar cada tela com a ordem que o banco devolve (vencimento crescente nas contas, `id` decrescente no catálogo). O critério não foi decidido por ninguém e a lista não mostra o que exige ação nem permite achar um cadastro sem pesquisar.

## Por que agora

O usuário já pediu abrir as contas pelo que exige ação e ordenar pelo cabeçalho da coluna; ambos dependem de uma ordem padrão definida.
