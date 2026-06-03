using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Pagamentos.Entities;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.NFe;

public class NfesRepository : INfesRepository
{
    private readonly DbSession _session;

    public NfesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Nfes>> ObterNfes(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM nfes;";

        const string querySql = @"
            SELECT n.id AS Id, n.chave_acesso, n.numero, n.serie, n.data_emissao, n.data_saida,
                   n.emitente_nome_razaosocial, n.emitente_cpf_cnpj, n.emitente_rg_ie,
                   n.emitente_apelido_nomefantasia, n.emitente_endereco, n.emitente_bairro,
                   n.emitente_telefone, n.emitente_email,
                   n.cliente_nome_razaosocial, n.cliente_cpf_cnpj, n.cliente_rg_ie,
                   n.cliente_apelido_nomefantasia, n.cliente_endereco, n.cliente_bairro,
                   n.cliente_telefone, n.cliente_email,
                   n.tipo_operacao, n.status_nfe,
                   n.valor_produtos, n.valor_desconto, n.valor_frete, n.valor_seguro,
                   n.valor_outras_despesas, n.valor_total,
                   e.id AS EmitenteId, e.nome_razao_social, e.cpf_cnpj, e.ativo,
                   c.id AS ClienteId, c.nome_razao_social, c.cpf_cnpj, c.ativo
            FROM nfes n
            JOIN emitentes e ON e.id = n.emitente_id
            JOIN clientes c ON c.id = n.cliente_id
            ORDER BY n.data_emissao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var nfes = (await _session.Connection.QueryAsync<Nfes, Emitentes, Clientes, Nfes>(
            querySql,
            (nfe, emitente, cliente) =>
            {
                nfe.AtualizarEmitente(emitente);
                nfe.AtualizarCliente(cliente);
                nfe.DefinirItens(new List<NfesItens>());
                nfe.DefinirPagamentos(new List<NfesPagamentos>());
                return nfe;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "EmitenteId,ClienteId")).ToList();

        if (nfes.Count > 0)
        {
            var ids = nfes.Select(n => n.Id).ToArray();

            const string itensSql = @"
                SELECT ni.id AS Id, ni.numero_item, ni.descricao_item, ni.quantidade, ni.valor_unitario,
                       ni.valor_desconto, ni.valor_total, ni.nfe_id,
                       s.sku AS Sku, s.gtin_ean, s.preco, s.estoque, s.ativo,
                       u.id AS UnidadeMedidaId, u.sigla, u.descricao, u.categoria, u.ativo
                FROM nfes_itens ni
                JOIN skus s ON s.sku = ni.sku
                JOIN unidades_medida u ON u.id = ni.unidade_medida_id
                WHERE ni.nfe_id = ANY(@Ids)
                ORDER BY ni.nfe_id, ni.numero_item;";

            const string pagamentosSql = @"
                SELECT np.id AS Id, np.indicador_pagamento, np.valor_pagamento, np.nfe_id,
                       mp.id AS MetodosPagamentoId, mp.codigo, mp.descricao, mp.ativo
                FROM nfes_pagamentos np
                JOIN metodos_pagamento mp ON mp.id = np.metodo_pagamento_id
                WHERE np.nfe_id = ANY(@Ids);";

            var itens = (await _session.Connection.QueryAsync<NfesItens, Skus, UnidadesMedida, NfesItens>(
                itensSql,
                (item, sku, unidade) =>
                {
                    sku.DefinirAtributos(new List<SkusAtributosValores>());
                    item.AtualizarSku(sku);
                    item.AtualizarUnidadeMedida(unidade);
                    return item;
                },
                new { Ids = ids },
                transaction: _session.Transaction,
                splitOn: "Sku,UnidadeMedidaId")).ToList();

            var pagamentos = (await _session.Connection.QueryAsync<NfesPagamentos, MetodosPagamentos, NfesPagamentos>(
                pagamentosSql,
                (pag, metodo) =>
                {
                    pag.AtualizarMetodosPagamento(metodo);
                    return pag;
                },
                new { Ids = ids },
                transaction: _session.Transaction,
                splitOn: "MetodosPagamentoId")).ToList();

            var itensPorNfe = itens.GroupBy(i => i.NfeId).ToDictionary(g => g.Key, g => g.AsEnumerable());
            var pagamentosPorNfe = pagamentos.GroupBy(p => p.NfeId).ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var nfe in nfes)
            {
                if (itensPorNfe.TryGetValue(nfe.Id, out var sub)) nfe.DefinirItens(sub.ToList());
                if (pagamentosPorNfe.TryGetValue(nfe.Id, out var pags)) nfe.DefinirPagamentos(pags.ToList());
            }
        }

        return new ResultadoPaginado<Nfes>(nfes, total, pagina, tamanhoDaPagina);
    }

