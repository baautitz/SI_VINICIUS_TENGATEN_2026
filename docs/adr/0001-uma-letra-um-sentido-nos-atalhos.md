# Uma letra, um sentido nos atalhos

> Refinado pelo [ADR 0005](0005-uma-combinacao-um-sentido-por-contexto.md): a unicidade vale por contexto (tela), não no sistema inteiro.

Cada `Alt+letra` tem um único significado em todo o sistema. Ações menos frequentes usam `Alt+Shift+letra`. A fonte da verdade é um registro tipado no código, e uma letra duplicada falha na compilação.

## Alternativa descartada

Manter o escopo por janela, com a mesma letra significando coisas diferentes em telas diferentes. Não gera colisão técnica, mas obriga o usuário a reaprender a letra a cada tela e deixa o sistema sem tabela única.

## Por que agora

Nenhum usuário usa os atalhos ainda. Padronizar depois do release custaria a memória muscular de quem já aprendeu.
