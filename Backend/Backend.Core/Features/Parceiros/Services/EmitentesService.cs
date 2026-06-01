using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Parceiros.DTOs;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Parceiros.Validators;

namespace Backend.Core.Features.Parceiros.Services;

public sealed class EmitentesService : BaseService
{
    private readonly IEmitentesRepository _emitentesRepository;
    private readonly IBairrosRepository _bairrosRepository;

    public EmitentesService(IEmitentesRepository emitentesRepository, IBairrosRepository bairrosRepository)
    {
        _emitentesRepository = emitentesRepository;
        _bairrosRepository = bairrosRepository;
    }

    public Task<ResultadoPaginado<Emitentes>> ObterEmitentes(int pagina = 1, int tamanhoDaPagina = 20)
        => _emitentesRepository.ObterEmitentes(pagina, tamanhoDaPagina);

    public Task<Emitentes?> ObterEmitentePorId(int id)
        => _emitentesRepository.ObterEmitentePorId(id);

    public async Task<Resultado<Emitentes>> CriarEmitente(CreateEmitenteDto dto)
    {
        var validator = new CreateEmitenteDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Emitentes>.Falha(validation.ToResultadoErros());

        Localizacao.Entities.Bairros? bairro = null;
        int? paisId = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Emitentes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
            paisId = bairro.Cidade?.Estado?.Pais?.Id;
        }

        var cpfCnpjNormalizado = TextNormalization.NormalizeDocument(dto.CpfCnpj);

        if (await _emitentesRepository.ExisteEmitenteCpfCnpj(cpfCnpjNormalizado, paisId))
            return Resultado<Emitentes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um emitente com este CPF ou CNPJ.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var emitente = new Emitentes(
                dto.NomeRazaoSocial,
                cpfCnpjNormalizado,
                dto.ApelidoNomeFantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.RgIe,
                dto.InscricaoMunicipal,
                dto.RegimeTributario,
                dto.Observacao,
                dto.Ativo
            );

            var criado = await _emitentesRepository.CriarEmitente(emitente);
            return Resultado<Emitentes>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Emitentes>> AtualizarEmitente(int id, UpdateEmitenteDto dto)
    {
        var validator = new UpdateEmitenteDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Emitentes>.Falha(validation.ToResultadoErros());

        var existente = await _emitentesRepository.ObterEmitentePorId(id);
        if (existente is null)
            return Resultado<Emitentes>.Falha(new ResultadoErro("EMITENTE_NAO_ENCONTRADO", "Emitente não encontrado."));

        Localizacao.Entities.Bairros? bairro = null;
        int? paisId = null;
        string? siglaIso = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Emitentes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
            paisId = bairro.Cidade?.Estado?.Pais?.Id;
            siglaIso = bairro.Cidade?.Estado?.Pais?.SiglaIso;
        }

        if (siglaIso == "BRA" && !CpfCnpjValidatorUtils.IsValid(dto.CpfCnpj))
            return Resultado<Emitentes>.Falha(new ResultadoErro("DOCUMENTO_INVALIDO", "CPF ou CNPJ inválido para o Brasil.", "CpfCnpj"));

        var cpfCnpjNormalizado = TextNormalization.NormalizeDocument(dto.CpfCnpj);

        if (await _emitentesRepository.ExisteEmitenteCpfCnpj(cpfCnpjNormalizado, paisId, id))
            return Resultado<Emitentes>.Falha(new ResultadoErro("DUPLICIDADE", siglaIso == "BRA" ? "Já existe outro emitente com este CPF ou CNPJ." : "Já existe outro emitente com este Documento.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.AtualizarDados(
                dto.NomeRazaoSocial,
                cpfCnpjNormalizado,
                dto.ApelidoNomeFantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.RgIe,
                dto.InscricaoMunicipal,
                dto.RegimeTributario,
                dto.Observacao
            );

            var atualizado = await _emitentesRepository.AtualizarEmitente(id, existente);
            return Resultado<Emitentes>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarEmitente(int id)
        => _emitentesRepository.DeletarEmitente(id);

    public Task<ResultadoPaginado<EmitentesResumo>> ObterEmitentesResumo(int pagina = 1, int tamanhoDaPagina = 20)
        => _emitentesRepository.ObterEmitentesResumo(pagina, tamanhoDaPagina);

    public Task<ResultadoPaginado<EmitentesResumo>> PesquisarEmitentes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _emitentesRepository.PesquisarEmitentes(termo, pagina, tamanhoDaPagina);
}
