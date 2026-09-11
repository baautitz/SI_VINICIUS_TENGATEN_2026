using System;
using Backend.Core.Common.Exceptions;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Common;

public static class DbExceptionTranslator
{
    private static readonly IReadOnlyDictionary<string, (string Code, string Message)> ReferencedRecordErrors =
        new Dictionary<string, (string Code, string Message)>(StringComparer.OrdinalIgnoreCase)
        {
            ["produtos_categoria_fk"] = ("CATEGORIA_EM_USO", "A categoria não pode ser removida porque possui produtos vinculados."),
            ["produtos_marca_fk"] = ("MARCA_EM_USO", "A marca não pode ser removida porque possui produtos vinculados."),
            ["produtos_unidade_fk"] = ("UNIDADE_MEDIDA_EM_USO", "A unidade de medida não pode ser removida porque possui produtos vinculados."),
            ["skus_produto_fk"] = ("PRODUTO_EM_USO", "O produto não pode ser removido porque possui SKUs vinculados."),
            ["sku_atributos_valores_chave_fk"] = ("ATRIBUTO_EM_USO", "O atributo não pode ser removido porque possui valores vinculados."),
            ["skus_atributos_valores_relacionamento_valor_fk"] = ("VALOR_ATRIBUTO_EM_USO", "O valor do atributo não pode ser removido porque está sendo utilizado por SKUs."),
            ["skus_atributos_valores_relacionamento_sku_fk"] = ("SKU_EM_USO", "O SKU não pode ser removido porque está sendo utilizado em outros registros."),
            ["bairros_cidade_fk"] = ("CIDADE_EM_USO", "A cidade não pode ser removida porque possui bairros vinculados."),
            ["cidades_estado_fk"] = ("ESTADO_EM_USO", "O estado não pode ser removido porque possui cidades vinculadas."),
            ["estados_pais_fk"] = ("PAIS_EM_USO", "O país não pode ser removido porque possui estados vinculados."),
            ["clientes_bairro_fk"] = ("BAIRRO_EM_USO", "O bairro não pode ser removido porque possui clientes vinculados."),
            ["clientes_nacionalidade_fk"] = ("PAIS_EM_USO", "O país não pode ser removido porque possui clientes vinculados."),
            ["emitentes_bairro_fk"] = ("BAIRRO_EM_USO", "O bairro não pode ser removido porque possui emitentes vinculados."),
            ["emitentes_nacionalidade_fk"] = ("PAIS_EM_USO", "O país não pode ser removido porque possui emitentes vinculados."),
            ["fornecedores_bairro_fk"] = ("BAIRRO_EM_USO", "O bairro não pode ser removido porque possui fornecedores vinculados."),
            ["fornecedores_nacionalidade_fk"] = ("PAIS_EM_USO", "O país não pode ser removido porque possui fornecedores vinculados."),
            ["transportadoras_bairro_fk"] = ("BAIRRO_EM_USO", "O bairro não pode ser removido porque possui transportadoras vinculadas."),
            ["transportadoras_nacionalidade_fk"] = ("PAIS_EM_USO", "O país não pode ser removido porque possui transportadoras vinculadas."),
            ["veiculos_transportadora_fk"] = ("TRANSPORTADORA_EM_USO", "A transportadora não pode ser removida porque possui veículos vinculados."),
            ["veiculos_estado_fk"] = ("ESTADO_EM_USO", "O estado não pode ser removido porque possui veículos vinculados."),
            ["condicoes_pagamentos_metodo_fk"] = ("METODO_PAGAMENTO_EM_USO", "O método de pagamento não pode ser removido porque possui condições de pagamento vinculadas."),
            ["condicoes_pagamentos_parcelas_condicao_fk"] = ("CONDICAO_PAGAMENTO_EM_USO", "A condição de pagamento não pode ser removida porque possui parcelas vinculadas."),
            ["contas_pagar_fornecedor_fk"] = ("FORNECEDOR_EM_USO", "O fornecedor não pode ser removido porque possui contas a pagar vinculadas."),
            ["contas_pagar_nfe_fk"] = ("NFE_EM_USO", "A NF-e não pode ser removida porque possui contas a pagar vinculadas."),
            ["contas_pagar_condicao_pagamento_fk"] = ("CONDICAO_PAGAMENTO_EM_USO", "A condição de pagamento não pode ser removida porque possui contas a pagar vinculadas."),
            ["contas_pagar_parcelas_conta_fk"] = ("CONTA_PAGAR_EM_USO", "A conta a pagar não pode ser removida porque possui parcelas vinculadas."),
            ["contas_receber_cliente_fk"] = ("CLIENTE_EM_USO", "O cliente não pode ser removido porque possui contas a receber vinculadas."),
            ["contas_receber_nfe_fk"] = ("NFE_EM_USO", "A NF-e não pode ser removida porque possui contas a receber vinculadas."),
            ["contas_receber_venda_fk"] = ("VENDA_EM_USO", "A venda não pode ser removida porque possui contas a receber vinculadas."),
            ["contas_receber_condicao_pagamento_fk"] = ("CONDICAO_PAGAMENTO_EM_USO", "A condição de pagamento não pode ser removida porque possui contas a receber vinculadas."),
            ["contas_receber_parcelas_conta_fk"] = ("CONTA_RECEBER_EM_USO", "A conta a receber não pode ser removida porque possui parcelas vinculadas."),
            ["nfes_emitente_fk"] = ("EMITENTE_EM_USO", "O emitente não pode ser removido porque possui NF-e vinculadas."),
            ["nfes_cliente_fk"] = ("CLIENTE_EM_USO", "O cliente não pode ser removido porque possui NF-e vinculadas."),
            ["nfes_transportadora_fk"] = ("TRANSPORTADORA_EM_USO", "A transportadora não pode ser removida porque possui NF-e vinculadas."),
            ["nfes_venda_fk"] = ("VENDA_EM_USO", "A venda não pode ser removida porque possui NF-e vinculadas."),
            ["nfes_itens_nfe_fk"] = ("NFE_EM_USO", "A NF-e não pode ser removida porque possui itens vinculados."),
            ["nfes_itens_sku_fk"] = ("SKU_EM_USO", "O SKU não pode ser removido porque possui itens de NF-e vinculados."),
            ["nfes_itens_unidade_fk"] = ("UNIDADE_MEDIDA_EM_USO", "A unidade de medida não pode ser removida porque possui itens de NF-e vinculados."),
            ["nfes_informacoes_adicionais_nfe_fk"] = ("NFE_EM_USO", "A NF-e não pode ser removida porque possui informações adicionais vinculadas."),
            ["nfes_pagamentos_nfe_fk"] = ("NFE_EM_USO", "A NF-e não pode ser removida porque possui pagamentos vinculados."),
            ["nfes_pagamentos_metodo_fk"] = ("METODO_PAGAMENTO_EM_USO", "O método de pagamento não pode ser removido porque possui pagamentos de NF-e vinculados."),
            ["nfes_transportes_nfe_fk"] = ("NFE_EM_USO", "A NF-e não pode ser removida porque possui transporte vinculado."),
            ["nfes_transportes_veiculo_fk"] = ("VEICULO_EM_USO", "O veículo não pode ser removido porque possui transportes vinculados."),
            ["vendas_emitente_fk"] = ("EMITENTE_EM_USO", "O emitente não pode ser removido porque possui vendas vinculadas."),
            ["vendas_cliente_fk"] = ("CLIENTE_EM_USO", "O cliente não pode ser removido porque possui vendas vinculadas."),
            ["vendas_itens_venda_fk"] = ("VENDA_EM_USO", "A venda não pode ser removida porque possui itens vinculados."),
            ["vendas_itens_sku_fk"] = ("SKU_EM_USO", "O SKU não pode ser removido porque possui itens de venda vinculados."),
            ["movimentacoes_estoque_usuario_fk"] = ("USUARIO_EM_USO", "O usuário não pode ser removido porque possui movimentações de estoque vinculadas."),
            ["movimentacoes_estoque_nfe_fk"] = ("NFE_EM_USO", "A NF-e não pode ser removida porque possui movimentações de estoque vinculadas."),
            ["movimentacoes_estoque_venda_fk"] = ("VENDA_EM_USO", "A venda não pode ser removida porque possui movimentações de estoque vinculadas."),
            ["movimentacoes_estoque_itens_mov_fk"] = ("MOVIMENTACAO_EM_USO", "A movimentação de estoque não pode ser removida porque possui itens vinculados."),
            ["movimentacoes_estoque_itens_sku_fk"] = ("SKU_EM_USO", "O SKU não pode ser removido porque possui movimentações de estoque vinculadas."),
            ["fk_sessoes_usuario"] = ("USUARIO_EM_USO", "O usuário não pode ser removido porque possui sessões vinculadas."),
            ["fk_auditoria_usuario"] = ("USUARIO_EM_USO", "O usuário não pode ser removido porque possui auditorias vinculadas."),
            ["fk_auditoria_sessao"] = ("SESSAO_EM_USO", "A sessão não pode ser removida porque possui auditorias vinculadas."),
        };

    public static Exception Translate(PostgresException ex)
    {
        return ex.SqlState switch
        {
            "23505" => new UniqueConstraintException("Este registro já existe.", ex),
            "23503" => TranslateReferencedRecord(ex),
            _ => ex
        };
    }

    private static Exception TranslateReferencedRecord(PostgresException ex)
    {
        if (ReferencedRecordErrors.TryGetValue(ex.ConstraintName ?? string.Empty, out var error))
            return new ReferencedRecordException(error.Code, error.Message, ex);

        return new ReferencedRecordException(
            "REGISTRO_EM_USO",
            "O registro não pode ser removido porque possui dados vinculados.",
            ex);
    }
}
