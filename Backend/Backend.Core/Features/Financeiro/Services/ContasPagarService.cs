using Backend.Core.Common;
using Backend.Core.Common.Results;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Financeiro.Validators.Commands;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.NFe.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Core.Features.Financeiro.Services;

public sealed class ContasPagarService : BaseService
{
    private readonly IContasPagarRepository _contasRepository;
    private readonly IFornecedoresRepository _fornecedoresRepository;
    private readonly ICondicoesPagamentosRepository _condicoesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ContasPagarService(
        IContasPagarRepository contasRepository,
        IFornecedoresRepository fornecedoresRepository,
        ICondicoesPagamentosRepository condicoesRepository,
        IUnitOfWork unitOfWork)
    {
        _contasRepository = contasRepository;
        _fornecedoresRepository = fornecedoresRepository;
        _condicoesRepository = condicoesRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<ContasPagar>> ObterContasPagar(int pagina = 1, int tamanhoDaPagina = 20)
        => _contasRepository.ObterContasPagar(pagina, tamanhoDaPagina);

    public Task<ContasPagar?> ObterContaPagarPorId(int id)
        => _contasRepository.ObterContaPagarPorId(id);

    public Task<ResultadoPaginado<ContasPagar>> PesquisarContasPagar(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _contasRepository.PesquisarContasPagar(termo, pagina, tamanhoDaPagina);

    public async Task<Resultado<ContasPagar>> CriarContaPagar(CriarContaPagarCommand command)
    {
        var validator = new CriarContaPagarCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<ContasPagar>.Falha(validation.ToResultadoErros());

        var fornecedor = await _fornecedoresRepository.ObterFornecedorPorId(command.FornecedorId);
        if (fornecedor is null)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("FORNECEDOR_INEXISTENTE", "O fornecedor informado não existe.", "FornecedorId"));

        CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<ContasPagar>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var conta = new ContasPagar(
                    command.Descricao,
                    command.ValorOriginal,
                    fornecedor,
                    command.DataEmissao,
                    null,
                    condicao,
                    observacao: command.Observacao
                );

                foreach (var p in command.Parcelas)
                {
                    conta.AdicionarParcela(p.NumeroParcela, p.DataVencimento, p.ValorParcela);
                }

                var criada = await _contasRepository.CriarContaPagar(conta);
                _unitOfWork.Commit();

                return Resultado<ContasPagar>.Sucesso(criada);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<ContasPagar>> AtualizarContaPagar(int id, AtualizarContaPagarCommand command)
    {
        var validator = new AtualizarContaPagarCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<ContasPagar>.Falha(validation.ToResultadoErros());

        var existente = await _contasRepository.ObterContaPagarPorId(id);
        if (existente is null)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("CONTA_PAGAR_NAO_ENCONTRADA", "Conta a pagar não encontrada."));

        if (existente.OrigemTipo != OrigemTituloFinanceiro.MANUAL)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("CONTA_PAGAR_EXTERNA", $"Conta gerada por {existente.OrigemTipo.Nome()} #{existente.OrigemId} não pode ser editada manualmente."));

        var fornecedor = await _fornecedoresRepository.ObterFornecedorPorId(command.FornecedorId);
        if (fornecedor is null)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("FORNECEDOR_INEXISTENTE", "O fornecedor informado não existe.", "FornecedorId"));

        CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<ContasPagar>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var parcelas = command.Parcelas.Select(p =>
                    new ContasPagarParcelas(0, id, p.NumeroParcela, p.DataVencimento, p.ValorParcela, p.ValorPago, p.Status));

                existente.Atualizar(
                    command.Descricao,
                    command.ValorOriginal,
                    fornecedor,
                    parcelas,
                    command.DataEmissao,
                    null,
                    condicao,
                    command.Observacao
                );

                var atualizada = await _contasRepository.AtualizarContaPagar(id, existente);
                _unitOfWork.Commit();

                return Resultado<ContasPagar>.Sucesso(atualizada);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<ContasPagar>> RegistrarPagamento(int id, RegistrarPagamentoParcelaCommand command)
    {
        if (command.ValorPago <= 0)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("VALOR_INVALIDO", "O valor de pagamento deve ser maior que zero.", "ValorPago"));

        var existente = await _contasRepository.ObterContaPagarPorId(id);
        if (existente is null)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("CONTA_PAGAR_NAO_ENCONTRADA", "Conta a pagar não encontrada."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                existente.RegistrarPagamento(command.NumeroParcela, command.ValorPago);

                var atualizada = await _contasRepository.AtualizarContaPagar(id, existente);
                _unitOfWork.Commit();

                return Resultado<ContasPagar>.Sucesso(atualizada);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<ContasPagar>> EstornarPagamento(int id, EstornarPagamentoParcelaCommand command)
    {
        if (command.ValorEstorno <= 0)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("VALOR_INVALIDO", "O valor de estorno deve ser maior que zero.", "ValorEstorno"));

        var existente = await _contasRepository.ObterContaPagarPorId(id);
        if (existente is null)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("CONTA_PAGAR_NAO_ENCONTRADA", "Conta a pagar não encontrada."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                existente.EstornarPagamento(command.NumeroParcela, command.ValorEstorno);

                var atualizada = await _contasRepository.AtualizarContaPagar(id, existente);
                _unitOfWork.Commit();

                return Resultado<ContasPagar>.Sucesso(atualizada);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<bool> DeletarContaPagar(int id)
    {
        var existente = await _contasRepository.ObterContaPagarPorId(id);
        if (existente is null) return false;

        if (existente.OrigemTipo != OrigemTituloFinanceiro.MANUAL)
            throw new Common.Exceptions.DomainException($"Conta gerada por {existente.OrigemTipo.Nome()} #{existente.OrigemId}. Para cancelá-la, cancele o documento de origem.");

        if (existente.ValorSaldo < existente.ValorOriginal || existente.ContasPagarParcelas.Any(p => p.Status != StatusTituloFinanceiro.ABERTO))
            throw new Common.Exceptions.DomainException("Não é possível excluir uma conta a pagar que já possui parcelas pagas ou alteradas.");

        return await _contasRepository.DeletarContaPagar(id);
    }
}

