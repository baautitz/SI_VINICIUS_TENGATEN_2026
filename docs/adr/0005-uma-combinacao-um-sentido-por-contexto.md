# Uma combinação, um sentido por contexto

Complementa o [ADR 0001](0001-uma-letra-um-sentido-nos-atalhos.md). A regra deixa de ser "uma `Alt+letra` em todo o sistema" e passa a ser "uma combinação, um sentido por **contexto de atalho**". Contexto é a tela ou janela em que o atalho vale (por exemplo `venda`, `nfe`, `produto`), ou `global` quando vale em qualquer tela (sidebar, busca, filtros, novo, ordenação e ações de tabela).

- Cada atalho do registro declara seu contexto (um nome, ou vários quando a mesma ação existe em telas parecidas).
- Há duplicado, e a compilação falha, quando duas entradas têm a mesma combinação e o mesmo contexto, ou quando uma delas é `global`.
- Só a janela do topo fica com os atalhos habilitados, então duas janelas distintas nunca disputam a mesma combinação, mesmo uma aberta sobre a outra.
- `Alt+Shift+letra` fica para quem realmente coexiste com um `Alt+letra` no mesmo contexto ou no global (hoje, Reembolsos contra o `Alt+B` da sidebar).

## Alternativa descartada

Manter `Alt+Shift+letra` para toda ação cuja letra já existe em outra tela. Ele só evitava uma colisão que nunca acontece e deixava o atalho mais difícil de acionar sem ganho.

## Por que agora

Vários `Alt+Shift+letra` (Ver XML, Baixar XML, Salvar rascunho, Movimentação original, Ver estorno, Origem, Replicar preço, Ver produtos) existiam só por causa de um `Alt+letra` de outra tela. Nenhum usuário usa os atalhos ainda.
