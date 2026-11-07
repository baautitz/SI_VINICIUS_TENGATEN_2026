using System.Security.Cryptography;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Vendas.Entities;

namespace Backend.Core.Features.NFe.Services;

// NF-e emitida pela loja ao registrar (ou estornar) uma Devolução de venda ou de compra. Vale a regra do ADR 0004:
// só emite se houver NF-e original ativa, nunca cancela a original e a nota de devolução só é desfeita por uma nota de estorno.
// Todos os métodos exigem a transação da Devolução aberta (a numeração usa advisory lock) e devolvem null quando não há nota a emitir.
public sealed class NfesDevolucaoService
{
    private readonly INfesRepository _nfesRepository;
    private readonly IEmitentesRepository _emitentesRepository;
    private readonly IClientesRepository _clientesRepository;
    private readonly IFornecedoresRepository _fornecedoresRepository;

    public NfesDevolucaoService(INfesRepository nfesRepository, IEmitentesRepository emitentesRepository,
        IClientesRepository clientesRepository, IFornecedoresRepository fornecedoresRepository)
    {
        _nfesRepository = nfesRepository;
        _emitentesRepository = emitentesRepository;
        _clientesRepository = clientesRepository;
        _fornecedoresRepository = fornecedoresRepository;
    }

    // Devolução de venda: nota de entrada própria, referenciando a NF-e de saída da venda.
    public async Task<NfeAtiva?> EmitirDevolucaoDeVenda(Venda venda, Devolucao devolucao)
    {
        var original = await _nfesRepository.ObterNfeEmitidaDaVenda(venda.Id);
        if (original is null) return null;
        return await Emitir(FinalidadeNfe.DEVOLUCAO, TipoOperacao.ENTRADA, original, venda.Emitente.Id, venda.Cliente.Id, null,
            ItensDaVenda(venda, devolucao), devolucao.Id, null);
    }

    // Estorno da devolução de venda: nota de saída referenciando a NF-e de devolução.
    public async Task<NfeAtiva?> EmitirEstornoDeDevolucaoDeVenda(Venda venda, Devolucao devolucao)
    {
        var devolvida = await _nfesRepository.ObterNfeDaDevolucao(devolucao.Id, null, FinalidadeNfe.DEVOLUCAO);
        if (devolvida is null) return null;
        return await Emitir(FinalidadeNfe.ESTORNO_DEVOLUCAO, TipoOperacao.SAIDA, devolvida, venda.Emitente.Id, venda.Cliente.Id, null,
            ItensDaVenda(venda, devolucao), devolucao.Id, null);
    }

    // Devolução de compra: nota de saída própria, referenciando a NF-e de entrada da compra.
    public async Task<NfeAtiva?> EmitirDevolucaoDeCompra(Compra compra, DevolucaoCompra devolucao)
    {
        var original = await _nfesRepository.ObterNfeEmitidaDaCompra(compra.Id);
        if (original is null) return null;
        return await Emitir(FinalidadeNfe.DEVOLUCAO, TipoOperacao.SAIDA, original, compra.Emitente.Id, null, compra.Fornecedor.Id,
            ItensDaCompra(compra, devolucao), null, devolucao.Id);
    }

    // Estorno da devolução de compra: nota de entrada referenciando a NF-e de devolução.
    public async Task<NfeAtiva?> EmitirEstornoDeDevolucaoDeCompra(Compra compra, DevolucaoCompra devolucao)
    {
        var devolvida = await _nfesRepository.ObterNfeDaDevolucao(null, devolucao.Id, FinalidadeNfe.DEVOLUCAO);
        if (devolvida is null) return null;
        return await Emitir(FinalidadeNfe.ESTORNO_DEVOLUCAO, TipoOperacao.ENTRADA, devolvida, compra.Emitente.Id, null, compra.Fornecedor.Id,
            ItensDaCompra(compra, devolucao), null, devolucao.Id);
    }

    private static List<NfesItens> ItensDaVenda(Venda venda, Devolucao devolucao)
        => devolucao.Itens.Select((d, n) =>
        {
            var sku = venda.Itens.First(i => i.Id == d.VendaItemId).Sku;
            return new NfesItens(n + 1, TextNormalization.Truncate(sku.NomeExibicao, 150)!, d.Quantidade, d.ValorUnitario, 0m, sku, sku.Produto!.UnidadeMedida);
        }).ToList();

    private static List<NfesItens> ItensDaCompra(Compra compra, DevolucaoCompra devolucao)
        => devolucao.Itens.Select((d, n) =>
        {
            var sku = compra.Itens.First(i => i.Id == d.CompraItemId).Sku;
            return new NfesItens(n + 1, TextNormalization.Truncate(sku.NomeExibicao, 150)!, d.Quantidade, d.ValorUnitario, 0m, sku, sku.Produto!.UnidadeMedida);
        }).ToList();

    private async Task<NfeAtiva> Emitir(FinalidadeNfe finalidade, TipoOperacao tipo, NfeAtiva referenciada, int emitenteId,
        int? clienteId, int? fornecedorId, List<NfesItens> itens, int? devolucaoId, int? devolucaoCompraId)
    {
        // O emitente do documento carrega a localização completa, que a venda/compra não traz.
        var emitente = await _emitentesRepository.ObterEmitentePorId(emitenteId);
        var uf = emitente?.Bairro?.Cidade?.Estado?.Uf;
        if (emitente is null || string.IsNullOrWhiteSpace(uf))
            throw new DomainException("O emitente precisa de bairro/cidade/estado cadastrados para emitir a NF-e de devolução.");

        var cliente = clienteId is { } c
            ? await _clientesRepository.ObterClientePorId(c) ?? throw new DomainException("Cliente da NF-e de devolução não encontrado.")
            : null;
        var fornecedor = fornecedorId is { } f
            ? await _fornecedoresRepository.ObterFornecedorPorId(f) ?? throw new DomainException("Fornecedor da NF-e de devolução não encontrado.")
            : null;

        var numero = await _nfesRepository.ProximoNumeroSaida(emitente.Id, NfesSaidaService.SeriePadrao);
        var emissao = DateTimeOffset.UtcNow;
        var chave = NfeChaveAcesso.Gerar(uf, emissao.ToOffset(NfeChaveAcesso.FusoBrasilia).DateTime, emitente.CpfCnpj.Valor,
            NfesSaidaService.SeriePadrao, numero, RandomNumberGenerator.GetInt32(1, 100_000_000));

        var nfe = Nfes.CriarDevolucao(finalidade, tipo, numero, NfesSaidaService.SeriePadrao, emissao.UtcDateTime, chave,
            emitente, cliente, fornecedor, itens, referenciada.NfeId, referenciada.ChaveAcesso, devolucaoId, devolucaoCompraId);
        await _nfesRepository.CriarNfe(nfe);
        return new NfeAtiva(nfe.Id, chave);
    }
}
