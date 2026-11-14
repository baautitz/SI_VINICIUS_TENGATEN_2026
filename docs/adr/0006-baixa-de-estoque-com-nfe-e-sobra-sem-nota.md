# Baixa de estoque e ajuste de inventário com NF-e (Paraná)

Todo ajuste de estoque deixa rastro: tem Motivo da movimentação, e toda baixa ou ajuste tem NF-e. O estoque não muda "por fora".

- **Entrada de estoque** só nasce da Compra (NF-e do fornecedor), dos retornos que já existem (devolução de venda, estorno) e do ajuste de inventário positivo do Balanço. Não existe Movimentação manual de entrada.
- **Saída manual** exige Motivo da movimentação. Perda/avaria, furto/roubo e vencimento emitem **NF-e de baixa**: saída própria, finalidade baixa de estoque, CFOP 5.927, com a loja como destinatária e vinculada à Movimentação, sem referenciar outra nota. Não existe saída manual sem nota: Outros e erro de contagem só existem no Balanço.
- **Estorno da Movimentação** com NF-e de baixa emite a **NF-e de estorno da baixa**: entrada própria, CFOP 1.949, referenciando a NF-e de baixa. A NF-e de baixa não se cancela, como a de devolução (ADR 0004).
- **Falta no Balanço** exige Motivo da movimentação (perda/avaria, furto/roubo, vencimento ou erro de contagem) e emite **NF-e de baixa** (CFOP 5.927) em todos os casos: a SEFA/PR entende que a regra do art. 237 do RICMS/PR vale "para todas as situações em que verificadas perdas de mercadorias".
- **Sobra no Balanço** exige Recontagem e motivo (erro de contagem ou outros) e emite **NF-e de entrada de ajuste de inventário**: entrada própria, CFOP 1.949, sem destaque de ICMS, com a loja como destinatária e justificativa. O cancelamento do Balanço emite a **NF-e de estorno do ajuste** (saída, CFOP 5.949) referenciando a nota de entrada.
- O Balanço só fecha com toda divergência resolvida ou justificada.

O sistema não tem impostos, então a NF-e de baixa não destaca tributo.

## Alternativas descartadas

- **Sobra sem nota (posição da SEFAZ-SP, RC 17665/2018):** a SP entende que não há previsão legal para regularizar sobra com nota. O Paraná pensa diferente: a Consulta SEFA nº 3/2024 reconhece que não há determinação expressa, mas orienta que o ingresso por divergência interna seja feito com nota de entrada, CFOP 1.949, sem destaque de ICMS, com justificativa. O sistema adota a posição do Paraná. Fonte lida numa reprodução (LegisWeb); conferir o PDF oficial da SEFA e confirmar com um contador.
- **Entrada manual livre:** aumentaria o saldo sem Compra, que é mercadoria sem nota.
- **Bloquear o Balanço enquanto houver sobra:** a Recontagem com motivo e a nota de entrada já a documentam.
- **Nota de débito de perda em estoque (Ajuste SINIEF 49/2025):** fora do escopo, o sistema só emite NF-e comum.

## Por que agora

A Movimentação manual e o fechamento do Balanço mudavam o saldo com motivo em texto livre e sem documento (#117), o que na prática deixa o estoque ser alterado sem rastro fiscal.
