# NF-e de devolução emitida sempre pela loja

Toda Devolução de venda ou de compra cuja Nota Fiscal original está ativa emite uma NF-e de devolução, dentro da mesma transação da Devolução. A loja sempre emite a nota, em emissão própria, mesmo quando a contraparte seria quem emite no mundo real.

- **Devolução de venda:** NF-e de entrada, finalidade devolução (CFOP 1.202), com o cliente como contraparte.
- **Devolução de compra:** NF-e de saída, finalidade devolução (CFOP 5.202), com o fornecedor como contraparte.
- A nota referencia a chave da NF-e original e leva só os Itens devolvidos, com o valor líquido da Devolução. O sistema não tem impostos, então não há tributo a destacar.
- A NF-e original nunca é cancelada pela Devolução, total ou parcial.
- Sem NF-e original ativa, a Devolução segue sem emitir nada.
- A NF-e de devolução não se cancela. O **estorno da Devolução** emite uma NF-e no sentido oposto (venda: saída; compra: entrada), finalidade normal, CFOP 5.949 ou 1.949, referenciando a NF-e de devolução.

## Alternativas descartadas

- **Cancelar a NF-e original na devolução total:** só vale dentro do prazo de cancelamento e se a mercadoria não circulou. Duas regras para o mesmo evento, e a devolução parcial não cancela nunca.
- **Deixar o cliente emitir a nota (PJ contribuinte):** o sistema só enxerga a loja.
- **Cancelar a NF-e de devolução no estorno:** não se cancela nota autorizada fora do prazo.

## Por que agora

A Devolução só avisava que havia NF-e ativa e deixava o cancelamento manual (#78, #85). Sem a nota de devolução, o estoque e o financeiro corrigiam, mas o documento fiscal continuava dizendo que a venda ou a compra estava inteira.
