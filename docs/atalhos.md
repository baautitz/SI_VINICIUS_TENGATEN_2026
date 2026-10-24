# Atalhos

A fonte da verdade e `frontend/src/lib/atalhos.ts` (`ATALHOS`: id, combinacao, rotulo). Regra: [ADR 0001](adr/0001-uma-letra-um-sentido-nos-atalhos.md).

- Novo atalho: adicionar no registro; `npm run test:atalhos` falha se a combinacao ja existir.
- Telas e `HotkeyKbd` devem ler o registro, nao usar strings livres.
- Vocabulario (atalho, dica de atalho): `GLOSSARY.md`.

## Navegacao e confirmacao

- `confirmar` Alt+Enter, `etapaAnterior` Alt+ArrowLeft, `etapaSeguinte` Alt+ArrowRight.
- Tabelas: `abrirLinha` Enter, `editarLinha` Alt+E, `excluirLinha` Delete. `excluirLinhaBackspace` (Backspace) e a segunda combinacao da mesma acao, com id proprio para manter a unicidade.

## Ver registros relacionados

- `verProdutos` Alt+Shift+P (marcas, categorias, unidades de medida). Alt+R ficou com `verContasReceber`, entao Produtos usa Shift.

## Devolucao de venda

- `devolucaoParcial` Alt+1 e `devolucaoTotal` Alt+2, na ordem dos botoes. Digitos evitam disputar letras ja usadas (Alt+P parcela, Alt+T titulos).
