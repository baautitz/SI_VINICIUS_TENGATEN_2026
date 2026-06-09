using Backend.Core.Common.Enums;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Parceiros.Validators.Commands;

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

    public async Task<Resultado<Emitentes>> CriarEmitente(CriarEmitenteCommand command)
    {
        var validator = new CriarEmitenteCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Emitentes>.Falha(validation.ToResultadoErros());

        var nacionalidade = await _paisesRepository.ObterPaisPorId(command.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Emitentes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso != "BRA" && !string.IsNullOrWhiteSpace(command.RgIe))
            return Resultado<Emitentes>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(command.RgIe)));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (command.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(command.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(command.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (command.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(command.BairroId.Value);
            if (bairro is null)
                return Resultado<Emitentes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(command.CpfCnpj).Valor;

        if (await _emitentesRepository.ExisteEmitenteCpfCnpj(documentoLimpo, command.NacionalidadeId))
            return Resultado<Emitentes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um emitente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var emitente = new Emitentes(
                command.TipoPessoa,
                command.NomeRazaoSocial,
                documentoLimpo,
                nacionalidade,
                command.ApelidoNomeFantasia,
                command.Endereco,
                bairro,
                command.Telefone,
                command.Email,
                command.RgIe,
                command.InscricaoMunicipal,
                command.RegimeTributario,
                command.Observacao,
                command.Ativo
            );

            var criado = await _emitentesRepository.CriarEmitente(emitente);
            return Resultado<Emitentes>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Emitentes>> AtualizarEmitente(int id, AtualizarEmitenteCommand command)
    {
        var validator = new AtualizarEmitenteCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Emitentes>.Falha(validation.ToResultadoErros());

        var existente = await _emitentesRepository.ObterEmitentePorId(id);
        if (existente is null)
            return Resultado<Emitentes>.Falha(new ResultadoErro("EMITENTE_NAO_ENCONTRADO", "Emitente não encontrado."));

        var nacionalidade = await _paisesRepository.ObterPaisPorId(command.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Emitentes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso != "BRA" && !string.IsNullOrWhiteSpace(command.RgIe))
            return Resultado<Emitentes>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(command.RgIe)));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (command.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(command.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(command.CpfCnpj).EhValido())
                    return Resultado<Emitentes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (command.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(command.BairroId.Value);
            if (bairro is null)
                return Resultado<Emitentes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(command.CpfCnpj).Valor;

        if (await _emitentesRepository.ExisteEmitenteCpfCnpj(documentoLimpo, command.NacionalidadeId, id))
            return Resultado<Emitentes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro emitente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.AtualizarDados(
                command.TipoPessoa,
                command.NomeRazaoSocial,
                documentoLimpo,
                nacionalidade,
                command.ApelidoNomeFantasia,
                command.Endereco,
                bairro,
                command.Telefone,
                command.Email,
                command.RgIe,
                command.InscricaoMunicipal,
                command.RegimeTributario,
                command.Observacao
            );

            if (command.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _emitentesRepository.AtualizarEmitente(id, existente);
            return Resultado<Emitentes>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarEmitente(int id)
        => _emitentesRepository.DeletarEmitente(id);

    public Task<ResultadoPaginado<Emitentes>> PesquisarEmitentes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _emitentesRepository.PesquisarEmitentes(termo, pagina, tamanhoDaPagina);
}
