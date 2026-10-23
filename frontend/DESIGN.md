---
name: Sistema de Gestão ERP
description: Interface limpa, densa e de alta produtividade para operações comerciais.
colors:
  primary: "#2575c0"
  primary-foreground: "#f4fbfc"
  background: "#ffffff"
  foreground: "#212124"
  secondary: "#f5f5f6"
  secondary-foreground: "#333237"
  muted: "#f5f5f6"
  muted-foreground: "#82818a"
  accent: "#f5f5f6"
  accent-foreground: "#333237"
  destructive: "#c43c2c"
  border: "#e5e6e7"
  input: "#e5e6e7"
  ring: "#aab0b4"
typography:
  display:
    fontFamily: "Inter, sans-serif"
    fontSize: "1.875rem"
    fontWeight: 600
    lineHeight: 1.25
    letterSpacing: "-0.02em"
  body:
    fontFamily: "Inter, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: "normal"
rounded:
  sm: "4px"
  md: "6px"
  lg: "8px"
  xl: "12px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.primary-foreground}"
    rounded: "{rounded.lg}"
    padding: "8px 10px"
  button-secondary:
    backgroundColor: "{colors.secondary}"
    textColor: "{colors.secondary-foreground}"
    rounded: "{rounded.lg}"
    padding: "8px 10px"
  input-base:
    backgroundColor: "transparent"
    textColor: "{colors.foreground}"
    rounded: "{rounded.lg}"
    padding: "4px 10px"
---

# Design System: Sistema de Gestão ERP

## 1. Overview

**Creative North Star: "O Livro de Registro Soberano" (The Sovereign Ledger)**

Este sistema de design foi projetado para interfaces operacionais de alta densidade e produtividade extrema. Inspirado por ferramentas técnicas modernas e minimalistas (como as interfaces do Linear e da Vercel), ele prioriza clareza visual, legibilidade absoluta e fluxo ágil para entrada de dados.

O design afasta-se de decorações supérfluas e clichês SaaS (como cantos excessivamente arredondados, sombras gigantescas e fundos coloridos barulhentos), focando no alinhamento rigoroso, tipografia estruturada e contrastes limpos. A interface é concebida como uma ferramenta de trabalho confiável, estável e rápida para operadores de sistema.

**Key Characteristics:**

- **Densidade Otimizada**: Spacing compacto e elementos de controle de tamanho uniforme (`h-8`) para permitir a visualização de múltiplos registros simultaneamente sem poluição visual.
- **Teclado como Cidadão de Primeira Classe**: Layouts e comportamentos focados em navegação por atalhos e preenchimento ágil.
- **Contraste de Confiança**: Cores funcionais nítidas e ausência de cinzas lavados para evitar fadiga ocular após uso prolongado.

## 2. Colors

O esquema de cores utiliza OKLCH como fonte de verdade no código para transições suaves de tema, com representação hex sRGB na especificação técnica. O contraste é mantido rigorosamente acima de 4.5:1 para garantir legibilidade sob qualquer iluminação ambiente.

### Primary

- **Active Blue** (`#2575c0` / `oklch(0.52 0.105 223.128)`): Usado estritamente para ações principais, destaques de foco ativo e links prioritários.

### Neutral

- **Deep Ink** (`#212124` / `oklch(0.141 0.005 285.823)`): Texto de corpo principal e elementos estruturais de alta prioridade.
- **Muted Slate** (`#82818a` / `oklch(0.552 0.016 285.938)`): Rótulos secundários, placeholders e textos auxiliares de menor hierarquia.
- **Clean Border** (`#e5e6e7` / `oklch(0.92 0.004 286.32)`): Linhas delimitadoras e bordas de inputs.
- **Solid White** (`#ffffff` / `oklch(1 0 0)`): Fundo de cartões, popovers e área de trabalho geral.

### Named Rules

**A Regra da Sobriedade do Azul.** O azul primário é usado em menos de 10% de qualquer tela. Sua força está em ser raro, indicando apenas elementos acionáveis ou de foco.
**A Regra da Legibilidade Absoluta.** Textos secundários ou silenciados nunca devem ter contraste inferior a 4.5:1 contra o fundo. Se um cinza parecer difícil de ler, ele deve ser escurecido imediatamente.

## 3. Typography

**Display Font:** Inter, sans-serif
**Body Font:** Inter, sans-serif
**Label/Mono Font:** Inter, sans-serif

A tipografia do sistema é baseada na família Inter para garantir clareza mecânica e excelente renderização de texto e números em telas de diferentes resoluções. Letras muito próximas em cabeçalhos display são proibidas.

