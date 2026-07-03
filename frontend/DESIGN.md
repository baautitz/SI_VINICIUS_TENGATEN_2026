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

### Inputs / Fields
- **Style:** Altura padrão (`h-8`), borda fina (`border-input`), cantos arredondados (`rounded-lg`).
- **Focus:** Contorno azul com anel suave (`focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50`).
- **Error:** Borda vermelha (`aria-invalid:border-destructive`) e anel avermelhado (`aria-invalid:ring-destructive/20`).

### Navigation
- **Sidebar**: Fundo quase branco (`bg-sidebar`), texto escuro, itens ativos destacados em azul suave (`bg-sidebar-accent`).

## 6. Do's and Don'ts

### Do:
- **Do** Alinhar sempre cabeçalhos de colunas monetárias e seus respectivos campos numéricos à direita (`text-right`).
- **Do** Manter a altura de botões e inputs padrão em `h-8` para consistência e alta densidade.
- **Do** Garantir que todo campo de input com erro de validação utilize `aria-invalid` para estilos corretos e acessibilidade.
- **Do** Utilizar fontes mono-espaçadas (`font-mono`) estritamente para códigos técnicos, SKUs e siglas de unidades de medida.

### Don't:
- **Don't** Usar cantos extremamente arredondados (`rounded-3xl` / `32px+`) em cartões ou botões normais.
- **Don't** Adicionar bordas coloridas espessas em um único lado (`border-left-4` / faixa lateral) para destacar cards ou alertas.
- **Don't** Utilizar degradês ou sombras gigantes com desfocagem maior que 16px para fins decorativos.
- **Don't** Usar texto em gradiente (`background-clip: text`) em títulos ou elementos da interface.
- **Don't** Usar fontes mono-espaçadas para identificadores simples de tabelas ou números sequenciais comuns (ex: `#1`, `#2`).
