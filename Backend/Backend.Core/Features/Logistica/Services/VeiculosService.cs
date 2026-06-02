using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Logistica.DTOs;
using Backend.Core.Features.Logistica.Entities;
using Backend.Core.Features.Logistica.Repositories;
using Backend.Core.Features.Logistica.Validators;

namespace Backend.Core.Features.Logistica.Services;

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

    public async Task<Resultado<Veiculos>> CriarVeiculo(CreateVeiculoDto dto)
    {
        var validator = new CreateVeiculoDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Veiculos>.Falha(validation.ToResultadoErros());

        Transportadoras? transportadora = null;
        if (dto.TransportadoraId.HasValue)
        {
            transportadora = await _transportadorasRepository.ObterTransportadoraPorId(dto.TransportadoraId.Value);
            if (transportadora is null)
                return Resultado<Veiculos>.Falha(new ResultadoErro("TRANSPORTADORA_NAO_ENCONTRADA", "A transportadora informada não foi encontrada.", "TransportadoraId"));
        }

        var estado = await _estadosRepository.ObterEstadoPorId(dto.EstadoId);
        if (estado is null)
            return Resultado<Veiculos>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "O estado informado não foi encontrado.", "EstadoId"));

        return await ExecuteResultAsync(async () =>
        {
            var veiculo = new Veiculos(
                dto.Placa,
                dto.EstadoId,
                dto.TransportadoraId,
                dto.Rntrc,
                dto.Renavam,
                dto.TipoVeiculo,
                dto.MarcaModelo,
                dto.Observacao
            );

            veiculo.VincularEstado(estado);
            if (transportadora != null) veiculo.VincularTransportadora(transportadora);

            var criado = await _veiculosRepository.CriarVeiculo(veiculo);
            return Resultado<Veiculos>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Veiculos>> AtualizarVeiculo(int id, UpdateVeiculoDto dto)
    {
        var validator = new UpdateVeiculoDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Veiculos>.Falha(validation.ToResultadoErros());

        var existente = await _veiculosRepository.ObterVeiculoPorId(id);
        if (existente is null)
            return Resultado<Veiculos>.Falha(new ResultadoErro("VEICULO_NAO_ENCONTRADO", "Veículo não encontrado."));

        Transportadoras? transportadora = null;
        if (dto.TransportadoraId.HasValue)
        {
            transportadora = await _transportadorasRepository.ObterTransportadoraPorId(dto.TransportadoraId.Value);
            if (transportadora is null)
                return Resultado<Veiculos>.Falha(new ResultadoErro("TRANSPORTADORA_NAO_ENCONTRADA", "A transportadora informada não foi encontrada.", "TransportadoraId"));
        }

        var estado = await _estadosRepository.ObterEstadoPorId(dto.EstadoId);
        if (estado is null)
            return Resultado<Veiculos>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "O estado informado não foi encontrado.", "EstadoId"));

        return await ExecuteResultAsync(async () =>
        {
            existente.Atualizar(
                dto.Placa,
                dto.EstadoId,
                dto.TransportadoraId,
                dto.Rntrc,
                dto.Renavam,
                dto.TipoVeiculo,
                dto.MarcaModelo,
                dto.Observacao
            );

            existente.VincularEstado(estado);
            if (transportadora != null) existente.VincularTransportadora(transportadora);

            if (dto.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _veiculosRepository.AtualizarVeiculo(id, existente);
            return Resultado<Veiculos>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarVeiculo(int id)
        => _veiculosRepository.DeletarVeiculo(id);

    public Task<ResultadoPaginado<VeiculosResumo>> ObterVeiculosResumo(int pagina = 1, int tamanhoDaPagina = 20)
        => _veiculosRepository.ObterVeiculosResumo(pagina, tamanhoDaPagina);

    public Task<ResultadoPaginado<VeiculosResumo>> PesquisarVeiculos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _veiculosRepository.PesquisarVeiculos(termo, pagina, tamanhoDaPagina);
}
