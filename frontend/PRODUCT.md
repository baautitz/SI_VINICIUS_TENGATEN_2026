# Product

## Register

product

## Users
Administradores, gerentes e operadores de sistema (como caixas, estoquistas e faturistas). Eles trabalham em ambientes dinâmicos e precisam inserir e consultar dados de forma ágil e sem atrito. O objetivo é concluir tarefas operacionais diárias (como faturamento de vendas, controle de estoque e conciliação financeira) com o menor número de cliques e a máxima precisão possível.

## Product Purpose
Simular um sistema de gestão integrado (ERP) completo e acadêmico que demonstre excelência em arquitetura fullstack, integração limpa, usabilidade impecável e robustez de banco de dados. O sucesso é definido por uma experiência fluida de cadastro e consulta, transações confiáveis e tempos de resposta rápidos na interface.

## Brand Personality
- **Profissional**: Transmite seriedade, segurança e solidez comercial.
- **Eficiente**: Focado em produtividade, legibilidade e clareza de fluxo.
- **Moderno**: Minimalista, limpo e direto ao ponto, inspirando-se em ferramentas técnicas contemporâneas.

## Anti-references
- **Dashboards SaaS Genéricos**: Evitar o excesso de cores berrantes, cards com sombras enormes e degradês exagerados.
- **Interfaces Corporativas Clássicas**: Evitar layouts densos com tabelas cinzentas sem contraste, fontes difíceis de ler e poluição visual de campos desnecessários.
- **Gimmicks Visuais**: Sem animações intrusivas ou decorações desnecessárias que atrasem o fluxo de trabalho do operador.

## Design Principles
1. **Densidade Equilibrada**: Apresentar dados complexos de forma organizada, maximizando a visibilidade das informações críticas sem sobrecarregar o usuário.
2. **Foco na Entrada de Dados**: Facilitar a entrada via teclado (com hotkeys inteligentes e focos automáticos) para que operadores rápidos trabalhem de forma ininterrupta.
3. **Clareza de Estado**: Fornecer feedback imediato e inequívoco sobre erros de validação, estados de carregamento e sucesso de operações.
4. **Consistência de Fluxo**: Manter os mesmos padrões de navegação, tabelas e modais de inserção (Upsert) em todos os módulos do ERP (Vendas, Estoque, Financeiro, etc.).

## Contrato de Feedback da Interação

Todo resultado operacional tem uma apresentação previsível. Erros de API,
domínio, rede e mutação produzem um único toast. O dialog ou sheet que iniciou
a operação permanece aberto, preservando os dados preenchidos para que a
pessoa possa entender o resultado e continuar com segurança. Falhas
operacionais nunca são duplicadas como `Alert` dentro dessa superfície nem
registradas com `console.error` ou `console.warn` durante o fluxo de uso.

A validação de campo é diferente de propósito: campos obrigatórios, inválidos
ou malformados são apresentados inline junto ao campo e marcados com
`aria-invalid`. Assim a pessoa identifica exatamente o que precisa corrigir
sem confundir validação com falha do sistema.

Sucesso, informação, warnings, loading, confirmações e navegação seguem a
matriz canônica em `docs/ui-feedback.md`. Uma operação pode produzir no máximo
um toast. `Alert` persistente fica restrito a estados contextuais duradouros,
não à resposta de uma requisição recém-executada.

A fronteira backend/frontend usa um envelope estruturado com `success`, `data` e
`errors`; cada erro possui `code` estável, `message` compreensível e `field`
opcional. Isso torna o feedback consistente em todos os módulos do ERP.

## Accessibility & Inclusion
- Contraste em conformidade com as diretrizes WCAG AA para garantir legibilidade sob diferentes condições de luz.
- Suporte completo a navegação por teclado e compatibilidade com leitores de tela em componentes interativos (utilizando primitives Radix / Shadcn).
- Respeito à preferência de redução de movimento do sistema operacional (`prefers-reduced-motion: reduce`).
