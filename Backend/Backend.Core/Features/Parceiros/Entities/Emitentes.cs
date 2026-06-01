using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Parceiros.Entities;

public class Emitentes
{
    public int Id { get; set; }
    public string NomeRazaoSocial { get; private set; }
    public string CpfCnpj { get; private set; }
    public string? ApelidoNomeFantasia { get; private set; }
    public string? Endereco { get; private set; }
    public Bairros? Bairro { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public string? RgIe { get; private set; }
    public string? InscricaoMunicipal { get; private set; }
    public string? RegimeTributario { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Observacao { get; private set; }

    // Required by Dapper
    protected Emitentes() 
    {
        NomeRazaoSocial = null!;
        CpfCnpj = null!;
    }

    public Emitentes(
        string nomeRazaoSocial,
        string cpfCnpj,
        string? apelidoNomeFantasia = null,
        string? endereco = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        string? rgIe = null,
        string? inscricaoMunicipal = null,
        string? regimeTributario = null,
        string? observacao = null,
        bool ativo = true)
    {
        nomeRazaoSocial = TextNormalization.Normalize(nomeRazaoSocial);
        cpfCnpj = TextNormalization.NormalizeDocument(cpfCnpj);

        if (string.IsNullOrWhiteSpace(nomeRazaoSocial))
            throw new DomainException("Nome ou razão social do emitente é obrigatório.");

        if (string.IsNullOrWhiteSpace(cpfCnpj))
            throw new DomainException("CPF/CNPJ do emitente é obrigatório.");

        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = cpfCnpj;
        ApelidoNomeFantasia = TextNormalization.NormalizeOrNull(apelidoNomeFantasia);
        Endereco = TextNormalization.NormalizeOrNull(endereco);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        RgIe = TextNormalization.NormalizeDocumentOrNull(rgIe);
        InscricaoMunicipal = TextNormalization.NormalizeOrNull(inscricaoMunicipal);
        RegimeTributario = TextNormalization.NormalizeOrNull(regimeTributario);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Ativo = ativo;
        CriadoEm = DateTime.UtcNow;
    }

    public Emitentes(int id,
        string nomeRazaoSocial,
        string cpfCnpj,
        string? apelidoNomeFantasia = null,
        string? endereco = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        string? rgIe = null,
        string? inscricaoMunicipal = null,
        string? regimeTributario = null,
        string? observacao = null,
        bool ativo = true,
        DateTime? criadoEm = null)
        : this(nomeRazaoSocial, cpfCnpj, apelidoNomeFantasia, endereco, bairro, telefone, email, rgIe, inscricaoMunicipal, regimeTributario, observacao, ativo)
    {
        Id = id;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
    }

    public void AtualizarDados(
        string nomeRazaoSocial,
        string cpfCnpj,
        string? apelidoNomeFantasia = null,
        string? endereco = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        string? rgIe = null,
        string? inscricaoMunicipal = null,
        string? regimeTributario = null,
        string? observacao = null)
    {
        nomeRazaoSocial = TextNormalization.Normalize(nomeRazaoSocial);
        cpfCnpj = TextNormalization.NormalizeDocument(cpfCnpj);

        if (string.IsNullOrWhiteSpace(nomeRazaoSocial))
            throw new DomainException("Nome ou razão social do emitente é obrigatório.");

        if (string.IsNullOrWhiteSpace(cpfCnpj))
            throw new DomainException("CPF/CNPJ do emitente é obrigatório.");

        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = cpfCnpj;
        ApelidoNomeFantasia = TextNormalization.NormalizeOrNull(apelidoNomeFantasia);
        Endereco = TextNormalization.NormalizeOrNull(endereco);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        RgIe = TextNormalization.NormalizeDocumentOrNull(rgIe);
        InscricaoMunicipal = TextNormalization.NormalizeOrNull(inscricaoMunicipal);
        RegimeTributario = TextNormalization.NormalizeOrNull(regimeTributario);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
    }

    public void DefinirBairro(Bairros? bairro)
    {
        Bairro = bairro;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
