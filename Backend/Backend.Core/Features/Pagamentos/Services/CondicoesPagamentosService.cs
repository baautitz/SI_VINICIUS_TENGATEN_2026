using Backend.Core.Common;
using Backend.Core.Common.Results;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Pagamentos.Commands;
using Backend.Core.Features.Pagamentos.Entities;
using Backend.Core.Features.Pagamentos.Repositories;
using Backend.Core.Features.Pagamentos.Validators.Commands;
using System.Linq;

namespace Backend.Core.Features.Pagamentos.Services;

public sealed class CondicoesPagamentosService : BaseService
{
    private readonly ICondicoesPagamentosRepository _condicoesRepository;
    private readonly IMetodosPagamentosRepository _metodosRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CondicoesPagamentosService(
        ICondicoesPagamentosRepository condicoesRepository,
        IMetodosPagamentosRepository metodosRepository,
        IUnitOfWork unitOfWork)
    {
        _condicoesRepository = condicoesRepository;
        _metodosRepository = metodosRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<CondicoesPagamentos>> ObterCondicoesPagamentos(int pagina = 1, int tamanhoDaPagina = 20)
        => _condicoesRepository.ObterCondicoesPagamentos(pagina, tamanhoDaPagina);

    public Task<CondicoesPagamentos?> ObterCondicaoPagamentoPorId(int id)
        => _condicoesRepository.ObterCondicaoPagamentoPorId(id);

    public Task<ResultadoPaginado<CondicoesPagamentos>> PesquisarCondicoesPagamentos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _condicoesRepository.PesquisarCondicoesPagamentos(termo, pagina, tamanhoDaPagina);

    public async Task<Resultado<CondicoesPagamentos>> CriarCondicaoPagamento(CriarCondicaoPagamentoCommand command)
    {
        var validator = new CriarCondicaoPagamentoCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<CondicoesPagamentos>.Falha(validation.ToResultadoErros());

        var metodo = await _metodosRepository.ObterMetodoPagamentoPorCodigo(command.MetodoPagamentoCodigo);
        if (metodo is null)
            return Resultado<CondicoesPagamentos>.Falha(new ResultadoErro("METODO_PAGAMENTO_INEXISTENTE", "O método de pagamento informado não existe.", "MetodoPagamentoCodigo"));

        if (await _condicoesRepository.ExisteDescricao(command.Descricao))
            return Resultado<CondicoesPagamentos>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe uma condição de pagamento com esta descrição.", "Descricao"));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var parcelas = command.Parcelas.Select(p => new CondicoesPagamentosParcelas(p.NumeroParcela, p.Percentual, p.PrazoDias));
                var condicao = new CondicoesPagamentos(
                    command.Descricao,
                    metodo,
                    parcelas,
                    command.EntradaMinimaPercentual,
                    command.DescontoPercentual,
                    command.AcrescimoPercentual,
                    command.MultaPercentual,
                    command.TaxaJurosPercentual,
                    command.Ativo
                );

                var criada = await _condicoesRepository.CriarCondicaoPagamento(condicao);
                _unitOfWork.Commit();

                return Resultado<CondicoesPagamentos>.Sucesso(criada);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<CondicoesPagamentos>> AtualizarCondicaoPagamento(int id, AtualizarCondicaoPagamentoCommand command)
    {
        var validator = new AtualizarCondicaoPagamentoCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<CondicoesPagamentos>.Falha(validation.ToResultadoErros());

        var existente = await _condicoesRepository.ObterCondicaoPagamentoPorId(id);
        if (existente is null)
            return Resultado<CondicoesPagamentos>.Falha(new ResultadoErro("CONDICAO_NAO_ENCONTRADA", "Condição de pagamento não encontrada."));

        var metodo = await _metodosRepository.ObterMetodoPagamentoPorCodigo(command.MetodoPagamentoCodigo);
        if (metodo is null)
            return Resultado<CondicoesPagamentos>.Falha(new ResultadoErro("METODO_PAGAMENTO_INEXISTENTE", "O método de pagamento informado não existe.", "MetodoPagamentoCodigo"));

        if (await _condicoesRepository.ExisteDescricao(command.Descricao, id))
            return Resultado<CondicoesPagamentos>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outra condição de pagamento com esta descrição.", "Descricao"));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var novasParcelas = command.Parcelas.Select(p => new CondicoesPagamentosParcelas(p.NumeroParcela, p.Percentual, p.PrazoDias)).ToList();
                existente.Atualizar(
                    command.Descricao,
                    metodo,
                    command.EntradaMinimaPercentual,
                    command.DescontoPercentual,
                    command.AcrescimoPercentual,
                    command.MultaPercentual,
                    command.TaxaJurosPercentual,
                    novasParcelas
                );

                if (command.Ativo) existente.Ativar();
                else existente.Desativar();

                var atualizada = await _condicoesRepository.AtualizarCondicaoPagamento(id, existente);
                _unitOfWork.Commit();

                return Resultado<CondicoesPagamentos>.Sucesso(atualizada);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public Task<bool> DeletarCondicaoPagamento(int id)
        => _condicoesRepository.DeletarCondicaoPagamento(id);
}
