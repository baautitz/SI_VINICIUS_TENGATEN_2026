# Matriz canônica de feedback da UI

## Objetivo

Esta matriz é a fonte de verdade para como a interface comunica resultado,
estado e risco. Ela se aplica a todos os módulos. `DESIGN.md` define a forma
visual; `PRODUCT.md` define a intenção do produto; esta página define a decisão
de interação.

## Contrato de resposta

O backend retorna resultados de negócio e erro no mesmo envelope:

```json
{
  "success": false,
  "data": null,
  "errors": [
    {
      "code": "CODIGO_ESTAVEL",
      "message": "Mensagem adequada para a pessoa usuária.",
      "field": null
    }
  ]
}
```

O frontend interpreta esse envelope tanto em respostas HTTP de erro quanto em
respostas HTTP bem-sucedidas com `success: false`. Cada item de `errors` possui
`code`, `message` e `field` opcional. Erros de rede, corpo inválido ou vazio e
falhas inesperadas são normalizados para o mesmo contrato antes de chegar à UI.
O transporte nunca lança objetos literais, nem converte um erro com
`String(error)` para exibição.

`404` de lookup opcional pode ser tratado silenciosamente quando a chamada
declara essa semântica; todo outro erro operacional segue a matriz abaixo.

## Matriz de estados

| Situação | Apresentação | Superfície atual | Regras |
| --- | --- | --- | --- |
| Validação de campo | `FieldError` inline e `aria-invalid` | Permanece aberta | Mensagem ao lado/abaixo do campo; focar o primeiro campo inválido ao submeter. Não criar toast para o mesmo erro. |
| Erro de API, domínio, rede ou mutação | Um toast de erro | Permanece aberta | Preservar valores. Não renderizar o erro como `Alert` no dialog, não duplicar toast e não deixar rejeição sem tratamento. |
| Sucesso de mutação | Um toast de sucesso, quando houver confirmação útil | Fechar/atualizar somente conforme o fluxo concluído | Invalidar/atualizar dados após o resultado. Não anunciar duas vezes a mesma operação. |
| Informação | Um toast informativo | Não interrompe | Usar para aviso transitório, sem ação destrutiva ou erro. |
| Warning | Um toast de warning ou estado persistente | Não interrompe | Toast para aviso transitório; `Alert` somente se a condição permanece relevante na tela. |
| Estado persistente | `Alert` contextual | Continua visível | Ex.: registro cancelado, bloqueio já conhecido ou condição que afeta a tela inteira. Não usar para resposta a uma requisição recém-executada. |
| Loading de página/lista | Estado de carregamento do componente | Contexto permanece reconhecível | Usar primitive de loading da tabela/página; não mostrar dados como se estivessem atualizados. |
| Loading de mutação | Ação afetada desabilitada com indicador compacto | Dialog/formulário permanece aberto | Não bloquear toda a aplicação ou substituir o formulário por tela vazia. |
| Ação destrutiva, irreversível ou descarte | `ui.windows.confirm` | Não executar antes da confirmação | Descrever consequência e rotular a ação. A confirmação antecede a mutação. |
| Navegação com ciclo de janelas | `ui.navigation` | Coordena descarte, foco e superfícies | Usar para fluxos imperativos. `next/link` serve apenas a links declarativos sem esse ciclo de vida. |

## Regras obrigatórias

- Toda operação gera no máximo um toast para o seu resultado.
- Todo erro de API, domínio, rede e mutação chega ao toast por
  `ui.feedback.notifyError`; notificações já normalizadas continuam usando
  `ui.feedback.notify`.
- O dialog ou sheet que iniciou uma falha operacional permanece aberto.
- `Alert` dentro de dialog não exibe erros operacionais de submit/mutação.
- Validação de campo permanece inline e acessível; ela é a única exceção de
  erro apresentada dentro do formulário.
- Handlers assíncronos, comandos e mutações consomem suas rejeições. Não pode
  ocorrer `unhandledRejection` nem mensagem `[object Object]`.
- Fluxos voltados à pessoa usuária não usam `console.error` ou `console.warn`.
  Observabilidade técnica, se necessária, deve estar fora desse fluxo e nunca
  duplicar o feedback visual.
- Sonner é detalhe de adaptador. Features usam a fachada `ui.feedback`, não
  importam Sonner diretamente.

## Regras visuais complementares

- Controles compactos de formulário e ação usam `h-8` como altura padrão.
- Cabeçalhos e células de valores numéricos, quantitativos e monetários usam
  `text-right`.
- Cards, sidebars e demais superfícies estáticas são planos; sombras são
  restritas a modal, menu, dropdown, popover e tooltip.
- O toast deve usar mensagem compreensível e já normalizada pelo contrato. Não
  expor objetos serializados, pilhas, códigos internos isolados ou detalhes de
  transporte sem contexto.
