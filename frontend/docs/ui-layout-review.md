# Revisão de layout de janelas

Status: implementado e validado
Data: 2026-09-11

## Escopo

Auditoria do frontend em `src/components/ui`, `src/imperative-ui`,
`src/features` e `src/components/entity-inputs`, com foco nas superfícies
abertas pelo Window Manager.

## Achados

### P1 — área de rolagem englobava header e ações

`DialogContent`, `AlertDialogContent` e `SheetContent` usavam `overflow-y-auto`
no container externo. Isso permitia que título e ações saíssem da viewport em
formulários longos.

### P1 — contrato estrutural não era explícito

O host inseria o conteúdo diretamente no `Content`, sem uma primitive de body
rolável compartilhada. A posição das ações dependia de seletores espalhados no
container externo.

### P2 — janelas especiais fora do marcador de footer

Quantidade de SKU, cancelamento de venda, checkout, baixa financeira e a
confirmação de efetivação tinham ações sem `data-window-actions`. Isso tornava
impossível aplicar o mesmo comportamento de footer a todas as janelas.

### P2 — título duplicado na baixa financeira

`BaixaParcelaWindow` repetia no conteúdo o mesmo título já fornecido pelo host.

## Instruções para o agente dev

- Criar `DialogBody`, `AlertDialogBody` e `SheetBody` como primitives públicas.
- Fazer os três `*Content` usarem `overflow-hidden` e manter header/footer fora
  da rolagem principal.
- Fazer o `WindowManagerHost` montar `Header → Body → conteúdo`.
- Manter `data-window-actions` como marcador canônico e aplicar sticky no body.
- Normalizar as cinco janelas especiais e mover a descrição da baixa financeira
  para a opção `description` da janela.
- Atualizar `docs/imperative-ui.md`, `DESIGN.md` e os contratos estáticos para
  documentar a regra.

## Resultado da implementação

- `DialogContent`, `AlertDialogContent` e `SheetContent` agora mantêm header e
  ações fora da rolagem principal com `overflow-hidden`.
- `DialogBody`, `AlertDialogBody` e `SheetBody` concentram a rolagem em
  `overflow-y-auto`, usam `min-h-0 flex-1` e têm `p-1` para preservar bordas,
  anéis de foco, listas e botões nas bordas.
- O host agora monta sempre `Header → Body → conteúdo`, e o marcador
  `data-window-actions` mantém o footer visível.
- As cinco janelas especiais foram normalizadas; o título duplicado da baixa
  financeira foi removido.

## Validação executada

```bash
npm run lint
npm run build
npm run test:imperative-ui
npm run imperative-ui:check-boundaries
npm run check:dialog-layout
git diff --check
```

Todos os gates passaram em 2026-09-11.

## Conformes que não devem ser alterados

- A pilha e o ciclo de vida do `WindowController`.
- Overlays independentes e regras de foco do host.
- A fronteira pública `@/ui/primitives`.
- O contrato de feedback por toast e validação inline.

## Gates

```bash
npm run lint
npm run build
npm run test:imperative-ui
npm run imperative-ui:check-boundaries
npm run check:dialog-layout
```
