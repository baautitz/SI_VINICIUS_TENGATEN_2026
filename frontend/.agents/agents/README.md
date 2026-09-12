# Agentes do projeto

Este diretório define o fluxo de revisão e implementação da UI do ERP.

1. `erp-ui-reviewer-editor.md` inspeciona o código e mantém os documentos de
   contrato atualizados. Sua saída obrigatória é um handoff em
   `docs/ui-layout-review.md`.
2. `erp-ui-developer.md` lê o handoff, implementa apenas as instruções
   aprovadas e executa os gates de validação.

O handoff é a fronteira entre os agentes: o revisor/editor decide o que está
inconsistente e o dev decide como aplicar a correção dentro da arquitetura
existente.