### Hierarchy

- **Display** (Semi-bold (600), `1.875rem` / 30px, `1.25` line-height): Título principal de telas ou seções globais.
- **Headline** (Semi-bold (600), `1.5rem` / 24px, `1.25` line-height): Subtítulos de grandes painéis ou grupos de recursos.
- **Title** (Medium (500), `1.125rem` / 18px, `1.5` line-height): Títulos de cartões, modais ou subtópicos.
- **Body** (Regular (400), `0.875rem` / 14px, `1.5` line-height, comprimento de linha limitado a `65-75ch` para prosa): Texto principal do sistema e tabelas de dados.
- **Label** (Medium (500), `0.75rem` / 12px, `normal` letter-spacing): Rótulos de formulários, metadados secundários e tags.

### Named Rules

**A Regra do Alinhamento de Números.** Todos os valores numéricos, quantitativos e monetários em tabelas e grades devem usar alinhamento à direita (`text-right`) para facilitar a varredura visual imediata.

## 4. Elevation

O sistema rejeita elevações volumétricas decorativas e sombras amplas e desfocadas. A profundidade é sugerida através de divisões finas (`border`) e sobreposição tonal plana.

### Shadow Vocabulary

- **Modal Overlay** (`box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05)`): Usado exclusivamente para dar destaque a modais, dropdowns e menus que se sobrepõem à tela principal.
- **Card Rest** (`box-shadow: none` / `ring-1 ring-foreground/10`): Cartões e painéis principais permanecem planos, delimitados por um contorno fino de 1px.

### Named Rules

**A Regra de Sombras Funcionais.** Sombras só aparecem em elementos temporários que flutuam sobre a interface (dropdowns, modais, tooltips). Elementos da página estática (cards, sidebars) nunca devem usar sombras.

## 5. Components

Os componentes são compactos, uniformes e desenhados para responder de forma imediata à interação do operador.

### Buttons

- **Shape:** Cantos levemente arredondados (`rounded-lg`, `0.45rem` / 7.2px)
- **Primary:** Fundo azul (`#2575c0`), texto branco (`#f4fbfc`), preenchimento interno compacto (`h-8`, `px-2.5`)
- **Hover / Focus:** Transição de opacidade no hover (`hover:bg-primary/80`); anel de foco azul-claro (`focus-visible:ring-3 focus-visible:ring-ring/50`) no foco por teclado.
- **Secondary:** Fundo cinza-claro (`bg-secondary`), texto escuro (`text-secondary-foreground`), hover com mistura suave (`hover:bg-secondary/90`).

### Cards / Containers

- **Corner Style:** Arredondamento médio-alto (`rounded-xl`, `12px` aproximado)
- **Background:** Branco sólido (`bg-card`)
- **Shadow Strategy:** Plano por padrão, contornado por `ring-1 ring-foreground/10`.
- **Internal Padding:** Espaçamento compacto de `p-4` (16px) a `p-6` (24px).

### Modal / Window layout

- **Structure:** Toda janela usa titlebar fixa, body com rolagem independente e footer de ações fixo/sticky.
- **Titlebar:** `DialogHeader`, `SheetHeader` ou `AlertDialogHeader` fica fora da área rolável e concentra título, descrição e fechamento.
- **Body:** `DialogBody`, `SheetBody` ou `AlertDialogBody` possui `min-h-0 flex-1 overflow-y-auto` e `p-1` como margem interna segura contra recorte de bordas e anéis de foco.
- **Footer:** Ações usam `data-window-actions`; o adaptador posiciona o footer no fim do body e o mantém visível durante a rolagem.
- **Avoid:** Não repetir o título dentro do formulário, não aplicar `overflow-y-auto` no `*Content` da janela e não criar footer específico por feature.

### Inputs / Fields

- **Style:** Altura padrão (`h-8`), borda fina (`border-input`), cantos arredondados (`rounded-lg`).
- **Focus:** Contorno azul com anel suave (`focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50`).
- **Error:** Borda vermelha (`aria-invalid:border-destructive`) e anel avermelhado (`aria-invalid:ring-destructive/20`).

### Feedback and transient states

