# Adaptador shadcn

As implementações visuais Radix/shadcn ficam em `components/ui` durante a
migração. `ui/primitives` e `ui/composites` são a única superfície pública;
este diretório é o ponto reservado para substituir a implementação por Base
UI sem alterar as features.

## Limites do adaptador

`src/imperative-ui` contém a implementação React do ciclo de vida de janelas.
`src/ui/imperative` é o contrato público e reexporta essa implementação. As
features não devem importar `components/ui`, Radix, Sonner ou este adaptador
diretamente; devem consumir `@/ui/primitives`, `@/ui/composites` e
`@/ui/imperative`.

Sonner é uma dependência interna do adaptador de feedback. O único ponto de
uso por uma feature é `ui.feedback.notify`/`ui.feedback.notifyError`. Falhas de
API, domínio, rede e
mutação produzem um único toast; não são exibidas como `Alert` em dialog e não
geram logs de console em fluxos de usuário. Validação de campo permanece inline
e acessível com `aria-invalid`.

Este adaptador deve preservar os contratos visuais: controles compactos em
`h-8`, números de tabelas alinhados à direita e sombras apenas para superfícies
temporárias sobrepostas. A semântica detalhada de feedback está em
[../../../../docs/ui-feedback.md](../../../../docs/ui-feedback.md).
