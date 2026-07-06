using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Parceiros.Entities;

public class Clientes
{
    public int Id { get; set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public string NomeRazaoSocial { get; private set; }
    public Documento CpfCnpj { get; private set; }
    public Documento? RgIe { get; private set; }
    public string? ApelidoNomeFantasia { get; private set; }
    public string? Logradouro { get; private set; }
    public string? Numero { get; private set; }
    public Bairros? Bairro { get; private set; }
    public Paises Nacionalidade { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public decimal LimiteCredito { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Sexo { get; private set; }
    public DateTime? DataNascimento { get; private set; }
    public string? Observacao { get; private set; }


    protected Clientes()
    {
        NomeRazaoSocial = null!;
        CpfCnpj = null!;
        Nacionalidade = null!;
    }

    public Clientes(
        TipoPessoa tipoPessoa,
        string nomeRazaoSocial,
        Documento cpfCnpj,
        Paises nacionalidade,
        Documento? rgIe = null,
        string? apelidoNomeFantasia = null,
        string? logradouro = null,
        string? numero = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        decimal limiteCredito = 0m,
        string? observacao = null,
        bool ativo = true,
        string? sexo = null,
        DateTime? dataNascimento = null)
    {
        nomeRazaoSocial = TextNormalization.Normalize(nomeRazaoSocial);

        if (string.IsNullOrWhiteSpace(nomeRazaoSocial))
            throw new DomainException("Nome ou razão social do cliente é obrigatório.");

        if (cpfCnpj == null || string.IsNullOrWhiteSpace(cpfCnpj.Valor))
            throw new DomainException("CPF/CNPJ ou Documento do cliente é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade do cliente é obrigatória.");

        if (limiteCredito < 0)
            throw new DomainException("Limite de crédito não pode ser negativo.");

        if (tipoPessoa == TipoPessoa.JURIDICA && !string.IsNullOrWhiteSpace(sexo))
            throw new DomainException("Sexo só pode ser informado para pessoa física.");

        if (!string.IsNullOrWhiteSpace(sexo) && sexo != "M" && sexo != "F" && sexo != "O")
            throw new DomainException("Sexo inválido.");

        TipoPessoa = tipoPessoa;
        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = cpfCnpj;
        Nacionalidade = nacionalidade;
        RgIe = rgIe;
        ApelidoNomeFantasia = TextNormalization.NormalizeOrNull(apelidoNomeFantasia);
        Logradouro = TextNormalization.NormalizeOrNull(logradouro);
        Numero = TextNormalization.NormalizeOrNull(numero);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        LimiteCredito = limiteCredito;
        Ativo = ativo;
        CriadoEm = DateTime.UtcNow;
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Sexo = tipoPessoa == TipoPessoa.FISICA ? TextNormalization.NormalizeOrNull(sexo)?.ToUpper() : null;
        DataNascimento = dataNascimento;
    }

    public Clientes(int id,
        TipoPessoa tipoPessoa,
        string nomeRazaoSocial,
        Documento cpfCnpj,
        Paises nacionalidade,
        Documento? rgIe = null,
        string? apelidoNomeFantasia = null,
        string? logradouro = null,
        string? numero = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        decimal limiteCredito = 0m,
        string? observacao = null,
        bool ativo = true,
        string? sexo = null,
        DateTime? dataNascimento = null,
        DateTime? criadoEm = null)
        : this(tipoPessoa, nomeRazaoSocial, cpfCnpj, nacionalidade, rgIe, apelidoNomeFantasia, logradouro, numero, bairro, telefone, email, limiteCredito, observacao, ativo, sexo, dataNascimento)
    {
        Id = id;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
    }

    public void AtualizarDados(
        TipoPessoa tipoPessoa,
        string nomeRazaoSocial,
        Documento cpfCnpj,
        Paises nacionalidade,
        Documento? rgIe = null,
        string? apelidoNomeFantasia = null,
        string? logradouro = null,
        string? numero = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        decimal limiteCredito = 0m,
        string? observacao = null,
        string? sexo = null,
        DateTime? dataNascimento = null)
    {
        nomeRazaoSocial = TextNormalization.Normalize(nomeRazaoSocial);

        if (string.IsNullOrWhiteSpace(nomeRazaoSocial))
            throw new DomainException("Nome ou razão social do cliente é obrigatório.");

        if (cpfCnpj == null || string.IsNullOrWhiteSpace(cpfCnpj.Valor))
            throw new DomainException("CPF/CNPJ ou Documento do cliente é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade do cliente é obrigatória.");

        if (limiteCredito < 0)
            throw new DomainException("Limite de crédito não pode ser negativo.");

        if (tipoPessoa == TipoPessoa.JURIDICA && !string.IsNullOrWhiteSpace(sexo))
            throw new DomainException("Sexo só pode ser informado para pessoa física.");

        if (!string.IsNullOrWhiteSpace(sexo) && sexo != "M" && sexo != "F" && sexo != "O")
            throw new DomainException("Sexo inválido.");

        TipoPessoa = tipoPessoa;
        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = cpfCnpj;
        Nacionalidade = nacionalidade;
        RgIe = rgIe;
        ApelidoNomeFantasia = TextNormalization.NormalizeOrNull(apelidoNomeFantasia);
        Logradouro = TextNormalization.NormalizeOrNull(logradouro);
        Numero = TextNormalization.NormalizeOrNull(numero);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        LimiteCredito = limiteCredito;
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Sexo = tipoPessoa == TipoPessoa.FISICA ? TextNormalization.NormalizeOrNull(sexo)?.ToUpper() : null;
        DataNascimento = dataNascimento;
    }

    public void DefinirBairro(Bairros? bairro)
    {
        Bairro = bairro;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}

