# Tamanho único dos botões

Botões com texto usam o tamanho padrão do `Button` (altura `h-8`, fonte `text-sm`). Não se passa `size="sm"` nem `size="xs"` em botão com texto. Os tamanhos `icon`, `icon-sm` e `icon-xs` continuam valendo para botões só de ícone.

## Alternativa descartada

Usar `sm` onde a tela é apertada (barra de parcelas, painéis). Dois botões lado a lado ficavam com altura e fonte diferentes dos botões do resto do sistema, e o usuário notou.

## Por que agora

"+ Parcela" e "Redistribuir" em Compras usavam `sm` (`h-7`, `0.8rem`) e destoavam dos demais botões. O `sm` existia no `Button` mas só 4 botões com texto o usavam.