- **Field validation:** Erros de preenchimento pertencem ao campo: usar `aria-invalid`, mensagem inline e foco no primeiro campo inválido quando a ação de envio for bloqueada.
- **Operational failure:** Falhas de API, domínio, rede e mutação são transientes e aparecem uma única vez em toast. Não usar `Alert` dentro de um dialog para repetir a mesma mensagem; o dialog permanece aberto.
- **Persistent state:** `Alert` é reservado a informação que continua válida enquanto a pessoa observa a tela, como uma venda já cancelada ou uma restrição contextual. Não é um canal para resposta de uma operação recém-executada.
- **Loading:** Preservar o contexto da superfície. Em tabela, usar o estado de carregamento da tabela; em formulário ou ação local, desabilitar apenas a ação afetada e usar o indicador compacto correspondente. Não trocar toda a tela por um spinner para uma mutação local.
- **Toast:** Sucesso, informação, warning e erro usam o mesmo sistema de feedback. Um único resultado de operação gera no máximo um toast. Posição `bottom-right`; duração 4s (sucesso/info), 6s (warning) e 8s (erro). Erros de negócio (4xx) mostram a mensagem do servidor (título até ~200 caracteres, o restante em descrição); exceções, 5xx e falhas inesperadas mostram só a mensagem amigável. O toast continua interativo com janela aberta e clicar nele não fecha a janela.
- **Confirmation:** Ações destrutivas, irreversíveis ou de descarte usam confirmação antes da mutação, nunca uma mensagem de erro posterior como substituto de confirmação.

### Navigation

- **Sidebar**: Fundo quase branco (`bg-sidebar`), texto escuro, itens ativos destacados em azul suave (`bg-sidebar-accent`).
- **Behavior:** Navegação de fluxo que possa precisar encerrar superfícies usa `ui.navigation`; links sem ciclo de vida adicional podem usar `next/link`. A escolha deve preservar foco, descarte e estado de janela previstos pelo fluxo.

### Action grammar (ações, ícones, badges, atalhos)

Regras para que a mesma ação tenha sempre o mesmo rótulo, ícone, variant e atalho em todas as telas.

#### 1. Vocabulário (verbo = significado, sempre)
| Verbo | Significa | Reversível? | Não usar para |
|---|---|---|---|
| Salvar | persiste cadastro/rascunho | sim | movimentar estoque/financeiro |
| Lançar | gera efeito em estoque/financeiro (movimentação, título) | só por estorno | cadastro |
| Confirmar | confirmação genérica (ConfirmWindow) ou etapa final de wizard | n/a | ação que já tem nome ("Estornar", não "Confirmar estorno") |
| Excluir | apaga registro gravado | não | linha ainda em edição |
| Remover | tira item de coleção não gravada (linha de grid/itens) | sim, até salvar | registro persistido |
| Estornar | reverte lançamento efetivado com contra-lançamento; mantém histórico | não | cadastros |
| Cancelar (sozinho) | sai da janela sem fazer nada | n/a | mudar status de documento |
| Cancelar <Objeto> | encerra documento sem apagar (NF-e, balanço, venda); sempre com substantivo | não | sair de janela |
| Fechar | sai de janela só-leitura | n/a | janela editável; mudar status ("Finalizar balanço") |
| Voltar | navegar entre etapas de wizard | sim | sair de diálogo |

#### 2. Ações: ícone / variant / size / atalho
| Ação | Ícone (lucide) | Variant | Size | Atalho | Observação |
|---|---|---|---|---|---|
| Novo registro (header) | Plus | default | default | Alt+N | |
| Adicionar linha/parcela | Plus | outline | default/sm | contextual | `<Plus />` sem classe |
| Salvar / Lançar / Confirmar | Check (opcional) | default | default | Alt+Enter | 1 primário por janela |
| Editar | Pencil | ghost | icon-sm | Alt+E | nunca junto de Eye |
| Visualizar | Eye | ghost | icon-sm | | só quando não há Editar |
| Excluir | Trash2 | destructive | icon-sm | Delete | confirmação obrigatória |
| Remover item de formulário | Trash2 ou X | ghost | icon-sm | | rótulo "Remover" |
| Estornar / Devolver | Undo2 | outline (abre janela) / destructive (confirma) | icon-sm | Alt+Z na janela; Delete na listagem | Delete na listagem = ação destrutiva da linha (excluir ou estornar), sempre com janela/confirmação (decidido) |
| Cancelar documento | Ban | destructive | default/icon-sm | sem tecla única | com motivo |
| Reabrir baixa de parcela | RotateCcw | ghost | icon-sm | | |
| Baixar/Pagar título | Check | ghost | icon-sm | | sem verde cru |
| Buscar | Search | outline | default | Alt+Q | |
| Saída da janela | sem ícone | outline | default | Esc | Cancelar/Fechar |
| Navegação textual | sem ícone | link | default | | |

