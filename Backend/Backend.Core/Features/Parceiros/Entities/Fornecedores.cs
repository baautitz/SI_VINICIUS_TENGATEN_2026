using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Parceiros.Entities;

public class Fornecedores
{
    public int Id { get; set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public string NomeRazaosocial { get; private set; }
    public Documento CpfCnpj { get; private set; }
    public Documento? RgIe { get; private set; }
    public string? ApelidoNomefantasia { get; private set; }
    public string? Logradouro { get; private set; }
    public string? Numero { get; private set; }
    public Bairros? Bairro { get; private set; }
    public Paises Nacionalidade { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Sexo { get; private set; }
    public DateTime? DataNascimento { get; private set; }
    public string? Observacao { get; private set; }


    protected Fornecedores()
    {
        NomeRazaosocial = null!;
        CpfCnpj = null!;
        Nacionalidade = null!;
    }

    public Fornecedores(
        TipoPessoa tipoPessoa,
        string nomeRazaosocial,
        Documento cpfCnpj,
        Paises nacionalidade,
        Documento? rgIe = null,
        string? apelidoNomefantasia = null,
        string? logradouro = null,
        string? numero = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        string? observacao = null,
        string? sexo = null,
        DateTime? dataNascimento = null)
    {
        nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);

        if (string.IsNullOrWhiteSpace(nomeRazaosocial))
            throw new DomainException("Nome/razão social é obrigatório.");

        if (cpfCnpj == null || string.IsNullOrWhiteSpace(cpfCnpj.Valor))
            throw new DomainException("CPF/CNPJ ou Documento é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade é obrigatória.");

        if (tipoPessoa == TipoPessoa.JURIDICA && !string.IsNullOrWhiteSpace(sexo))
            throw new DomainException("Sexo só pode ser informado para pessoa física.");

        if (!string.IsNullOrWhiteSpace(sexo) && sexo != "M" && sexo != "F" && sexo != "O")
            throw new DomainException("Sexo inválido.");

        TipoPessoa = tipoPessoa;
        NomeRazaosocial = nomeRazaosocial;
        CpfCnpj = cpfCnpj;
        Nacionalidade = nacionalidade;
        RgIe = rgIe;
        ApelidoNomefantasia = TextNormalization.NormalizeOrNull(apelidoNomefantasia);
        Logradouro = TextNormalization.NormalizeOrNull(logradouro);
        Numero = TextNormalization.NormalizeOrNull(numero);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
        Sexo = tipoPessoa == TipoPessoa.FISICA ? TextNormalization.NormalizeOrNull(sexo)?.ToUpper() : null;
        DataNascimento = dataNascimento;
    }

    public Fornecedores(int id, TipoPessoa tipoPessoa, string nomeRazaosocial, Documento cpfCnpj, Paises nacionalidade, Documento? rgIe = null, string? apelidoNomefantasia = null, string? logradouro = null, string? numero = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? observacao = null, bool ativo = true, DateTime? criadoEm = null, string? sexo = null, DateTime? dataNascimento = null)
        : this(tipoPessoa, nomeRazaosocial, cpfCnpj, nacionalidade, rgIe, apelidoNomefantasia, logradouro, numero, bairro, telefone, email, observacao, sexo, dataNascimento)
    {
        Id = id;
        Ativo = ativo;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
    }

    public void Atualizar(TipoPessoa tipoPessoa, string nomeRazaosocial, Documento cpfCnpj, Paises nacionalidade, Documento? rgIe = null, string? apelidoNomefantasia = null, string? logradouro = null, string? numero = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? observacao = null, string? sexo = null, DateTime? dataNascimento = null)
    {
        nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);

        if (string.IsNullOrWhiteSpace(nomeRazaosocial))
            throw new DomainException("Nome/razão social é obrigatório.");

        if (cpfCnpj == null || string.IsNullOrWhiteSpace(cpfCnpj.Valor))
            throw new DomainException("CPF/CNPJ ou Documento é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade é obrigatória.");

        if (tipoPessoa == TipoPessoa.JURIDICA && !string.IsNullOrWhiteSpace(sexo))
            throw new DomainException("Sexo só pode ser informado para pessoa física.");

        if (!string.IsNullOrWhiteSpace(sexo) && sexo != "M" && sexo != "F" && sexo != "O")
            throw new DomainException("Sexo inválido.");

        TipoPessoa = tipoPessoa;
        NomeRazaosocial = nomeRazaosocial;
        CpfCnpj = cpfCnpj;
        Nacionalidade = nacionalidade;
        RgIe = rgIe;
        ApelidoNomefantasia = TextNormalization.NormalizeOrNull(apelidoNomefantasia);
        Logradouro = TextNormalization.NormalizeOrNull(logradouro);
        Numero = TextNormalization.NormalizeOrNull(numero);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Sexo = tipoPessoa == TipoPessoa.FISICA ? TextNormalization.NormalizeOrNull(sexo)?.ToUpper() : null;
        DataNascimento = dataNascimento;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}

