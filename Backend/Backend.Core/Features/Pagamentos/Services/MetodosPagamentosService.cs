using Backend.Core.Common;
using Backend.Core.Common.Results;
using Backend.Core.Common.Extensions;
using Backend.Core.Features.Pagamentos.Commands;
using Backend.Core.Features.Pagamentos.Entities;
using Backend.Core.Features.Pagamentos.Repositories;
using Backend.Core.Features.Pagamentos.Validators.Commands;

namespace Backend.Core.Features.Pagamentos.Services;

public sealed class MetodosPagamentosService : BaseService
{
    private readonly IMetodosPagamentosRepository _metodosRepository;

    public MetodosPagamentosService(IMetodosPagamentosRepository metodosRepository)
    {
        _metodosRepository = metodosRepository;
    }

    public Task<ResultadoPaginado<MetodosPagamentos>> ObterMetodosPagamentos(int pagina = 1, int tamanhoDaPagina = 20)
        => _metodosRepository.ObterMetodosPagamentos(pagina, tamanhoDaPagina);

    public Task<MetodosPagamentos?> ObterMetodoPagamentoPorId(int id)
        => _metodosRepository.ObterMetodoPagamentoPorId(id);

    public Task<ResultadoPaginado<MetodosPagamentos>> PesquisarMetodosPagamentos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _metodosRepository.PesquisarMetodosPagamentos(termo, pagina, tamanhoDaPagina);

    public async Task<Resultado<MetodosPagamentos>> CriarMetodoPagamento(CriarMetodoPagamentoCommand command)
    {
        var validator = new CriarMetodoPagamentoCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<MetodosPagamentos>.Falha(validation.ToResultadoErros());

        string codigoFinal = command.Codigo;

        if (string.IsNullOrWhiteSpace(codigoFinal))
        {
            var ultimoCodigo = await _metodosRepository.ObterUltimoCodigo();
            int proximoNumero = 1;
            if (!string.IsNullOrEmpty(ultimoCodigo) && int.TryParse(ultimoCodigo, out var ultimoNum))
            {
                proximoNumero = ultimoNum + 1;
            }
            codigoFinal = proximoNumero.ToString("D2");
        }
        else
        {
            if (await _metodosRepository.ExisteCodigo(codigoFinal))
                return Resultado<MetodosPagamentos>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um método de pagamento com este código.", "Codigo"));
        }

        return await ExecuteResultAsync(async () =>
        {
            var metodo = new MetodosPagamentos(codigoFinal, command.Descricao, command.Ativo);
            var criado = await _metodosRepository.CriarMetodoPagamento(metodo);
            return Resultado<MetodosPagamentos>.Sucesso(criado);
        });
    }

    public async Task<Resultado<MetodosPagamentos>> AtualizarMetodoPagamento(int id, AtualizarMetodoPagamentoCommand command)
    {
        var validator = new ResumoAtualizarMetodoPagamentoCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<MetodosPagamentos>.Falha(validation.ToResultadoErros());

        var existente = await _metodosRepository.ObterMetodoPagamentoPorId(id);
        if (existente is null)
            return Resultado<MetodosPagamentos>.Falha(new ResultadoErro("METODO_NAO_ENCONTRADO", "Método de pagamento não encontrado."));

        return await ExecuteResultAsync(async () =>
        {
            existente.AtualizarDescricao(command.Descricao);
            if (command.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _metodosRepository.AtualizarMetodoPagamento(id, existente);
            return Resultado<MetodosPagamentos>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarMetodoPagamento(int id)
        => _metodosRepository.DeletarMetodoPagamento(id);
}
