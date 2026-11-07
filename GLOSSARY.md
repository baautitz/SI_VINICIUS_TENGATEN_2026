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

**NF-e de devolução**:
Nota Fiscal emitida pela loja ao registrar uma Devolução (de venda ou de compra) cuja Nota Fiscal original está ativa. Referencia a chave da nota original e leva só os Itens devolvidos. Na Devolução de venda é uma nota de entrada; na de compra, de saída. A nota original não é cancelada, e a NF-e de devolução não se cancela: o Estorno de devolução emite a NF-e de estorno da devolução.
_Avoid_: Nota de crédito, cancelamento da nota

**NF-e de estorno da devolução**:
Nota Fiscal emitida no Estorno de devolução, no sentido oposto ao da NF-e de devolução e referenciando-a. Só existe se a Devolução emitiu NF-e de devolução.

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

**Devolução de venda**:
Retorno de Itens de uma Venda pelo cliente, com motivo, que devolve os Itens ao estoque e dá Abatimento nas parcelas a receber da Venda. O que passar do saldo em aberto vira Reembolso. Uma Venda pode ter várias Devoluções de venda até esgotar o saldo devolvível dos seus Itens.
_Avoid_: Devolução (sem qualificar), Estorno (é o desfazer de uma baixa ou compra), cancelamento

**Devolução de compra**:
Retorno de Itens de uma Compra ao fornecedor, com motivo, total ou parcial, limitada ao saldo em estoque dos Itens. É o espelho da Devolução de venda no lado da compra: dá Abatimento nas parcelas a pagar da Compra e o que passar do saldo em aberto vira Reembolso do fornecedor.
_Avoid_: Devolução (sem qualificar), Estorno de compra (é o desfazer da Compra), cancelamento

**Abatimento**:
Baixa não monetária de uma parcela a receber (Devolução de venda) ou a pagar (Devolução de compra). Reduz o saldo em aberto sem alterar o valor original da parcela nem o valor recebido, e cada Devolução registra quais parcelas abateu e quanto.

**Ordem do Abatimento**:
Ordem em que uma Devolução consome o saldo em aberto das parcelas, igual para venda (a receber) e compra (a pagar): primeiro as parcelas vencidas, da mais antiga para a mais recente; depois as a vencer, da última para a primeira. Parcela que vence hoje ainda não está vencida.
_Avoid_: Prioridade de abatimento
_Avoid_: Desconto, redução do valor da parcela

**Reembolso**:
Conta a pagar ao cliente gerada por uma Devolução de venda, ou conta a receber do fornecedor gerada por uma Devolução de compra, quando o valor devolvido passa do saldo em aberto da Venda, de modo que parte já recebida (ou paga, na compra) precisa ser devolvida. É um título único, vencendo na data da Devolução.
_Avoid_: Estorno, crédito do cliente

**Devolução parcial**:
Devolução de venda ou de compra em que se escolhem as quantidades de cada Item a devolver, dentro do saldo devolvível.
_Avoid_: Devolução simples

**Devolução total**:
Devolução de venda de todo o saldo devolvível de todos os Itens da Venda, ou Devolução de compra de todo o saldo devolvível dos Itens da Compra. Após ela a Venda ou a Compra não aceita nova Devolução.
_Avoid_: Devolução completa

**Parcela baixada**:
Parcela de conta a pagar ou a receber com algum valor pago ou recebido, ou com Abatimento por Devolução. Nenhum fluxo pode alterá-la, excluí-la ou cancelá-la, nem o documento de origem pode ser estornado, até a baixa ser estornada. A Devolução (de venda ou de compra) é a única exceção, e só consome saldo em aberto, nunca o valor já recebido ou pago.
_Avoid_: Parcela paga (deixa de fora a parcial e o abatimento)

**Estorno de devolução**:
Desfazimento de uma Devolução de venda ou de compra, com motivo: os Itens saem de novo do estoque, os Abatimentos são desfeitos nas parcelas a receber e o Reembolso é cancelado, se existir. O estorno é barrado se o Reembolso já tiver baixa. A Devolução de venda fica registrada como estornada e libera o saldo devolvível dos Itens.
Na Devolução de compra vale o espelho: os Itens voltam ao estoque, os Abatimentos são desfeitos nas parcelas a pagar e o Reembolso do fornecedor é cancelado, barrado se já tiver recebimento.
_Avoid_: Cancelamento de devolução, exclusão de devolução

**Unidade de medida**:
Unidade em que o estoque e as quantidades dos Itens de um produto são contados (UN, KG, CX). Cada produto tem uma só, e ela vale para todos os seus SKUs.
_Avoid_: Unidade, UM

**Permite decimais**:
Característica da Unidade de medida que diz se as quantidades dos Itens podem ser fracionadas (KG) ou só inteiras (UN).
_Avoid_: Fracionável, casas decimais

**Troca de unidade**:
Mudança da Unidade de medida de um produto já cadastrado. É barrada quando o produto já tem estoque ou movimentação, porque as quantidades registradas perderiam o significado.
_Avoid_: Conversão de unidade (o sistema não converte quantidades)

**Ordenação padrão**:
Critério com que uma listagem abre, sempre com desempate estável. Operações abrem pela mais recente; contas a pagar e a receber abrem com pendentes e vencidas antes das pagas, por vencimento crescente; parceiros, catálogo e referência abrem em ordem alfabética. A pesquisa por texto continua mandando quando há termo. O usuário pode trocá-la ordenando pelo cabeçalho da coluna.
_Avoid_: Ordem natural, ordem de cadastro
