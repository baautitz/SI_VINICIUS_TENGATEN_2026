using Backend.Core.Common;
using Backend.Core.Common.Results;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Financeiro.Validators.Commands;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.Pagamentos.Repositories;
using Backend.Core.Features.Pagamentos.Entities;
using Backend.Core.Features.Vendas.Repositories;
using Backend.Core.Features.Vendas.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Core.Features.Financeiro.Services;

public sealed class ContasReceberService : BaseService
{
    private readonly IContasReceberRepository _contasRepository;
    private readonly IClientesRepository _clientesRepository;
    private readonly INfesRepository _nfesRepository;
    private readonly ICondicoesPagamentosRepository _condicoesRepository;
    private readonly IVendasRepository _vendasRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ContasReceberService(
        IContasReceberRepository contasRepository,
        IClientesRepository clientesRepository,
        INfesRepository nfesRepository,
        ICondicoesPagamentosRepository condicoesRepository,
        IVendasRepository vendasRepository,
        IUnitOfWork unitOfWork)
    {
        _contasRepository = contasRepository;
        _clientesRepository = clientesRepository;
        _nfesRepository = nfesRepository;
        _condicoesRepository = condicoesRepository;
        _vendasRepository = vendasRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<ContasReceber>> ObterContasReceber(int pagina = 1, int tamanhoDaPagina = 20)
        => _contasRepository.ObterContasReceber(pagina, tamanhoDaPagina);

    public Task<ContasReceber?> ObterContaReceberPorId(int id)
        => _contasRepository.ObterContaReceberPorId(id);

    public Task<ResultadoPaginado<ContasReceber>> PesquisarContasReceber(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _contasRepository.PesquisarContasReceber(termo, pagina, tamanhoDaPagina);

    public async Task<Resultado<ContasReceber>> CriarContaReceber(CriarContaReceberCommand command)
    {
        var validator = new CriarContaReceberCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<ContasReceber>.Falha(validation.ToResultadoErros());

        var cliente = await _clientesRepository.ObterClientePorId(command.ClienteId);
        if (cliente is null)
            return Resultado<ContasReceber>.Falha(new ResultadoErro("CLIENTE_INEXISTENTE", "O cliente informado não existe.", "ClienteId"));

        Nfes? nfe = null;
        if (command.NfeId.HasValue)
        {
            nfe = await _nfesRepository.ObterNfePorId(command.NfeId.Value);
            if (nfe is null)
                return Resultado<ContasReceber>.Falha(new ResultadoErro("NFE_INEXISTENTE", "A nota fiscal informada não existe.", "NfeId"));
        }

        CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<ContasReceber>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        Venda? venda = null;
        if (command.VendaId.HasValue)
        {
            venda = await _vendasRepository.ObterVendaPorId(command.VendaId.Value);
            if (venda is null)
                return Resultado<ContasReceber>.Falha(new ResultadoErro("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId"));
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var conta = new ContasReceber(
                    command.Descricao,
                    command.ValorOriginal,
                    cliente,
                    command.DataEmissao,
                    command.DataVencimento,
                    condicao,
                    command.NfeId,
                    command.VendaId,
                    command.Observacao
                );

                foreach (var p in command.Parcelas)
                {
                    conta.AdicionarParcela(p.NumeroParcela, p.DataVencimento, p.ValorParcela);
                }

                var criada = await _contasRepository.CriarContaReceber(conta);
                _unitOfWork.Commit();

                return Resultado<ContasReceber>.Sucesso(criada);
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

        var existente = await _contasRepository.ObterContaReceberPorId(id);
        if (existente is null)
            return Resultado<ContasReceber>.Falha(new ResultadoErro("CONTA_RECEBER_NAO_ENCONTRADA", "Conta a receber não encontrada."));

        var cliente = await _clientesRepository.ObterClientePorId(command.ClienteId);
        if (cliente is null)
            return Resultado<ContasReceber>.Falha(new ResultadoErro("CLIENTE_INEXISTENTE", "O cliente informado não existe.", "ClienteId"));

        Nfes? nfe = null;
        if (command.NfeId.HasValue)
        {
            nfe = await _nfesRepository.ObterNfePorId(command.NfeId.Value);
            if (nfe is null)
                return Resultado<ContasReceber>.Falha(new ResultadoErro("NFE_INEXISTENTE", "A nota fiscal informada não existe.", "NfeId"));
        }

        CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<ContasReceber>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        Venda? venda = null;
        if (command.VendaId.HasValue)
        {
            venda = await _vendasRepository.ObterVendaPorId(command.VendaId.Value);
            if (venda is null)
                return Resultado<ContasReceber>.Falha(new ResultadoErro("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId"));
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var parcelas = command.Parcelas.Select(p => 
                    new ContasReceberParcelas(0, id, p.NumeroParcela, p.DataVencimento, p.ValorParcela, p.ValorRecebido, p.Status));

                existente.Atualizar(
                    command.Descricao,
                    command.ValorOriginal,
                    cliente,
                    parcelas,
                    command.DataEmissao,
                    command.DataVencimento,
                    condicao,
                    command.NfeId,
                    command.VendaId,
                    command.Observacao
                );

                var atualizada = await _contasRepository.AtualizarContaReceber(id, existente);
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

    public async Task<Resultado<ContasReceber>> RegistrarRecebimento(int id, RegistrarRecebimentoParcelaCommand command)
    {
        if (command.ValorRecebido <= 0)
            return Resultado<ContasReceber>.Falha(new ResultadoErro("VALOR_INVALIDO", "O valor de recebimento deve ser maior que zero.", "ValorRecebido"));

        var existente = await _contasRepository.ObterContaReceberPorId(id);
        if (existente is null)
            return Resultado<ContasReceber>.Falha(new ResultadoErro("CONTA_RECEBER_NAO_ENCONTRADA", "Conta a receber não encontrada."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                existente.RegistrarRecebimento(command.NumeroParcela, command.ValorRecebido);

                var atualizada = await _contasRepository.AtualizarContaReceber(id, existente);
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

    public async Task<bool> DeletarContaReceber(int id)
    {
        var existente = await _contasRepository.ObterContaReceberPorId(id);
        if (existente is null) return false;

        return await _contasRepository.DeletarContaReceber(id);
    }
}
