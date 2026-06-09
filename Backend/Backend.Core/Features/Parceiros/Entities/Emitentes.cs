using Backend.Core.Common.Enums;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Parceiros.Entities;

public class Emitentes
{
    public int Id { get; set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public string NomeRazaoSocial { get; private set; }
    public string CpfCnpj { get; private set; }
    public string? ApelidoNomeFantasia { get; private set; }
    public string? Endereco { get; private set; }
    public Bairros? Bairro { get; private set; }
    public Paises Nacionalidade { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public string? RgIe { get; private set; }
    public string? InscricaoMunicipal { get; private set; }
    public string? RegimeTributario { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Observacao { get; private set; }


    protected Emitentes()
    {
        NomeRazaoSocial = null!;
        CpfCnpj = null!;
        Nacionalidade = null!;
    }

    public Emitentes(
        TipoPessoa tipoPessoa,
        string nomeRazaoSocial,
        string cpfCnpj,
        Paises nacionalidade,
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
        var documentoLimpo = new DocumentoGenerico(cpfCnpj).Valor;

        if (string.IsNullOrWhiteSpace(nomeRazaoSocial))
            throw new DomainException("Nome ou razão social do emitente é obrigatório.");

        if (string.IsNullOrWhiteSpace(documentoLimpo))
            throw new DomainException("CPF/CNPJ ou Documento do emitente é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade do emitente é obrigatória.");

        TipoPessoa = tipoPessoa;
        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = documentoLimpo;
        Nacionalidade = nacionalidade;
        ApelidoNomeFantasia = TextNormalization.NormalizeOrNull(apelidoNomeFantasia);
        Endereco = TextNormalization.NormalizeOrNull(endereco);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        RgIe = new DocumentoGenerico(rgIe ?? "").Valor;
        if (string.IsNullOrWhiteSpace(RgIe)) RgIe = null;
        InscricaoMunicipal = TextNormalization.NormalizeOrNull(inscricaoMunicipal);
        RegimeTributario = TextNormalization.NormalizeOrNull(regimeTributario);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Ativo = ativo;
        CriadoEm = DateTime.UtcNow;
    }

    public Emitentes(int id,
        TipoPessoa tipoPessoa,
        string nomeRazaoSocial,
        string cpfCnpj,
        Paises nacionalidade,
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
        : this(tipoPessoa, nomeRazaoSocial, cpfCnpj, nacionalidade, apelidoNomeFantasia, endereco, bairro, telefone, email, rgIe, inscricaoMunicipal, regimeTributario, observacao, ativo)
    {
        Id = id;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
    }

    public void AtualizarDados(
        TipoPessoa tipoPessoa,
        string nomeRazaoSocial,
        string cpfCnpj,
        Paises nacionalidade,
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
        var documentoLimpo = new DocumentoGenerico(cpfCnpj).Valor;

        if (string.IsNullOrWhiteSpace(nomeRazaoSocial))
            throw new DomainException("Nome ou razão social do emitente é obrigatório.");

        if (string.IsNullOrWhiteSpace(documentoLimpo))
            throw new DomainException("CPF/CNPJ ou Documento do emitente é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade do emitente é obrigatória.");

        TipoPessoa = tipoPessoa;
        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = documentoLimpo;
        Nacionalidade = nacionalidade;
        ApelidoNomeFantasia = TextNormalization.NormalizeOrNull(apelidoNomeFantasia);
        Endereco = TextNormalization.NormalizeOrNull(endereco);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        RgIe = new DocumentoGenerico(rgIe ?? "").Valor;
        if (string.IsNullOrWhiteSpace(RgIe)) RgIe = null;
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
