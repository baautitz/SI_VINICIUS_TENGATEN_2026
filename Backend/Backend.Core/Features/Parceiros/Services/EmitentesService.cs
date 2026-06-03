using Backend.Core.Common.Enums;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Common.ValueObjects;
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
    private readonly IPaisesRepository _paisesRepository;

    public EmitentesService(IEmitentesRepository emitentesRepository, IBairrosRepository bairrosRepository, IPaisesRepository paisesRepository)
    {
        _emitentesRepository = emitentesRepository;
        _bairrosRepository = bairrosRepository;
        _paisesRepository = paisesRepository;
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

        var nacionalidade = await _paisesRepository.ObterPaisPorId(dto.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Emitentes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso != "BRA" && !string.IsNullOrWhiteSpace(dto.RgIe))
            return Resultado<Emitentes>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(dto.RgIe)));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (dto.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(dto.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(dto.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Emitentes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(dto.CpfCnpj).Valor;

        if (await _emitentesRepository.ExisteEmitenteCpfCnpj(documentoLimpo, dto.NacionalidadeId))
            return Resultado<Emitentes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um emitente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var emitente = new Emitentes(
                dto.TipoPessoa,
                dto.NomeRazaoSocial,
                documentoLimpo,
                nacionalidade,
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

        var nacionalidade = await _paisesRepository.ObterPaisPorId(dto.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Emitentes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso != "BRA" && !string.IsNullOrWhiteSpace(dto.RgIe))
            return Resultado<Emitentes>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(dto.RgIe)));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (dto.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(dto.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(dto.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Emitentes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(dto.CpfCnpj).Valor;

        if (await _emitentesRepository.ExisteEmitenteCpfCnpj(documentoLimpo, dto.NacionalidadeId, id))
            return Resultado<Emitentes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro emitente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.AtualizarDados(
                dto.TipoPessoa,
                dto.NomeRazaoSocial,
                documentoLimpo,
                nacionalidade,
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
