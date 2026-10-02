using System.Security.Cryptography;
using Backend.Core.Common;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Vendas.Services;

namespace Backend.Core.Features.NFe.Services;

// Emissão (a partir de uma venda) e cancelamento fiscal de NF-e de saída.
public sealed class NfesSaidaService : BaseService
{
    public const short SeriePadrao = 1;
    public const string VendaJaTemNfe = "VENDA_JA_TEM_NFE";
    public const string NfeEntradaNaoCancelavel = "NFE_ENTRADA_NAO_CANCELAVEL";
    public const string NfeNaoEmitida = "NFE_NAO_EMITIDA";
    public const string NfeJaCancelada = "NFE_JA_CANCELADA";

    private readonly INfesRepository _nfesRepository;
    private readonly IEmitentesRepository _emitentesRepository;
    private readonly VendasService _vendasService;
    private readonly IUnitOfWork _unitOfWork;

    public NfesSaidaService(INfesRepository nfesRepository, IEmitentesRepository emitentesRepository, VendasService vendasService, IUnitOfWork unitOfWork)
    {
        _nfesRepository = nfesRepository;
        _emitentesRepository = emitentesRepository;
        _vendasService = vendasService;
        _unitOfWork = unitOfWork;
    }

    public static bool EhConflito(IEnumerable<ResultadoErro> erros)
        => erros.Any(e => e.Code is VendaJaTemNfe or NfeEntradaNaoCancelavel or NfeNaoEmitida or NfeJaCancelada);

    public async Task<Resultado<Nfes>> EmitirDeVenda(int vendaId)
    {
        var venda = await _vendasService.ObterVendaPorId(vendaId);
        if (venda is null)
            return Falha("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId");

        if (await _nfesRepository.ObterNfeEmitidaDaVenda(vendaId) is { } ativa)
            return Falha(VendaJaTemNfe, $"A venda já possui a NF-e {ativa.NfeId} ativa (chave {ativa.ChaveAcesso}).", "VendaId");

        // A venda não carrega a localização do emitente; a UF vem do cadastro completo.
        var emitente = await _emitentesRepository.ObterEmitentePorId(venda.Emitente.Id);
        var uf = emitente?.Bairro?.Cidade?.Estado?.Uf;
        if (emitente is null || string.IsNullOrWhiteSpace(uf))
            return Falha("EMITENTE_SEM_UF", "O emitente precisa de bairro/cidade/estado cadastrados para emitir NF-e.", "EmitenteId");

        var conta = await _vendasService.ObterContaDaVenda(vendaId);
        if (conta is null || conta.CondicaoPagamento is null || !conta.ContasReceberParcelas.Any())
            return Falha("VENDA_SEM_PAGAMENTO", "A venda não tem condição de pagamento/parcelas para compor os pagamentos da NF-e.", "VendaId");
        var aVista = conta.CondicaoPagamento.EntradaMinimaPercentual >= 100;
        var indicador = aVista ? IndicadorPagamento.A_VISTA : IndicadorPagamento.A_PRAZO;
        var metodo = conta.CondicaoPagamento.MetodoPagamento;
        // Parcelas abatidas por devolução deixam de somar o total da venda: nesse caso um único pagamento do total.
        var pagamentos = conta.ContasReceberParcelas.Sum(p => p.ValorParcela) == venda.ValorTotal
            ? conta.ContasReceberParcelas.Select(p => new NfesPagamentos(indicador, p.ValorParcela, metodo)).ToList()
            : new List<NfesPagamentos> { new(indicador, venda.ValorTotal, metodo) };

        var itens = venda.Itens.Select((i, n) => new NfesItens(
            n + 1, TextNormalization.Truncate(i.Sku.NomeExibicao, 150)!, i.Quantidade, i.ValorUnitario, i.ValorDesconto, i.Sku, i.Sku.Produto!.UnidadeMedida)).ToList();

        try
        {
            _unitOfWork.BeginTransaction();

            var numero = await _nfesRepository.ProximoNumeroSaida(emitente.Id, SeriePadrao);
            var emissao = DateTimeOffset.UtcNow;
            var chave = NfeChaveAcesso.Gerar(uf, emissao.ToOffset(NfeChaveAcesso.FusoBrasilia).DateTime, emitente.CpfCnpj.Valor, SeriePadrao, numero, RandomNumberGenerator.GetInt32(1, 100_000_000));

            var nfe = new Nfes(numero, SeriePadrao, emissao.UtcDateTime, emitente, venda.Cliente, TipoOperacao.SAIDA, itens, pagamentos,
                chave, valorDesconto: itens.Sum(i => i.ValorDesconto), vendaId: vendaId);
            nfe.Emitir(NfeXmlGerador.Gerar(nfe));

            nfe = await _nfesRepository.CriarNfe(nfe);
            _unitOfWork.Commit();
            return Resultado<Nfes>.Sucesso(nfe);
        }
        catch (UniqueConstraintException)
        {
            // Corrida: outra emissão para a mesma venda (ou número) venceu.
            _unitOfWork.Rollback();
            return Falha(VendaJaTemNfe, "A venda já possui NF-e ativa.", "VendaId");
        }
        catch (Exception)
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<Resultado<Nfes>> Cancelar(int id, CancelarNfeCommand command)
    {
        var nfe = await _nfesRepository.ObterNfePorId(id);
        if (nfe is null)
            return Falha("NFE_INEXISTENTE", "A NF-e informada não existe.", "Id");
        if (nfe.TipoOperacao != TipoOperacao.SAIDA)
            return Falha(NfeEntradaNaoCancelavel, "NF-e de entrada não é cancelada diretamente: estorne a compra.", "Id");
        if (nfe.StatusNfe == StatusNfe.CANCELADA)
            return Falha(NfeJaCancelada, "A NF-e já está cancelada.", "Id");
        if (nfe.StatusNfe != StatusNfe.EMITIDA)
            return Falha(NfeNaoEmitida, "Somente NF-e emitida pode ser cancelada.", "Id");

        try { nfe.Cancelar(command.Justificativa); }
        catch (DomainException ex) { return Falha("JUSTIFICATIVA_INVALIDA", ex.Message, "Justificativa"); }

        if (!await _nfesRepository.CancelarNfe(nfe))
            return Falha(NfeJaCancelada, "A NF-e já foi cancelada ou deixou de estar emitida.", "Id");
        return Resultado<Nfes>.Sucesso(nfe);
    }

    private static Resultado<Nfes> Falha(string code, string message, string? field = null)
        => Resultado<Nfes>.Falha(new ResultadoErro(code, message, field));
}
