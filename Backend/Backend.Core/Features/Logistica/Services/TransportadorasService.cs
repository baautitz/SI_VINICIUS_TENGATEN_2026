using Backend.Core.Common;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Logistica.DTOs;
using Backend.Core.Features.Logistica.Entities;
using Backend.Core.Features.Logistica.Repositories;
using Backend.Core.Features.Logistica.Validators;

namespace Backend.Core.Features.Logistica.Services;

public sealed class TransportadorasService : BaseService
{
    private readonly ITransportadorasRepository _transportadorasRepository;
    private readonly IBairrosRepository _bairrosRepository;

    public TransportadorasService(ITransportadorasRepository transportadorasRepository, IBairrosRepository bairrosRepository)
    {
        _transportadorasRepository = transportadorasRepository;
        _bairrosRepository = bairrosRepository;
    }

    public Task<ResultadoPaginado<Transportadoras>> ObterTransportadoras(int pagina = 1, int tamanhoDaPagina = 20)
        => _transportadorasRepository.ObterTransportadoras(pagina, tamanhoDaPagina);

    public Task<Transportadoras?> ObterTransportadoraPorId(int id)
        => _transportadorasRepository.ObterTransportadoraPorId(id);

    public async Task<Resultado<Transportadoras>> CriarTransportadora(CreateTransportadoraDto dto)
    {
        var validator = new CreateTransportadoraDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Transportadoras>.Falha(validation.ToResultadoErros());

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Transportadoras>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var cpfCnpjNormalizado = new CpfCnpj(dto.CpfCnpj);

        if (await _transportadorasRepository.ExisteTransportadoraCpfCnpj(cpfCnpjNormalizado))
            return Resultado<Transportadoras>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe uma transportadora com este CPF ou CNPJ.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var transportadora = new Transportadoras(
                dto.NomeRazaosocial,
                cpfCnpjNormalizado,
                dto.RgIe,
                dto.ApelidoNomefantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.Rntrc,
                dto.Observacao
            );

            var criado = await _transportadorasRepository.CriarTransportadora(transportadora);
            return Resultado<Transportadoras>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Transportadoras>> AtualizarTransportadora(int id, UpdateTransportadoraDto dto)
    {
        var validator = new UpdateTransportadoraDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Transportadoras>.Falha(validation.ToResultadoErros());

        var existente = await _transportadorasRepository.ObterTransportadoraPorId(id);
        if (existente is null)
            return Resultado<Transportadoras>.Falha(new ResultadoErro("TRANSPORTADORA_NAO_ENCONTRADO", "Transportadora não encontrada."));

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Transportadoras>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var cpfCnpjNormalizado = new CpfCnpj(dto.CpfCnpj);

        if (await _transportadorasRepository.ExisteTransportadoraCpfCnpj(cpfCnpjNormalizado, id))
            return Resultado<Transportadoras>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outra transportadora com este CPF ou CNPJ.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.Atualizar(
                dto.NomeRazaosocial,
                cpfCnpjNormalizado,
                dto.RgIe,
                dto.ApelidoNomefantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.Rntrc,
                dto.Observacao
            );

            if (dto.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _transportadorasRepository.AtualizarTransportadora(id, existente);
            return Resultado<Transportadoras>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarTransportadora(int id)
        => _transportadorasRepository.DeletarTransportadora(id);

    public Task<ResultadoPaginado<TransportadorasResumo>> ObterTransportadorasResumo(int pagina = 1, int tamanhoDaPagina = 20)
        => _transportadorasRepository.ObterTransportadorasResumo(pagina, tamanhoDaPagina);

    public Task<ResultadoPaginado<TransportadorasResumo>> PesquisarTransportadoras(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _transportadorasRepository.PesquisarTransportadoras(termo, pagina, tamanhoDaPagina);
}
