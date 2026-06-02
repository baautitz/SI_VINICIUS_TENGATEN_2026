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

public sealed class ClientesService : BaseService
{
    private readonly IClientesRepository _clientesRepository;
    private readonly IBairrosRepository _bairrosRepository;

    public ClientesService(IClientesRepository clientesRepository, IBairrosRepository bairrosRepository)
    {
        _clientesRepository = clientesRepository;
        _bairrosRepository = bairrosRepository;
    }

    public Task<ResultadoPaginado<Clientes>> ObterClientes(int pagina = 1, int tamanhoDaPagina = 20)
        => _clientesRepository.ObterClientes(pagina, tamanhoDaPagina);

    public Task<Clientes?> ObterClientePorId(int id)
        => _clientesRepository.ObterClientePorId(id);

    public async Task<Resultado<Clientes>> CriarCliente(CreateClienteDto dto)
    {
        var validator = new CreateClienteDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Clientes>.Falha(validation.ToResultadoErros());

        Localizacao.Entities.Bairros? bairro = null;
        int? paisId = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Clientes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
            paisId = bairro.Cidade?.Estado?.Pais?.Id;
        }

        var cpfCnpjNormalizado = new CpfCnpj(dto.CpfCnpj).Valor;

        if (await _clientesRepository.ExisteClienteCpfCnpj(cpfCnpjNormalizado, paisId))
            return Resultado<Clientes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um cliente com este CPF ou CNPJ.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var cliente = new Clientes(
                dto.NomeRazaoSocial,
                cpfCnpjNormalizado,
                dto.RgIe,
                dto.ApelidoNomeFantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.LimiteCredito,
                dto.Observacao,
                dto.Ativo
            );

            var criado = await _clientesRepository.CriarCliente(cliente);
            return Resultado<Clientes>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Clientes>> AtualizarCliente(int id, UpdateClienteDto dto)
    {
        var validator = new UpdateClienteDtoValidator();
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Resultado<Clientes>.Falha(validation.ToResultadoErros());

        var existente = await _clientesRepository.ObterClientePorId(id);
        if (existente is null)
            return Resultado<Clientes>.Falha(new ResultadoErro("CLIENTE_NAO_ENCONTRADO", "Cliente não encontrado."));

        Localizacao.Entities.Bairros? bairro = null;
        int? paisId = null;
        string? siglaIso = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Clientes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
            paisId = bairro.Cidade?.Estado?.Pais?.Id;
            siglaIso = bairro.Cidade?.Estado?.Pais?.SiglaIso;
        }

        if (siglaIso == "BRA" && !CpfCnpjValidatorUtils.IsValid(dto.CpfCnpj))
            return Resultado<Clientes>.Falha(new ResultadoErro("DOCUMENTO_INVALIDO", "CPF ou CNPJ inválido para o Brasil.", "CpfCnpj"));

        var cpfCnpjNormalizado = new CpfCnpj(dto.CpfCnpj).Valor;

        if (await _clientesRepository.ExisteClienteCpfCnpj(cpfCnpjNormalizado, paisId, id))
            return Resultado<Clientes>.Falha(new ResultadoErro("DUPLICIDADE", siglaIso == "BRA" ? "Já existe outro cliente com este CPF ou CNPJ." : "Já existe outro cliente com este Documento.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.AtualizarDados(
                dto.NomeRazaoSocial,
                cpfCnpjNormalizado,
                dto.RgIe,
                dto.ApelidoNomeFantasia,
                dto.Endereco,
                bairro,
                dto.Telefone,
                dto.Email,
                dto.LimiteCredito,
                dto.Observacao
            );


            if (dto.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _clientesRepository.AtualizarCliente(id, existente);
            return Resultado<Clientes>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarCliente(int id)
        => _clientesRepository.DeletarCliente(id);

    public Task<ResultadoPaginado<ClientesResumo>> ObterClientesResumo(int pagina = 1, int tamanhoDaPagina = 20)
        => _clientesRepository.ObterClientesResumo(pagina, tamanhoDaPagina);

    public Task<ResultadoPaginado<ClientesResumo>> PesquisarClientes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _clientesRepository.PesquisarClientes(termo, pagina, tamanhoDaPagina);
}