Botões de ação de negócio na linha da listagem (baixar, estornar, cancelar) usam `RowAction` de `@/utils/table-columns`, que fixa ícone e variant por tipo; nunca montar `Button` manual para essas ações. Ordem na linha: ação de negócio, Visualizar/Editar, Excluir.

Regras: ícone-only sempre com `aria-label`; Kbd só em Cancelar/Fechar (Esc), primário (Alt+Enter) e ações principais da janela, nunca em botão de linha; sem `h-/w-` manual nem cores cruas (`text-red-*`, `bg-emerald-*`).

#### 3. Badges (não existe Chip; Badge cobre)
| Estado | Variant |
|---|---|
| Ativo, Pago, Emitida | success |
| Aberto, Pendente | warning |
| Parcial | info |
| Cancelado, Estornado | destructive |
| Inativo, Rascunho | secondary |
| Categoria (ex.: Física/Jurídica) | outline (sem cor) |
Texto capitalizado ("Estornada"), um mapa status → variant, sem `hover` (Badge não é clicável). Exceção legítima: `ComboboxChip` em multi-seleção.

#### 4. Rodapé de janela
Esquerda (`mr-auto`): ações relacionadas/navegação. Direita: saída (Cancelar/Fechar, Esc) e depois o primário (Alt+Enter). Destrutivo nunca misturado ao grupo neutro. Sempre `WindowActions`; sem rodapé próprio por feature.

#### 5. Atalhos
- Globais reservados: N (novo), Q (busca), S/B/H (sidebar). Janelas não os reutilizam.
- Alt+Enter sempre submete a janela; Alt+→/← navega etapas.
- Destrutivo sem atalho de tecla única em janela; na listagem, Delete dispara a ação destrutiva da linha (Excluir ou Estornar) e sempre abre confirmação (decidido).
- Sidebar mantém Alt+S ("Buscar no menu") e Alt+B ("Recolher menu") (decidido). Quem cede: "Salvar rascunho" em balanços → Alt+R; "Ver estorno" em movimentações → Alt+V; "Baixar XML" na NF-e → Alt+D.
- Tabela de atalhos por escopo mantida no DESIGN.md; conflitos: Alt+S resolvido (sidebar mantém); falta decidir Alt+K (Cancelar NF-e) e estorno (Alt+S/Z/D → Alt+Z).

#### 6. O que NÃO usar
Chip genérico; Trash2 para cancelar/estornar; Pencil para visualizar; "Voltar" como saída de diálogo; "Fechar" para mudar status; `secondary` no lugar de `outline`; `title=` sem `aria-label`; `border-none hover:*` em Badge; cores fora dos tokens.

#### 7. Usos duplos legítimos
Plus (linha inline e novo registro); Eye (ver e "consultar" em modal só-leitura); Undo2 (estorno e devolução parcial); "Cancelar" (sair) e "Cancelar NF-e" (status) desde que o segundo leve o substantivo; "Baixar/Estornar" alternando por estado.

## 6. Do's and Don'ts

### Do:

- **Do** Alinhar sempre cabeçalhos de colunas monetárias e seus respectivos campos numéricos à direita (`text-right`).
- **Do** Manter a altura de botões e inputs padrão em `h-8` para consistência e alta densidade.
- **Do** Garantir que todo campo de input com erro de validação utilize `aria-invalid` para estilos corretos e acessibilidade.
- **Do** Utilizar fontes mono-espaçadas (`font-mono`) estritamente para códigos técnicos, SKUs e siglas de unidades de medida.
- **Do** Usar `h-8` como altura padrão de controles compactos; exceções precisam ser semânticas e documentadas, não ajustes visuais locais.
- **Do** Manter mensagens de validação junto ao campo e feedback operacional no toast.
- **Do** Usar sombra somente em superfícies temporárias sobrepostas, como menu, popover, tooltip e modal.

### Don't:

- **Don't** Usar cantos extremamente arredondados (`rounded-3xl` / `32px+`) em cartões ou botões normais.
- **Don't** Adicionar bordas coloridas espessas em um único lado (`border-left-4` / faixa lateral) para destacar cards ou alertas.
- **Don't** Utilizar degradês ou sombras gigantes com desfocagem maior que 16px para fins decorativos.
- **Don't** Usar texto em gradiente (`background-clip: text`) em títulos ou elementos da interface.
- **Don't** Usar fontes mono-espaçadas para identificadores simples de tabelas ou números sequenciais comuns (ex: `#1`, `#2`).
- **Don't** Exibir a resposta de erro de API dentro de um dialog como `Alert` ou duplicá-la em mais de um toast.
- **Don't** Aplicar `shadow-*` em cards, sidebar ou outras superfícies estáticas.
