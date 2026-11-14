using System.Security.Cryptography;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Parceiros.Repositories;

namespace Backend.Core.Features.NFe.Services;

// NF-e emitida pela loja, para si mesma, ao dar baixa no estoque por perda, furto ou vencimento (ADR 0006).
// A nota de baixa é saída (CFOP 5.927) e o estorno da movimentação emite a nota de estorno da baixa, entrada (CFOP 1.949)
// referenciando a nota de baixa. Os dois métodos exigem a transação da Movimentação aberta (a numeração usa advisory lock).
public sealed class NfesBaixaEstoqueService
{
    private readonly INfesRepository _nfesRepository;
    private readonly IEmitentesRepository _emitentesRepository;

    public NfesBaixaEstoqueService(INfesRepository nfesRepository, IEmitentesRepository emitentesRepository)
    {
        _nfesRepository = nfesRepository;
        _emitentesRepository = emitentesRepository;
    }

    // Chamar depois de registrar a movimentação: os itens da nota levam o custo médio com que a saída foi valorizada.
    public Task<NfeAtiva> EmitirBaixa(MovimentacoesEstoques movimentacao, int emitenteId)
        => Emitir(FinalidadeNfe.BAIXA_ESTOQUE, movimentacao, emitenteId, null);

    // Só emite se a movimentação original teve NF-e de baixa; devolve null quando não há nota a estornar.
    public async Task<NfeAtiva?> EmitirEstornoDaBaixa(MovimentacoesEstoques original)
    {
        var baixa = await _nfesRepository.ObterNfeDaMovimentacao(original.Id, FinalidadeNfe.BAIXA_ESTOQUE);
        if (baixa is null) return null;

        var nota = await _nfesRepository.ObterNfePorId(baixa.NfeId)
            ?? throw new DomainException("NF-e de baixa da movimentação não encontrada.");
        return await Emitir(FinalidadeNfe.ESTORNO_BAIXA_ESTOQUE, original, nota.Emitente.Id, baixa);
    }

    private async Task<NfeAtiva> Emitir(FinalidadeNfe finalidade, MovimentacoesEstoques movimentacao, int emitenteId, NfeAtiva? referenciada)
    {
        // O emitente do documento carrega a localização completa (a chave de acesso usa a UF).
        var emitente = await _emitentesRepository.ObterEmitentePorId(emitenteId)
            ?? throw new DomainException("Emitente da NF-e de baixa não encontrado.");
        var uf = emitente.Bairro?.Cidade?.Estado?.Uf;
        if (string.IsNullOrWhiteSpace(uf))
            throw new DomainException("O emitente precisa de bairro/cidade/estado cadastrados para emitir a NF-e de baixa.");

        var itens = movimentacao.MovimentacoesEstoquesItens
            .Select((i, n) => new NfesItens(n + 1, TextNormalization.Truncate(i.Sku.NomeExibicao, 150)!, i.Quantidade, i.CustoUnitario, 0m, i.Sku, i.Sku.Produto!.UnidadeMedida))
            .ToList();

        var numero = await _nfesRepository.ProximoNumeroSaida(emitente.Id, NfesSaidaService.SeriePadrao);
        var emissao = DateTimeOffset.UtcNow;
        var chave = NfeChaveAcesso.Gerar(uf, emissao.ToOffset(NfeChaveAcesso.FusoBrasilia).DateTime, emitente.CpfCnpj.Valor,
            NfesSaidaService.SeriePadrao, numero, RandomNumberGenerator.GetInt32(1, 100_000_000));

        var nfe = Nfes.CriarBaixaEstoque(finalidade, numero, NfesSaidaService.SeriePadrao, emissao.UtcDateTime, chave, emitente, itens,
            movimentacao.Id, referenciada?.NfeId, referenciada?.ChaveAcesso);
        await _nfesRepository.CriarNfe(nfe);
        return new NfeAtiva(nfe.Id, chave);
    }
}
