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
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Core.Features.Financeiro.Services;

public sealed class ContasReceberService : BaseService
{
    private readonly IContasReceberRepository _contasRepository;
    private readonly IClientesRepository _clientesRepository;
    private readonly ICondicoesPagamentosRepository _condicoesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ContasReceberService(
        IContasReceberRepository contasRepository,
        IClientesRepository clientesRepository,
        ICondicoesPagamentosRepository condicoesRepository,
        IUnitOfWork unitOfWork)
    {
        _contasRepository = contasRepository;
        _clientesRepository = clientesRepository;
        _condicoesRepository = condicoesRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ContasReceber?> ObterContaReceberPorId(int id)
        => _contasRepository.ObterContaReceberPorId(id);

    public Task<ResultadoPaginado<ContasReceber>> PesquisarContasReceber(string termo, int pagina = 1, int tamanhoDaPagina = 20, FiltroContasReceber? filtro = null)
        => _contasRepository.PesquisarContasReceber(termo, pagina, tamanhoDaPagina, filtro);

    public async Task<Resultado<IReadOnlyList<ContasReceber>>> CriarContaReceber(CriarContaReceberCommand command)
    {
        var validator = new CriarContaReceberCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<IReadOnlyList<ContasReceber>>.Falha(validation.ToResultadoErros());

        var cliente = await _clientesRepository.ObterClientePorId(command.ClienteId);
        if (cliente is null)
            return Resultado<IReadOnlyList<ContasReceber>>.Falha(new ResultadoErro("CLIENTE_INEXISTENTE", "O cliente informado não existe.", "ClienteId"));

        CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<IReadOnlyList<ContasReceber>>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        // ponytail: número manual por timestamp (ms); colisão só com dois lançamentos no mesmo milissegundo.
        var documento = string.IsNullOrWhiteSpace(command.NumeroDocumento)
            ? $"MAN-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
            : command.NumeroDocumento.Trim();
        var parcelas = command.Parcelas.ToList();

        return await ExecuteResultAsync<IReadOnlyList<ContasReceber>>(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var criadas = new List<ContasReceber>();
                for (var i = 0; i < parcelas.Count; i++)
                {
                    var conta = new ContasReceber(command.Descricao, parcelas[i].Valor, cliente, parcelas[i].DataVencimento, documento,
                        i + 1, parcelas.Count, command.DataEmissao, condicao, observacao: command.Observacao);
                    criadas.Add(await _contasRepository.CriarContaReceber(conta));
                }

                _unitOfWork.Commit();
                return Resultado<IReadOnlyList<ContasReceber>>.Sucesso(criadas);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<ContasReceber>> AtualizarContaReceber(int id, AtualizarContaReceberCommand command)
    {
        var validator = new AtualizarContaReceberCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<ContasReceber>.Falha(validation.ToResultadoErros());

        return await Persistir(id, c => c.Atualizar(command.Descricao, command.DataVencimento, command.Valor, command.Observacao),
            existente => existente.OrigemTipo != OrigemTituloFinanceiro.MANUAL
                ? new ResultadoErro("CONTA_RECEBER_EXTERNA", $"Conta gerada por {existente.OrigemTipo.Nome()} #{existente.OrigemId} não pode ser editada manualmente.")
                : null);
    }

    public async Task<Resultado<ContasReceber>> RegistrarRecebimento(int id, ReceberContaReceberCommand command)
    {
        if (command.Valor <= 0)
            return Resultado<ContasReceber>.Falha(new ResultadoErro("VALOR_INVALIDO", "O valor de recebimento deve ser maior que zero.", "Valor"));

        return await Persistir(id, c => c.RegistrarRecebimento(command.Valor));
    }

    public async Task<Resultado<ContasReceber>> EstornarRecebimento(int id, EstornarContaReceberCommand command)
    {
        if (command.Valor <= 0)
            return Resultado<ContasReceber>.Falha(new ResultadoErro("VALOR_INVALIDO", "O valor de estorno deve ser maior que zero.", "Valor"));

        return await Persistir(id, c => c.EstornarRecebimento(command.Valor));
    }

    // Lê com FOR UPDATE e deleta na mesma transação, como Persistir.
    public async Task<bool> DeletarContaReceber(int id)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            var existente = await _contasRepository.ObterContaReceberPorIdParaAtualizar(id);
            if (existente is null)
            {
                _unitOfWork.Rollback();
                return false;
            }

            if (existente.OrigemTipo != OrigemTituloFinanceiro.MANUAL)
                throw new Common.Exceptions.DomainException($"Conta gerada por {existente.OrigemTipo.Nome()} #{existente.OrigemId}. Para cancelá-la, cancele o documento de origem.");

            if (existente.PossuiBaixa || existente.Status != StatusTituloFinanceiro.ABERTO)
                throw new Common.Exceptions.DomainException("Não é possível excluir uma conta a receber que já possui recebimento ou não está aberta.");

            var deletada = await _contasRepository.DeletarContaReceber(id);
            _unitOfWork.Commit();
            return deletada;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // Lê com FOR UPDATE dentro da transação: recebimentos concorrentes da mesma conta se enfileiram e cada um parte do valor_pago já gravado.
    private Task<Resultado<ContasReceber>> Persistir(int id, Action<ContasReceber> alterar, Func<ContasReceber, ResultadoErro?>? validar = null)
        => ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var conta = await _contasRepository.ObterContaReceberPorIdParaAtualizar(id);
                if (conta is null)
                {
                    _unitOfWork.Rollback();
                    return Resultado<ContasReceber>.Falha(new ResultadoErro("CONTA_RECEBER_NAO_ENCONTRADA", "Conta a receber não encontrada."));
                }

                var erro = validar?.Invoke(conta);
                if (erro is not null)
                {
                    _unitOfWork.Rollback();
                    return Resultado<ContasReceber>.Falha(erro);
                }

                alterar(conta);
                var atualizada = await _contasRepository.AtualizarContaReceber(id, conta);
                _unitOfWork.Commit();

                return Resultado<ContasReceber>.Sucesso(atualizada);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
}
