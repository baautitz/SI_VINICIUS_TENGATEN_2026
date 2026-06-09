using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class UnidadesMedidaService : BaseService
{
    private readonly IUnidadesMedidaRepository _unidadesMedidaRepository;

    public UnidadesMedidaService(IUnidadesMedidaRepository unidadesMedidaRepository)
    {
        _unidadesMedidaRepository = unidadesMedidaRepository;
    }

    public Task<ResultadoPaginado<UnidadesMedida>> ObterUnidadesMedida(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _unidadesMedidaRepository.ObterUnidadesMedida(pagina, tamanhoPagina);
        }
        return _unidadesMedidaRepository.PesquisarUnidadesMedida(search, pagina, tamanhoPagina);
    }

    public Task<UnidadesMedida?> ObterUnidadeMedidaPorId(int id)
        => _unidadesMedidaRepository.ObterUnidadeMedidaPorId(id);

    public async Task<Resultado<UnidadesMedida>> CriarUnidadeMedida(CriarUnidadeMedidaCommand command)
    {
        var validation = new CriarUnidadeMedidaCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<UnidadesMedida>.Falha(validation.ToResultadoErros());

        if (await _unidadesMedidaRepository.ExisteSigla(command.Sigla))
            return Resultado<UnidadesMedida>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe uma unidade de medida com esta sigla.", "Sigla"));

        var entidade = new UnidadesMedida(command.Sigla, command.Descricao, command.Categoria, command.PermiteDecimais, command.Ativo);

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _unidadesMedidaRepository.CriarUnidadeMedida(entidade);
            return Resultado<UnidadesMedida>.Sucesso(criado);
        });
    }

    public async Task<Resultado<UnidadesMedida>> AtualizarUnidadeMedida(int id, AtualizarUnidadeMedidaCommand command)
    {
        var validation = new AtualizarUnidadeMedidaCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<UnidadesMedida>.Falha(validation.ToResultadoErros());

        var existente = await _unidadesMedidaRepository.ObterUnidadeMedidaPorId(id);
        if (existente is null)
            return Resultado<UnidadesMedida>.Falha(new ResultadoErro("UNIDADE_MEDIDA_NAO_ENCONTRADA", "Unidade de medida não encontrada."));

        if (await _unidadesMedidaRepository.ExisteSigla(command.Sigla, id))
            return Resultado<UnidadesMedida>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outra unidade de medida com esta sigla.", "Sigla"));

        existente.Atualizar(command.Sigla, command.Descricao, command.Categoria, command.PermiteDecimais, command.Ativo);
        
        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _unidadesMedidaRepository.AtualizarUnidadeMedida(id, existente);
            return Resultado<UnidadesMedida>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarUnidadeMedida(int id)
        => _unidadesMedidaRepository.DeletarUnidadeMedida(id);
}
