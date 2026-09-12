# UI imperativa

## Objetivo

A UI imperativa coordena fluxos que atravessam superfícies (abrir janela,
confirmar, resolver, descartar, notificar e navegar). Estado local de Select,
Popover, filtros, menus e campos continua declarativo.

O produto usa uma única fachada:

```tsx
const ui = useUi()

const result = await ui.windows.open(ClienteForm, { editingItem: cliente }, {
  title: "Editar cliente",
  icon: <Users />,
  surface: "dialog",
  size: "large",
})

if (result.status === "confirmed") {
  await invalidate()
  ui.feedback.notify({ type: "success", title: "Cliente atualizado" })
}

const confirmed = await ui.windows.confirm({
  title: "Excluir cliente?",
  description: "Esta ação não poderá ser desfeita.",
  confirmVariant: "destructive",
})
if (confirmed) await invalidate()
```

Dentro da superfície, `useWindow()` expõe somente o ciclo de vida daquela
janela:

```tsx
const window = useWindow<Cliente>()
window.resolve(cliente)
window.dismiss()
window.setDirty(isDirty)
```

`useWindowManager` e `useActiveWindow` continuam apenas como aliases de
compatibilidade durante a migração. O código novo deve usar `useUi` e
`useWindow`.

## Arquitetura

`src/ui/imperative/controller.ts` é uma máquina de estados TypeScript pura.
Ela não importa React, DOM, Radix, shadcn, Next ou Sonner. As portas são:

```ts
interface WindowPorts {
  confirmDiscard(request: DiscardRequest): Promise<boolean>
  restoreFocus(target: FocusTarget | null): void
}
```

O Provider adapta essas portas para React, foco, confirmação, feedback e
navegação. O Host apenas projeta o snapshot.

### Titlebar e confirmação

Janelas de produto exibem uma titlebar única, com ícone e título. `icon` aceita
um elemento (`<Users />`) ou um componente de ícone; a mesma propriedade pode
ser passada pela feature de listagem para que página e seleção modal mantenham
a identidade visual.

`ui.windows.confirm` é uma superfície de confirmação independente da janela
de formulário. Ela usa `AlertDialog` compacto, com título, descrição e ações
no footer; não herda largura, chrome ou layout do formulário que a abriu.

### Invariantes

- IDs são únicos e cada Promise é resolvida uma única vez.
- Somente o topo pode resolver ou fechar; IDs inferiores são ignorados.
- `dismiss` e `requestDismiss` passam pela política de dirty.
- Uma segunda solicitação enquanto a confirmação de descarte está aberta é
  ignorada.
- `dismissAll()` resolve todas como `reason: "unmounted"`.
- `dispose()` encerra a pilha sem deixar Promises pendentes.

## Janelas e backdrop

Cada janela é independente e modal:

- cada `Dialog`/`Sheet` mantém seu próprio Portal e Overlay Radix;
- A → B → C não altera posição, tamanho, escala, opacidade ou estado de A/B;
- não existe backdrop base, profundidade, cálculo de stack ou variação visual;
- o topo é usado somente para regras de foco, teclado e fechamento;
- o conteúdo inferior fica inerte, sem CSS de “janela atrás”.

O Host não usa `modal={isTop}`, `showOverlay`, `hasBaseOverlay`, `opacity`,
`transform` ou largura estrutural forçada. O Radix já cria os portais no
`document.body`; outro portal esconderia o problema em vez de corrigi-lo.

## Fronteira de UI

```text
src/
  imperative-ui/    implementação React: provider, host, context e commands
  ui/
    imperative/     API pública e reexports para a implementação e contratos
    primitives/      contrato visual público (adaptador shadcn/Radix)
    composites/      DataTable, EntityInput, FeatureHeader etc.
    adapters/        integrações de feedback, navegação e futuras bases visuais
```

As opções de janela são semânticas (`surface`, `size`, `chrome`, `side` e
`closeOnOutside`); classes estruturais não atravessam a API. As features
importam `@/ui/primitives`, `@/ui/composites` e `@/ui/imperative`.
`src/imperative-ui` contém a implementação; `src/ui/imperative` é o contrato
público e a camada de reexportação. `components/ui` é a implementação visual
shadcn/Radix legada, reexportada pelas superfícies públicas, e não deve ser
importada diretamente por novas features.

`scripts/check-imperative-ui-boundaries.mjs` verifica imports de Router,
Sonner, hotkeys, Radix/Base UI e superfícies de janela fora dos adaptadores.

## Feedback, falhas e navegação

`ui.feedback.notify` e `ui.feedback.notifyError` são a única fachada para
feedback operacional. Um erro de API, domínio, rede ou mutação gera exatamente
um toast; não deve ser apresentado novamente como `Alert` dentro da janela que o originou. A janela permanece
aberta após a falha e sua Promise/handler precisa consumir a rejeição, sem
`unhandledRejection`, `console.error` ou `console.warn` no fluxo do usuário.

Erros de validação de campo permanecem declarativos e inline, com
`aria-invalid`; eles não substituem nem duplicam o feedback operacional. A
matriz completa de sucesso, informação, warning, loading, confirmação e
navegação está em [ui-feedback.md](ui-feedback.md).

Para navegação que participa do ciclo de vida de janelas, usar
`ui.navigation`. Um `next/link` é adequado apenas para navegação declarativa
sem necessidade de coordenar descarte, foco ou limpeza de superfícies.

## Foco e comandos

O controller/provider são os únicos donos da restauração de foco. `DataTable`
e `EntityInput` não observam o DOM, não contam dialogs e não usam timers para
disputar o foco. Comandos permanecem em `useWindowCommands`, com escopo da
janela e registro de conflitos.

## Radix e Base UI

A correção funcional usa o adaptador Radix existente. A troca para Base UI não
é misturada ao Window Controller: o projeto tem muitos consumidores e as APIs
de Dialog, Drawer, `asChild`/`render`, foco, portais e Select diferem.

Próximo passo isolado: um adaptador experimental para Dialog, Sheet e Popover,
validando Dialog/Dialog, Sheet/Sheet, Dialog/Sheet, dirty, Escape, clique
externo, foco, acessibilidade, Select e build. Só migrar outras primitivas
depois de paridade real.

## Gates

```bash
npm run lint
npm run build
npm run test:imperative-ui
npm run imperative-ui:check-boundaries
```

O teste de contrato cobre invariantes do controller, overlays independentes,
ausência de alterações visuais de stack e remoção dos observadores globais de
foco. Testes manuais/automatizados devem cobrir Dialog/Dialog, Sheet/Sheet,
Dialog/Sheet e as sequências Venda → Checkout → Condição de pagamento e
formulário sujo → confirmação de descarte.
