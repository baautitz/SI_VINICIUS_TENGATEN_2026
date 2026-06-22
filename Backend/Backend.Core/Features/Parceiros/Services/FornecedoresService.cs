using Backend.Core.Common.Results;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Parceiros.Validators.Commands;
using Backend.Core.Common.Extensions;
using Backend.Core.Common;
using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Repositories;
using FluentValidation;

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

    public async Task<Resultado<Fornecedores>> CriarFornecedor(CriarFornecedorCommand command)
    {
        var validation = new CriarFornecedorCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Fornecedores>.Falha(validation.ToResultadoErros());

        var nacionalidade = await _paisesRepository.ObterPaisPorId(command.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Fornecedores>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        Localizacao.Entities.Bairros? bairro = null;
        if (command.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(command.BairroId.Value);
            if (bairro is null)
                return Resultado<Fornecedores>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(command.CpfCnpj).Valor;

        if (await _fornecedoresRepository.ExisteFornecedorCpfCnpj(documentoLimpo, command.NacionalidadeId))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um fornecedor com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            Documento documento = Documento.Criar(command.CpfCnpj, nacionalidade.CodigoIsoPais, command.TipoPessoa);
            Documento? rgIe = Documento.CriarGenerico(command.RgIe);

            var fornecedor = new Fornecedores(
                command.TipoPessoa,
                command.NomeRazaosocial,
                documento,
                nacionalidade,
                rgIe,
                command.ApelidoNomefantasia,
                command.Endereco,
                bairro,
                command.Telefone,
                command.Email,
                command.Observacao
            );

            if (command.Ativo) fornecedor.Ativar();
            else fornecedor.Desativar();

            var criado = await _fornecedoresRepository.CriarFornecedor(fornecedor);
            return Resultado<Fornecedores>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Fornecedores>> AtualizarFornecedor(int id, AtualizarFornecedorCommand command)
    {
        var validation = new AtualizarFornecedorCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Fornecedores>.Falha(validation.ToResultadoErros());

        var existente = await _fornecedoresRepository.ObterFornecedorPorId(id);
        if (existente is null)
            return Resultado<Fornecedores>.Falha(new ResultadoErro("FORNECEDOR_NAO_ENCONTRADO", "Fornecedor não encontrado."));

        var nacionalidade = await _paisesRepository.ObterPaisPorId(command.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Fornecedores>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        Localizacao.Entities.Bairros? bairro = null;
        if (command.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(command.BairroId.Value);
            if (bairro is null)
                return Resultado<Fornecedores>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(command.CpfCnpj).Valor;

        if (await _fornecedoresRepository.ExisteFornecedorCpfCnpj(documentoLimpo, command.NacionalidadeId, id))
            return Resultado<Fornecedores>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro fornecedor com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            Documento documento = Documento.Criar(command.CpfCnpj, nacionalidade.CodigoIsoPais, command.TipoPessoa);
            Documento? rgIe = Documento.CriarGenerico(command.RgIe);

            existente.Atualizar(
                command.TipoPessoa,
                command.NomeRazaosocial,
                documento,
                nacionalidade,
                rgIe,
                command.ApelidoNomefantasia,
                command.Endereco,
                bairro,
                command.Telefone,
                command.Email,
                command.Observacao
            );

            if (command.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _fornecedoresRepository.AtualizarFornecedor(id, existente);
            return Resultado<Fornecedores>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarFornecedor(int id)
        => _fornecedoresRepository.DeletarFornecedor(id);

    public Task<ResultadoPaginado<Fornecedores>> PesquisarFornecedores(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _fornecedoresRepository.PesquisarFornecedores(termo, pagina, tamanhoDaPagina);
}

