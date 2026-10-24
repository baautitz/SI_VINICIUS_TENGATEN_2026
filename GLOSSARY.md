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

**Emitente**:
Parte cadastrada que emite as Vendas e Compras do sistema e consta como emitente na NF-e.
_Avoid_: Empresa, remetente

**Emitente padrão**:
O único Emitente marcado como "Padrão", pré-selecionado ao criar um registro que pede um Emitente. Pode não existir, e nunca é um Emitente inativo.
_Avoid_: Favorito, principal

**Nota Fiscal**:
Documento fiscal (NF-e) emitido por uma Venda ou recebido numa Compra. Distinta da Compra e da Venda, que são as operações do sistema.
_Avoid_: Nota (ambíguo)

**Item**:
Linha de uma Compra, Venda, Movimentação ou Balanço, que refere um SKU e uma quantidade.
_Avoid_: Linha, produto da nota

**SKU do item**:
Código do SKU de um Item já adicionado, mostrado em Visualização.
_Avoid_: Campo de SKU (ambíguo com o Campo de adição de SKU)

**Campo de adição de SKU**:
Campo que busca um SKU e o adiciona como novo Item.
_Avoid_: Campo de SKU, pesquisa de produto

**Compra**:
Operação em que o Emitente recebe mercadoria de um fornecedor, com seus Itens, e que gera Parcelas da compra. Pode ter uma Nota Fiscal recebida.
_Avoid_: Pedido, entrada

**Parcelas da compra**:
Contas a pagar geradas por uma Compra, tanto da mercadoria quanto do frete.
_Avoid_: Contas vinculadas, títulos

**Devolução**:
Retorno de Itens de uma Venda pelo cliente, com motivo, que devolve os Itens ao estoque e abate o valor devolvido da conta a receber da Venda. Uma Venda pode ter várias Devoluções até esgotar o saldo devolvível dos seus Itens.
_Avoid_: Estorno (é o desfazer de uma baixa ou compra), cancelamento

**Devolução parcial**:
Devolução em que o cliente escolhe as quantidades de cada Item a devolver, dentro do saldo devolvível.
_Avoid_: Devolução simples

**Devolução total**:
Devolução de todo o saldo devolvível de todos os Itens da Venda. Após ela a Venda não aceita nova Devolução.
_Avoid_: Devolução completa
