# Atalhos

A fonte da verdade e `frontend/src/lib/atalhos.ts` (`ATALHOS`: id, combinacao, rotulo, contexto). Regra: [ADR 0001](adr/0001-uma-letra-um-sentido-nos-atalhos.md), por contexto conforme o [ADR 0005](adr/0005-uma-combinacao-um-sentido-por-contexto.md).

- Novo atalho: adicionar no registro com `contexto` (tela, lista de telas ou `global`); `npm run test:atalhos` e a compilacao falham se a combinacao ja existir no mesmo contexto ou em `global`.
- Telas e `HotkeyKbd` devem ler o registro, nao usar strings livres.
- Vocabulario (atalho, dica de atalho): `GLOSSARY.md`.

## Navegacao e confirmacao

- `confirmar` Alt+Enter, `anterior` Alt+ArrowLeft, `seguinte` Alt+ArrowRight (etapa do wizard ou pagina da tabela; telas distintas, mesmo sentido).
- Tabelas: `ordenar` Alt+J abre o menu de ordenacao pelo teclado (↑ ↓ coluna, ← → modo, Enter fecha). `abrirLinha` Enter, `editarLinha` Alt+E, `excluirLinha` Delete. `anterior`/`seguinte` (Alt+ArrowLeft/Right) paginam a tabela. `excluirLinhaBackspace` (Backspace) e a segunda combinacao da mesma acao, com id proprio para manter a unicidade.

## Ver registros relacionados

- `verProdutos` Alt+P (marcas, categorias, unidades de medida). Alt+P tambem e `adicionarParcela`, mas em outras telas.
- `reembolsos` Alt+Shift+B (cliente): unico que mantem Shift, pois Alt+B e `sidebarRecolher` (global).

## Devolucao de venda

- `devolucaoParcial` Alt+1 e `devolucaoTotal` Alt+2, na ordem dos botoes. Digitos evitam disputar letras ja usadas (Alt+P parcela, Alt+T titulos).

## Venda

- `adicionarPagamento` Alt+Y adiciona uma linha de forma de pagamento no checkout da venda (Alt+P ja e parcela).