    public async Task<Nfes?> ObterNfePorId(int id)
    {
        const string nfeSql = @"
            SELECT n.id AS Id, n.chave_acesso, n.numero, n.serie, n.data_emissao, n.data_saida,
                   n.emitente_nome_razaosocial, n.emitente_cpf_cnpj, n.emitente_rg_ie,
                   n.emitente_apelido_nomefantasia, n.emitente_endereco, n.emitente_bairro,
                   n.emitente_telefone, n.emitente_email,
                   n.cliente_nome_razaosocial, n.cliente_cpf_cnpj, n.cliente_rg_ie,
                   n.cliente_apelido_nomefantasia, n.cliente_endereco, n.cliente_bairro,
                   n.cliente_telefone, n.cliente_email,
                   n.tipo_operacao, n.status_nfe,
                   n.valor_produtos, n.valor_desconto, n.valor_frete, n.valor_seguro,
                   n.valor_outras_despesas, n.valor_total,
                   e.id AS EmitenteId, e.nome_razao_social, e.cpf_cnpj, e.apelido_nome_fantasia, e.endereco,
                   e.telefone, e.email, e.rg_ie, e.inscricao_municipal, e.regime_tributario,
                   e.ativo, e.criado_em, e.observacao,
                   c.id AS ClienteId, c.nome_razao_social, c.cpf_cnpj, c.rg_ie, c.apelido_nome_fantasia,
                   c.endereco, c.telefone, c.email, c.limite_credito, c.ativo, c.criado_em,
                   c.observacao
            FROM nfes n
            JOIN emitentes e ON e.id = n.emitente_id
            JOIN clientes c ON c.id = n.cliente_id
            WHERE n.id = @Id;";

        const string itensSql = @"
            SELECT ni.id AS Id, ni.numero_item, ni.descricao_item, ni.quantidade, ni.valor_unitario,
                   ni.valor_desconto, ni.valor_total,
                   s.sku AS Sku, s.gtin_ean, s.preco, s.estoque, s.ativo,
                   u.id AS UnidadeMedidaId, u.sigla, u.descricao, u.categoria, u.ativo
            FROM nfes_itens ni
            JOIN skus s ON s.sku = ni.sku
            JOIN unidades_medida u ON u.id = ni.unidade_medida_id
            WHERE ni.nfe_id = @Id
            ORDER BY ni.numero_item;";

        const string pagamentosSql = @"
            SELECT np.id AS Id, np.indicador_pagamento, np.valor_pagamento,
                   mp.id AS MetodosPagamentoId, mp.codigo, mp.descricao, mp.ativo
            FROM nfes_pagamentos np
            JOIN metodos_pagamento mp ON mp.id = np.metodo_pagamento_id
            WHERE np.nfe_id = @Id;";

        const string infoSql = @"
            SELECT id, informacoes_fisco, informacoes_complementares
            FROM nfes_informacoes_adicionais
            WHERE nfe_id = @Id;";

        var nfe = (await _session.Connection.QueryAsync<Nfes, Emitentes, Clientes, Nfes>(
            nfeSql,
            (n, emitente, cliente) =>
            {
                n.AtualizarEmitente(emitente);
                n.AtualizarCliente(cliente);
                n.DefinirItens(new List<NfesItens>());
                n.DefinirPagamentos(new List<NfesPagamentos>());
                return n;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "EmitenteId,ClienteId")).SingleOrDefault();

        if (nfe is null) return null;

        var itens = await _session.Connection.QueryAsync<NfesItens, Skus, UnidadesMedida, NfesItens>(
            itensSql,
            (item, sku, unidade) =>
            {
                sku.DefinirAtributos(new List<SkusAtributosValores>());
                item.AtualizarSku(sku);
                item.AtualizarUnidadeMedida(unidade);
                return item;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "Sku,UnidadeMedidaId");

        nfe.DefinirItens(itens.ToList());

        var pagamentos = await _session.Connection.QueryAsync<NfesPagamentos, MetodosPagamentos, NfesPagamentos>(
            pagamentosSql,
            (pag, metodo) => { pag.AtualizarMetodosPagamento(metodo); return pag; },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "MetodosPagamentoId");

        nfe.DefinirPagamentos(pagamentos.ToList());

        var informacoesAdicionais = await _session.Connection.QuerySingleOrDefaultAsync<NfesInformacoesAdicionais>(
            infoSql, new { Id = id }, transaction: _session.Transaction);

        if (informacoesAdicionais is not null)
            nfe.AtualizarInformacoesAdicionais(informacoesAdicionais);

        return nfe;
    }

    public async Task<Nfes> CriarNfe(Nfes nfe)
    {
        const string sql = @"
            INSERT INTO nfes (
                chave_acesso, numero, serie, data_emissao, data_saida,
                emitente_nome_razaosocial, emitente_cpf_cnpj, emitente_rg_ie,
                emitente_apelido_nomefantasia, emitente_endereco, emitente_bairro,
                emitente_telefone, emitente_email,
                cliente_nome_razaosocial, cliente_cpf_cnpj, cliente_rg_ie,
                cliente_apelido_nomefantasia, cliente_endereco, cliente_bairro,
                cliente_telefone, cliente_email,
                tipo_operacao, status_nfe,
                valor_produtos, valor_desconto, valor_frete, valor_seguro,
                valor_outras_despesas, valor_total,
                emitente_id, cliente_id)
            VALUES (
                @ChaveAcesso, @Numero, @Serie, @DataEmissao, @DataSaida,
                @EmitenteNomeRazaosocial, @EmitenteCpfCnpj, @EmitenteRgIe,
                @EmitenteApelidoNomefantasia, @EmitenteEndereco, @EmitenteBairro,
                @EmitenteTelefone, @EmitenteEmail,
                @ClienteNomeRazaosocial, @ClienteCpfCnpj, @ClienteRgIe,
                @ClienteApelidoNomefantasia, @ClienteEndereco, @ClienteBairro,
                @ClienteTelefone, @ClienteEmail,
                @TipoOperacao, @StatusNfe,
                @ValorProdutos, @ValorDesconto, @ValorFrete, @ValorSeguro,
                @ValorOutrasDespesas, @ValorTotal,
                @EmitenteId, @ClienteId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                nfe.ChaveAcesso,
                nfe.Numero,
                nfe.Serie,
                nfe.DataEmissao,
                nfe.DataSaida,
                nfe.EmitenteNomeRazaosocial,
                nfe.EmitenteCpfCnpj,
                nfe.EmitenteRgIe,
                nfe.EmitenteApelidoNomefantasia,
                nfe.EmitenteEndereco,
                nfe.EmitenteBairro,
                nfe.EmitenteTelefone,
                nfe.EmitenteEmail,
                nfe.ClienteNomeRazaosocial,
                nfe.ClienteCpfCnpj,
                nfe.ClienteRgIe,
                nfe.ClienteApelidoNomefantasia,
                nfe.ClienteEndereco,
                nfe.ClienteBairro,
                nfe.ClienteTelefone,
                nfe.ClienteEmail,
                nfe.TipoOperacao,
                nfe.StatusNfe,
                nfe.ValorProdutos,
                nfe.ValorDesconto,
                nfe.ValorFrete,
                nfe.ValorSeguro,
                nfe.ValorOutrasDespesas,
                nfe.ValorTotal,
                EmitenteId = nfe.Emitente.Id,
                ClienteId = nfe.Cliente.Id
            },
            transaction: _session.Transaction);

        nfe.Id = idGerado;

        await InserirItens(idGerado, nfe.NfesItens);
        await InserirPagamentos(idGerado, nfe.NfesPagamentos);

        if (nfe.NfesInformacoesAdicionais is not null)
            await InserirInformacoesAdicionais(idGerado, nfe.NfesInformacoesAdicionais);

        return nfe;
    }

    public async Task<Nfes> AtualizarNfe(int id, Nfes nfe)
    {
        const string sql = @"
            UPDATE nfes
            SET chave_acesso = @ChaveAcesso, numero = @Numero, serie = @Serie,
                data_emissao = @DataEmissao, data_saida = @DataSaida,
                tipo_operacao = @TipoOperacao, status_nfe = @StatusNfe,
                valor_produtos = @ValorProdutos, valor_desconto = @ValorDesconto,
                valor_frete = @ValorFrete, valor_seguro = @ValorSeguro,
                valor_outras_despesas = @ValorOutrasDespesas, valor_total = @ValorTotal,
                emitente_id = @EmitenteId, cliente_id = @ClienteId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(sql,
            new
            {
                Id = id,
                nfe.ChaveAcesso,
                nfe.Numero,
                nfe.Serie,
                nfe.DataEmissao,
                nfe.DataSaida,
                nfe.TipoOperacao,
                nfe.StatusNfe,
                nfe.ValorProdutos,
                nfe.ValorDesconto,
                nfe.ValorFrete,
                nfe.ValorSeguro,
                nfe.ValorOutrasDespesas,
                nfe.ValorTotal,
                EmitenteId = nfe.Emitente.Id,
                ClienteId = nfe.Cliente.Id
            },
            transaction: _session.Transaction);

        await ReplacerItens(id, nfe.NfesItens);
        await ReplacerPagamentos(id, nfe.NfesPagamentos);
        await ReplacerInformacoesAdicionais(id, nfe.NfesInformacoesAdicionais);

        nfe.Id = id;
        return nfe;
    }

    public async Task<bool> DeletarNfe(int id)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM nfes_itens WHERE nfe_id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        await _session.Connection.ExecuteAsync(
            "DELETE FROM nfes_pagamentos WHERE nfe_id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        await _session.Connection.ExecuteAsync(
            "DELETE FROM nfes_informacoes_adicionais WHERE nfe_id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM nfes WHERE id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<NfesResumo>> ObterNfesResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM nfes;

            SELECT id, numero, serie, data_emissao, valor_total, status_nfe
            FROM nfes
            ORDER BY data_emissao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<NfesResumo>();

        return new ResultadoPaginado<NfesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<NfesResumo>> PesquisarNfes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM nfes
            WHERE chave_acesso ILIKE @Termo
               OR cliente_nome_razaosocial ILIKE @Termo
               OR emitente_nome_razaosocial ILIKE @Termo
               OR numero::text ILIKE @Termo;

            SELECT id, numero, serie, data_emissao, valor_total, status_nfe
            FROM nfes
            WHERE chave_acesso ILIKE @Termo
               OR cliente_nome_razaosocial ILIKE @Termo
               OR emitente_nome_razaosocial ILIKE @Termo
               OR numero::text ILIKE @Termo
            ORDER BY data_emissao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<NfesResumo>();

        return new ResultadoPaginado<NfesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirItens(int nfeId, IEnumerable<NfesItens> itens)
    {
        const string sql = @"
            INSERT INTO nfes_itens (numero_item, descricao_item, quantidade, valor_unitario,
                                    valor_desconto, valor_total, sku, unidade_medida_id, nfe_id)
            VALUES (@NumeroItem, @DescricaoItem, @Quantidade, @ValorUnitario,
                    @ValorDesconto, @ValorTotal, @SkuCodigo, @UnidadeMedidaId, @NfeId);";

        await _session.Connection.ExecuteAsync(
            sql,
            itens.Select(i => new
            {
                i.NumeroItem,
                i.DescricaoItem,
                i.Quantidade,
                i.ValorUnitario,
                i.ValorDesconto,
                i.ValorTotal,
                SkuCodigo = i.Sku.Sku,
                UnidadeMedidaId = i.UnidadeMedida.Id,
                NfeId = nfeId
            }),
            transaction: _session.Transaction);
    }

    private async Task InserirPagamentos(int nfeId, IEnumerable<NfesPagamentos> pagamentos)
    {
        const string sql = @"
            INSERT INTO nfes_pagamentos (indicador_pagamento, valor_pagamento, metodo_pagamento_id, nfe_id)
            VALUES (@IndicadorPagamento, @ValorPagamento, @MetodoPagamentoId, @NfeId);";

        await _session.Connection.ExecuteAsync(
            sql,
            pagamentos.Select(p => new
            {
                p.IndicadorPagamento,
                p.ValorPagamento,
                MetodoPagamentoId = p.MetodosPagamento.Id,
                NfeId = nfeId
            }),
            transaction: _session.Transaction);
    }

    private async Task InserirInformacoesAdicionais(int nfeId, NfesInformacoesAdicionais info)
    {
        const string sql = @"
            INSERT INTO nfes_informacoes_adicionais (informacoes_fisco, informacoes_complementares, nfe_id)
            VALUES (@InformacoesFisco, @InformacoesComplementares, @NfeId);";

        await _session.Connection.ExecuteAsync(
            sql,
            new { info.InformacoesFisco, info.InformacoesComplementares, NfeId = nfeId },
            transaction: _session.Transaction);
    }

    private async Task ReplacerItens(int nfeId, IEnumerable<NfesItens> itens)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM nfes_itens WHERE nfe_id = @NfeId;",
            new { NfeId = nfeId }, transaction: _session.Transaction);

        await InserirItens(nfeId, itens);
    }

    private async Task ReplacerPagamentos(int nfeId, IEnumerable<NfesPagamentos> pagamentos)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM nfes_pagamentos WHERE nfe_id = @NfeId;",
            new { NfeId = nfeId }, transaction: _session.Transaction);

        await InserirPagamentos(nfeId, pagamentos);
    }

    private async Task ReplacerInformacoesAdicionais(int nfeId, NfesInformacoesAdicionais? info)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM nfes_informacoes_adicionais WHERE nfe_id = @NfeId;",
            new { NfeId = nfeId }, transaction: _session.Transaction);

        if (info is not null)
            await InserirInformacoesAdicionais(nfeId, info);
    }
}
