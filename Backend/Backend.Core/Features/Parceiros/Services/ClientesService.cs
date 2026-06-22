using Backend.Core.Features.Parceiros.Enums;
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

    public async Task<Resultado<Clientes>> CriarCliente(CriarClienteCommand command)
    {
        var validator = new CriarClienteCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Clientes>.Falha(validation.ToResultadoErros());

        var nacionalidade = await _paisesRepository.ObterPaisPorId(command.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Clientes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.CodigoIsoPais != "BRA" && !string.IsNullOrWhiteSpace(command.RgIe))
            return Resultado<Clientes>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(command.RgIe)));

        if (nacionalidade.CodigoIsoPais == "BRA")
        {
            if (command.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(command.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(command.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (command.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(command.BairroId.Value);
            if (bairro is null)
                return Resultado<Clientes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(command.CpfCnpj).Valor;

        if (await _clientesRepository.ExisteClienteCpfCnpj(documentoLimpo, command.NacionalidadeId))
            return Resultado<Clientes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um cliente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            Documento documento = Documento.Criar(command.CpfCnpj, nacionalidade.CodigoIsoPais, command.TipoPessoa);
            Documento? rgIe = Documento.CriarGenerico(command.RgIe);

            var cliente = new Clientes(
                command.TipoPessoa,
                command.NomeRazaoSocial,
                documento,
                nacionalidade,
                rgIe,
                command.ApelidoNomeFantasia,
                command.Endereco,
                bairro,
                command.Telefone,
                command.Email,
                command.LimiteCredito,
                command.Observacao,
                command.Ativo
            );

            var criado = await _clientesRepository.CriarCliente(cliente);
            return Resultado<Clientes>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Clientes>> AtualizarCliente(int id, AtualizarClienteCommand command)
    {
        var validator = new AtualizarClienteCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Clientes>.Falha(validation.ToResultadoErros());

        var existente = await _clientesRepository.ObterClientePorId(id);
        if (existente is null)
            return Resultado<Clientes>.Falha(new ResultadoErro("CLIENTE_NAO_ENCONTRADO", "Cliente não encontrado."));

        var nacionalidade = await _paisesRepository.ObterPaisPorId(command.NacionalidadeId);
        if (nacionalidade is null)
            return Resultado<Clientes>.Falha(new ResultadoErro("NACIONALIDADE_NAO_ENCONTRADA", "Nacionalidade não encontrada.", "NacionalidadeId"));

        if (nacionalidade.CodigoIsoPais != "BRA" && !string.IsNullOrWhiteSpace(command.RgIe))
            return Resultado<Clientes>.Falha(new ResultadoErro("RG_IE_NAO_PERMITIDO", "RG/IE não é permitido para estrangeiros.", nameof(command.RgIe)));

        if (nacionalidade.CodigoIsoPais == "BRA")
        {
            if (command.TipoPessoa == TipoPessoa.FISICA)
            {
                if (!new Cpf(command.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CPF_INVALIDO", "CPF inválido para o Brasil.", "CpfCnpj"));
            }
            else
            {
                if (!new Cnpj(command.CpfCnpj).EhValido())
                    return Resultado<Clientes>.Falha(new ResultadoErro("CNPJ_INVALIDO", "CNPJ inválido para o Brasil.", "CpfCnpj"));
            }
        }

        Localizacao.Entities.Bairros? bairro = null;
        if (command.BairroId.HasValue)
        {
            bairro = await _bairrosRepository.ObterBairroPorId(command.BairroId.Value);
            if (bairro is null)
                return Resultado<Clientes>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "O bairro informado não foi encontrado.", "BairroId"));
        }

        var documentoLimpo = new DocumentoGenerico(command.CpfCnpj).Valor;

        if (await _clientesRepository.ExisteClienteCpfCnpj(documentoLimpo, command.NacionalidadeId, id))
            return Resultado<Clientes>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro cliente com este documento nesta nacionalidade.", "CpfCnpj"));

        return await ExecuteResultAsync(async () =>
        {
            Documento documento = Documento.Criar(command.CpfCnpj, nacionalidade.CodigoIsoPais, command.TipoPessoa);
            Documento? rgIe = Documento.CriarGenerico(command.RgIe);

            existente.AtualizarDados(
                command.TipoPessoa,
                command.NomeRazaoSocial,
                documento,
                nacionalidade,
                rgIe,
                command.ApelidoNomeFantasia,
                command.Endereco,
                bairro,
                command.Telefone,
                command.Email,
                command.LimiteCredito,
                command.Observacao
            );


            if (command.Ativo) existente.Ativar();
            else existente.Desativar();

            var atualizado = await _clientesRepository.AtualizarCliente(id, existente);
            return Resultado<Clientes>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarCliente(int id)
        => _clientesRepository.DeletarCliente(id);

    public Task<ResultadoPaginado<Clientes>> PesquisarClientes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => _clientesRepository.PesquisarClientes(termo, pagina, tamanhoDaPagina);
}

