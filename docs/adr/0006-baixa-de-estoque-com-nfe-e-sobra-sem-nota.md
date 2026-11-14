# Baixa de estoque com NF-e e sobra de inventário sem nota

Todo ajuste de estoque deixa rastro: tem Motivo da movimentação, e a saída por perda, furto ou vencimento tem NF-e. O estoque não muda "por fora".

- **Entrada de estoque** só nasce da Compra (NF-e do fornecedor), dos retornos que já existem (devolução de venda, estorno) e do ajuste de inventário positivo do Balanço. Não existe Movimentação manual de entrada.
- **Saída manual** exige Motivo da movimentação. Perda/avaria, furto/roubo e vencimento emitem **NF-e de baixa**: saída própria, finalidade baixa de estoque, CFOP 5.927, com a loja como destinatária e vinculada à Movimentação, sem referenciar outra nota. Outros não emite nota.
- **Estorno da Movimentação** com NF-e de baixa emite a **NF-e de estorno da baixa**: entrada própria, CFOP 1.949, referenciando a NF-e de baixa. A NF-e de baixa não se cancela, como a de devolução (ADR 0004).
- **Falta no Balanço** exige Motivo da movimentação (perda/avaria, furto/roubo, vencimento ou erro de contagem); os três primeiros emitem NF-e de baixa, e erro de contagem é ajuste interno.
- **Sobra no Balanço** exige Recontagem e motivo (erro de contagem ou outros) e vira entrada de ajuste de inventário **sem NF-e**, documentada pelo próprio Balanço.
- O Balanço só fecha com toda divergência resolvida ou justificada.

O sistema não tem impostos, então a NF-e de baixa não destaca tributo.

## Alternativas descartadas

- **NF-e de entrada de regularização (CFOP 1.949) para a sobra:** circula em fóruns de contabilidade, mas a SEFAZ-SP entende que não há previsão legal para regularizar sobra com nota (RICMS/SP art. 204 veda documento sem operação real). A regra varia por estado; o sistema adota a posição de São Paulo.
- **Entrada manual livre:** aumentaria o saldo sem Compra, que é mercadoria sem nota.
- **Bloquear o Balanço enquanto houver sobra:** não há nota que a regularize; a Recontagem com motivo já a documenta.
- **Nota de débito de perda em estoque (Ajuste SINIEF 49/2025):** fora do escopo, o sistema só emite NF-e comum.

## Por que agora

A Movimentação manual e o fechamento do Balanço mudavam o saldo com motivo em texto livre e sem documento (#117), o que na prática deixa o estoque ser alterado sem rastro fiscal.
