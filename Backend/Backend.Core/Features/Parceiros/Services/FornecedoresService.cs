using Backend.Core.Common;
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

    public FornecedoresService(IFornecedoresRepository fornecedoresRepository, IBairrosRepository bairrosRepository)
    {
        _fornecedoresRepository = fornecedoresRepository;
        _bairrosRepository = bairrosRepository;
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

        Localizacao.Entities.Bairros? bairro = null;
        int? paisId = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Fornecedores>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
            paisId = bairro.Cidade?.Estado?.Pais?.Id;
        }

        if (await _fornecedoresRepository.ExisteFornecedorCpfCnpj(dto.CpfCnpj, paisId))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um fornecedor com este CPF ou CNPJ.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var fornecedor = new Fornecedores(
                dto.NomeRazaosocial,
                dto.CpfCnpj,
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

        Localizacao.Entities.Bairros? bairro = null;
        int? paisId = null;
        string? siglaIso = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Fornecedores>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
            paisId = bairro.Cidade?.Estado?.Pais?.Id;
            siglaIso = bairro.Cidade?.Estado?.Pais?.SiglaIso;
        }

        if (siglaIso == "BRA" && !CpfCnpjValidatorUtils.IsValid(dto.CpfCnpj))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("DOCUMENTO_INVALIDO", "CPF ou CNPJ inválido para o Brasil.", "CpfCnpj"));

        if (await _fornecedoresRepository.ExisteFornecedorCpfCnpj(dto.CpfCnpj, paisId, id))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("DUPLICIDADE", siglaIso == "BRA" ? "Já existe outro fornecedor com este CPF ou CNPJ." : "Já existe outro fornecedor com este Documento.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.Atualizar(
                dto.NomeRazaosocial,
                dto.CpfCnpj,
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
