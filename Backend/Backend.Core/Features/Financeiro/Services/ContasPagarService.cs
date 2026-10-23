using Backend.Core.Common;
using Backend.Core.Common.Results;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.DTOs;
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

    public Task<ContasPagar?> ObterContaPagarPorId(int id)
        => _contasRepository.ObterContaPagarPorId(id);

    public Task<ResultadoPaginado<ContasPagar>> PesquisarContasPagar(string termo, int pagina = 1, int tamanhoDaPagina = 20, FiltroContasPagar? filtro = null)
        => _contasRepository.PesquisarContasPagar(termo, pagina, tamanhoDaPagina, filtro);

    public async Task<Resultado<IReadOnlyList<ContasPagar>>> CriarContaPagar(CriarContaPagarCommand command)
    {
        var validator = new CriarContaPagarCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<IReadOnlyList<ContasPagar>>.Falha(validation.ToResultadoErros());

        var fornecedor = await _fornecedoresRepository.ObterFornecedorPorId(command.FornecedorId);
        if (fornecedor is null)
            return Resultado<IReadOnlyList<ContasPagar>>.Falha(new ResultadoErro("FORNECEDOR_INEXISTENTE", "O fornecedor informado não existe.", "FornecedorId"));

        CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<IReadOnlyList<ContasPagar>>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        // ponytail: número manual por timestamp (ms); colisão só com dois lançamentos no mesmo milissegundo.
        var documento = string.IsNullOrWhiteSpace(command.NumeroDocumento)
            ? $"MAN-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
            : command.NumeroDocumento.Trim();
        var parcelas = command.Parcelas.ToList();

        return await ExecuteResultAsync<IReadOnlyList<ContasPagar>>(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var criadas = new List<ContasPagar>();
                for (var i = 0; i < parcelas.Count; i++)
                {
                    var conta = new ContasPagar(command.Descricao, parcelas[i].Valor, fornecedor, parcelas[i].DataVencimento, documento,
                        i + 1, parcelas.Count, command.DataEmissao, condicao, observacao: command.Observacao);
                    criadas.Add(await _contasRepository.CriarContaPagar(conta));
                }

                _unitOfWork.Commit();
                return Resultado<IReadOnlyList<ContasPagar>>.Sucesso(criadas);
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

        return await Persistir(id, c => c.Atualizar(command.Descricao, command.DataVencimento, command.Valor, command.Observacao),
            existente => existente.OrigemTipo != OrigemTituloFinanceiro.MANUAL
                ? new ResultadoErro("CONTA_PAGAR_EXTERNA", $"Conta gerada por {existente.OrigemTipo.Nome()} #{existente.OrigemId} não pode ser editada manualmente.")
                : null);
    }

    public async Task<Resultado<ContasPagar>> RegistrarPagamento(int id, PagarContaPagarCommand command)
    {
        if (command.Valor <= 0)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("VALOR_INVALIDO", "O valor de pagamento deve ser maior que zero.", "Valor"));

        return await Persistir(id, c => c.RegistrarPagamento(command.Valor));
    }

    public async Task<Resultado<ContasPagar>> EstornarPagamento(int id, EstornarContaPagarCommand command)
    {
        if (command.Valor <= 0)
            return Resultado<ContasPagar>.Falha(new ResultadoErro("VALOR_INVALIDO", "O valor de estorno deve ser maior que zero.", "Valor"));

        return await Persistir(id, c => c.EstornarPagamento(command.Valor));
    }

    // Lê com FOR UPDATE e deleta na mesma transação, como Persistir.
    public async Task<bool> DeletarContaPagar(int id)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            var existente = await _contasRepository.ObterContaPagarPorIdParaAtualizar(id);
            if (existente is null)
            {
                _unitOfWork.Rollback();
                return false;
            }

            if (existente.OrigemTipo != OrigemTituloFinanceiro.MANUAL)
                throw new Common.Exceptions.DomainException($"Conta gerada por {existente.OrigemTipo.Nome()} #{existente.OrigemId}. Para cancelá-la, cancele o documento de origem.");

            if (existente.ValorPago > 0 || existente.Status != StatusTituloFinanceiro.ABERTO)
                throw new Common.Exceptions.DomainException("Não é possível excluir uma conta a pagar que já possui pagamento ou não está aberta.");

            var deletada = await _contasRepository.DeletarContaPagar(id);
            _unitOfWork.Commit();
            return deletada;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // Lê com FOR UPDATE dentro da transação: pagamentos concorrentes da mesma conta se enfileiram e cada um parte do valor_pago já gravado.
    private Task<Resultado<ContasPagar>> Persistir(int id, Action<ContasPagar> alterar, Func<ContasPagar, ResultadoErro?>? validar = null)
        => ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var conta = await _contasRepository.ObterContaPagarPorIdParaAtualizar(id);
                if (conta is null)
                {
                    _unitOfWork.Rollback();
                    return Resultado<ContasPagar>.Falha(new ResultadoErro("CONTA_PAGAR_NAO_ENCONTRADA", "Conta a pagar não encontrada."));
                }

                var erro = validar?.Invoke(conta);
                if (erro is not null)
                {
                    _unitOfWork.Rollback();
                    return Resultado<ContasPagar>.Falha(erro);
                }

                alterar(conta);
                var atualizada = await _contasRepository.AtualizarContaPagar(id, conta);
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
