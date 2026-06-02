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

public sealed class ClientesService : BaseService
{
    private readonly IClientesRepository _clientesRepository;
    private readonly IBairrosRepository _bairrosRepository;
    private readonly IPaisesRepository _paisesRepository;

    public ClientesService(IClientesRepository clientesRepository, IBairrosRepository bairrosRepository, IPaisesRepository paisesRepository)
    {
        _clientesRepository = clientesRepository;
        _bairrosRepository = bairrosRepository;
        _paisesRepository = paisesRepository;
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

        var nacionalidade = await _paisesRepository.ObterPaisPorId(dto.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Clientes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (dto.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(dto.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(dto.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Clientes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(dto.CpfCnpj).Valor;

        if (await _clientesRepository.ExisteClienteCpfCnpj(documentoLimpo, dto.NacionalidadeId))
            return Resultado<Clientes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um cliente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            var cliente = new Clientes(
                dto.TipoPessoa,
                dto.NomeRazaoSocial,
                documentoLimpo,
                nacionalidade,
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

        var nacionalidade = await _paisesRepository.ObterPaisPorId(dto.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Clientes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.SiglaIso == "BRA")
        {
            if (dto.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(dto.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(dto.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (dto.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(dto.BairroId.Value);
            if (bairro is null)
                return Resultado<Clientes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(dto.CpfCnpj).Valor;

        if (await _clientesRepository.ExisteClienteCpfCnpj(documentoLimpo, dto.NacionalidadeId, id))
            return Resultado<Clientes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro cliente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            existente.AtualizarDados(
                dto.TipoPessoa,
                dto.NomeRazaoSocial,
                documentoLimpo,
                nacionalidade,
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
