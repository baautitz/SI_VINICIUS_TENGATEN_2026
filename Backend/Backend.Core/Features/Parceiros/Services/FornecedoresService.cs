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

public sealed class FornecedoresService : BaseService
{
    private readonly IFornecedoresRepository _fornecedoresRepository;
    private readonly IBairrosRepository _bairrosRepository;
    private readonly IPaisesRepository _paisesRepository;

    public FornecedoresService(IFornecedoresRepository fornecedoresRepository, IBairrosRepository bairrosRepository, IPaisesRepository paisesRepository)
    {
        _fornecedoresRepository = fornecedoresRepository;
        _bairrosRepository = bairrosRepository;
        _paisesRepository = paisesRepository;
    }

    public Task<ResultadoPaginado<Fornecedores>> ObterFornecedores(int pagina = 1, int tamanhoDaPagina = 20)
        => _fornecedoresRepository.ObterFornecedores(pagina, tamanhoDaPagina);

    public Task<Fornecedores?> ObterFornecedorPorId(int id)
        => _fornecedoresRepository.ObterFornecedorPorId(id);

    public async Task<Resultado<Fornecedores>> CriarFornecedor(CreateFornecedorDto dto)
    {
        var validator = new CreateFornecedorDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Fornecedores>.Falha(validation.ToResultadoErros());

        var nacionalidade = await _paisesRepository.ObterPaisPorId(dto.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Fornecedores>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso != "BRA" && !string.IsNullOrWhiteSpace(dto.RgIe))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(dto.RgIe)));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (dto.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(dto.CpfCnpj).EhValido())
                    return Resultado<Fornecedores>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(dto.CpfCnpj).EhValido())
                    return Resultado<Fornecedores>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Fornecedores>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(dto.CpfCnpj).Valor;

        if (await _fornecedoresRepository.ExisteFornecedorCpfCnpj(documentoLimpo, dto.NacionalidadeId))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um fornecedor com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var fornecedor = new Fornecedores(
                dto.TipoPessoa,
                dto.NomeRazaosocial,
                documentoLimpo,
                nacionalidade,
                dto.RgIe,
                dto.ApelidoNomefantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.Observacao
            );

            var criado = await _fornecedoresRepository.CriarFornecedor(fornecedor);
            return Resultado<Fornecedores>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Fornecedores>> AtualizarFornecedor(int id, UpdateFornecedorDto dto)
    {
        var validator = new UpdateFornecedorDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Fornecedores>.Falha(validation.ToResultadoErros());

        var existente = await _fornecedoresRepository.ObterFornecedorPorId(id);
        if (existente is null)
            return Resultado<Fornecedores>.Falha(new ResultadoErro("FORNECEDOR_NAO_ENCONTRADO", "Fornecedor não encontrado."));

        var nacionalidade = await _paisesRepository.ObterPaisPorId(dto.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Fornecedores>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso != "BRA" && !string.IsNullOrWhiteSpace(dto.RgIe))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(dto.RgIe)));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (dto.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(dto.CpfCnpj).EhValido())
                    return Resultado<Fornecedores>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(dto.CpfCnpj).EhValido())
                    return Resultado<Fornecedores>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Fornecedores>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(dto.CpfCnpj).Valor;

        if (await _fornecedoresRepository.ExisteFornecedorCpfCnpj(documentoLimpo, dto.NacionalidadeId, id))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro fornecedor com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.Atualizar(
                dto.TipoPessoa,
                dto.NomeRazaosocial,
                documentoLimpo,
                nacionalidade,
                dto.RgIe,
                dto.ApelidoNomefantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.Observacao
            );

            if (dto.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _fornecedoresRepository.AtualizarFornecedor(id, existente);
            return Resultado<Fornecedores>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarFornecedor(int id)
        => _fornecedoresRepository.DeletarFornecedor(id);

    public Task<ResultadoPaginado<FornecedoresResumo>> ObterFornecedoresResumo(int pagina = 1, int tamanhoDaPagina = 20)
        => _fornecedoresRepository.ObterFornecedoresResumo(pagina, tamanhoDaPagina);

    public Task<ResultadoPaginado<FornecedoresResumo>> PesquisarFornecedores(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _fornecedoresRepository.PesquisarFornecedores(termo, pagina, tamanhoDaPagina);
}
