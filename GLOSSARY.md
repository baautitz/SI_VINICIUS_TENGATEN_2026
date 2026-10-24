# Sistema de Gestão

Sistema de gestão (compras, vendas, estoque, financeiro e NF-e) com backend .NET e frontend web.

## Língua

**Toast**:
Notificação transitória de feedback exibida sobre a tela, pelo mesmo sistema de feedback para sucesso, erro, aviso e informação, diferenciados por severidade.
_Avoid_: Alerta, snackbar, notificação (ambíguo)

**Visualização**:
Modo de um formulário em que os dados são apenas lidos, sem edição. Os campos continuam legíveis e copiáveis.
_Avoid_: Somente leitura, desabilitado (ambíguo)

**Bloqueio condicional**:
Estado de um campo indisponível naquele momento por uma regra de negócio ou de fluxo, mesmo com o formulário em edição.
_Avoid_: Desabilitado (ambíguo)

**Atalho**:
Combinação de teclas que aciona uma ação sem o mouse, como Alt+Enter para confirmar. Vale na janela ou tela em que está registrado.
_Avoid_: Hotkey, tecla de atalho

**Dica de atalho**:
Indicação visual do atalho exibida na tela, junto da ação que ele aciona.
_Avoid_: Kbd, legenda de teclado
