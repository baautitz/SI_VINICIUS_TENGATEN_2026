# Agente dev da UI do ERP

## Missão

Receber `docs/ui-layout-review.md` do agente revisor/editor, implementar as
instruções aprovadas no frontend e devolver o projeto validado. O handoff é a
fonte de escopo: não criar refatorações paralelas nem alterar regras de
negócio sem uma instrução explícita.

## Fluxo

1. Ler o handoff, `PRODUCT.md`, `DESIGN.md` e
   `docs/imperative-ui.md` antes de editar.
2. Preservar a fronteira: features importam `@/ui/primitives`,
   `@/ui/composites` e `@/ui/imperative`; Radix/shadcn fica nos adaptadores.
3. Aplicar primeiro a correção nos primitives compartilhados e no host.
4. Normalizar cada exceção listada pelo revisor com
   `data-window-actions`; remover títulos duplicados e mover descrições para
   as opções da janela quando o header for o lugar correto.
5. Usar `apply_patch` para alterações manuais, manter mudanças locais
   existentes e não sobrescrever componentes shadcn sem autorização.
6. Executar os gates definidos no handoff. Se um gate falhar por causa de
   outra alteração, registrar a causa e não mascarar o erro.

O gate específico do padrão de janelas é `npm run check:dialog-layout`.

## Critério de aceite

Para toda janela de produto:

- `DialogContent`, `SheetContent` e `AlertDialogContent` não são a área de
  rolagem;
- header/titlebar permanece visível durante a rolagem;
- somente o body rola verticalmente;
- footer/ações permanece visível no limite inferior;
- título é renderizado uma vez, no header;
- campos continuam navegáveis por teclado e com feedback inline.

## Entrega

Retornar um resumo dos arquivos alterados, os gates executados e qualquer
pendência que precise voltar ao agente revisor/editor. Não considerar a tarefa
concluída se uma exceção de janela conhecida continuar sem instrução ou teste.
