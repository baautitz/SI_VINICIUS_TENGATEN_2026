using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Parceiros.Validators.Commands;

namespace Backend.Core.Features.Parceiros.Services;

// todo: validar placa de transportadora brasileira e bairro só pode ser da nacionalidade da transportadora
public sealed class VeiculosService : BaseService
{
    private readonly IVeiculosRepository _veiculosRepository;
    private readonly ITransportadorasRepository _transportadorasRepository;
    private readonly IEstadosRepository _estadosRepository;

    public VeiculosService(IVeiculosRepository veiculosRepository, ITransportadorasRepository transportadorasRepository, IEstadosRepository estadosRepository)
    {
        _veiculosRepository = veiculosRepository;
        _transportadorasRepository = transportadorasRepository;
        _estadosRepository = estadosRepository;
    }

    public Task<ResultadoPaginado<Veiculos>> ObterVeiculos(int pagina = 1, int tamanhoDaPagina = 20)
        => _veiculosRepository.ObterVeiculos(pagina, tamanhoDaPagina);

    public Task<Veiculos?> ObterVeiculoPorId(int id)
        => _veiculosRepository.ObterVeiculoPorId(id);

    public async Task<Resultado<Veiculos>> CriarVeiculo(CriarVeiculoCommand command)
    {
        var validator = new CriarVeiculoCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Veiculos>.Falha(validation.ToResultadoErros());

        Transportadoras? transportadora = null;
        if (command.TransportadoraId.HasValue && command.TransportadoraId.Value > 0)
        {
            transportadora = await _transportadorasRepository.ObterTransportadoraPorId(command.TransportadoraId.Value);
            if (transportadora is null)
                return Resultado<Veiculos>.Falha(new ResultadoErro("TRANSPORTADORA_NAO_ENCONTRADA", "A transportadora informada não foi encontrada.", "TransportadoraId"));
        }

        var estado = await _estadosRepository.ObterEstadoPorId(command.EstadoId);
        if (estado is null)
            return Resultado<Veiculos>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "O estado informado não foi encontrado.", "EstadoId"));

        return await ExecuteResultAsync(async () =>
        {
            var veiculo = new Veiculos(
                command.Placa,
                estado,
                transportadora,
                command.Rntrc,
                command.Renavam,
                command.TipoVeiculo,
                command.MarcaModelo,
                command.Observacao
            );

            if (!command.Ativo) veiculo.Desativar();

            var criado = await _veiculosRepository.CriarVeiculo(veiculo);
            return Resultado<Veiculos>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Veiculos>> AtualizarVeiculo(int id, AtualizarVeiculoCommand command)
    {
        var validator = new AtualizarVeiculoCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Veiculos>.Falha(validation.ToResultadoErros());

        var existente = await _veiculosRepository.ObterVeiculoPorId(id);
        if (existente is null)
            return Resultado<Veiculos>.Falha(new ResultadoErro("VEICULO_NAO_ENCONTRADO", "Veículo não encontrado."));

        Transportadoras? transportadora = null;
        if (command.TransportadoraId.HasValue && command.TransportadoraId.Value > 0)
        {
            transportadora = await _transportadorasRepository.ObterTransportadoraPorId(command.TransportadoraId.Value);
            if (transportadora is null)
                return Resultado<Veiculos>.Falha(new ResultadoErro("TRANSPORTADORA_NAO_ENCONTRADA", "A transportadora informada não foi encontrada.", "TransportadoraId"));
        }

        var estado = await _estadosRepository.ObterEstadoPorId(command.EstadoId);
        if (estado is null)
            return Resultado<Veiculos>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "O estado informado não foi encontrado.", "EstadoId"));

        return await ExecuteResultAsync(async () =>
        {
            existente.Atualizar(
                command.Placa,
                estado,
                transportadora,
                command.Rntrc,
                command.Renavam,
                command.TipoVeiculo,
                command.MarcaModelo,
                command.Observacao
            );

            if (command.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _veiculosRepository.AtualizarVeiculo(id, existente);
            return Resultado<Veiculos>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarVeiculo(int id)
        => _veiculosRepository.DeletarVeiculo(id);

    public Task<ResultadoPaginado<Veiculos>> PesquisarVeiculos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _veiculosRepository.PesquisarVeiculos(termo, pagina, tamanhoDaPagina);
}
