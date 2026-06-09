using Backend.Core.Common.Enums;
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
    public string CpfCnpj { get; private set; }
    public string? RgIe { get; private set; }
    public string? ApelidoNomefantasia { get; private set; }
    public string? Endereco { get; private set; }
    public Bairros? Bairro { get; private set; }
    public Paises Nacionalidade { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
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
        string cpfCnpj,
        Paises nacionalidade,
        string? rgIe = null,
        string? apelidoNomefantasia = null,
        string? endereco = null,
        Bairros? bairro = null,
        string? telefone = null,
        string? email = null,
        string? observacao = null)
    {
        nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);
        var documentoLimpo = new DocumentoGenerico(cpfCnpj).Valor;

        if (string.IsNullOrWhiteSpace(nomeRazaosocial))
            throw new DomainException("Nome/razão social é obrigatório.");

        if (string.IsNullOrWhiteSpace(documentoLimpo))
            throw new DomainException("CPF/CNPJ ou Documento é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade é obrigatória.");

        TipoPessoa = tipoPessoa;
        NomeRazaosocial = nomeRazaosocial;
        CpfCnpj = documentoLimpo;
        Nacionalidade = nacionalidade;
        RgIe = new DocumentoGenerico(rgIe ?? "").Valor;
        if (string.IsNullOrWhiteSpace(RgIe)) RgIe = null;
        ApelidoNomefantasia = TextNormalization.NormalizeOrNull(apelidoNomefantasia);
        Endereco = TextNormalization.NormalizeOrNull(endereco);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public Fornecedores(int id, TipoPessoa tipoPessoa, string nomeRazaosocial, string cpfCnpj, Paises nacionalidade, string? rgIe = null, string? apelidoNomefantasia = null, string? endereco = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? observacao = null, bool ativo = true, DateTime? criadoEm = null)
        : this(tipoPessoa, nomeRazaosocial, cpfCnpj, nacionalidade, rgIe, apelidoNomefantasia, endereco, bairro, telefone, email, observacao)
    {
        Id = id;
        Ativo = ativo;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
    }

    public void Atualizar(TipoPessoa tipoPessoa, string nomeRazaosocial, string cpfCnpj, Paises nacionalidade, string? rgIe = null, string? apelidoNomefantasia = null, string? endereco = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? observacao = null)
    {
        nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);
        var documentoLimpo = new DocumentoGenerico(cpfCnpj).Valor;

        if (string.IsNullOrWhiteSpace(nomeRazaosocial))
            throw new DomainException("Nome/razão social é obrigatório.");

        if (string.IsNullOrWhiteSpace(documentoLimpo))
            throw new DomainException("CPF/CNPJ ou Documento é obrigatório.");

        if (nacionalidade == null)
            throw new DomainException("Nacionalidade é obrigatória.");

        TipoPessoa = tipoPessoa;
        NomeRazaosocial = nomeRazaosocial;
        CpfCnpj = documentoLimpo;
        Nacionalidade = nacionalidade;
        RgIe = new DocumentoGenerico(rgIe ?? "").Valor;
        if (string.IsNullOrWhiteSpace(RgIe)) RgIe = null;
        ApelidoNomefantasia = TextNormalization.NormalizeOrNull(apelidoNomefantasia);
        Endereco = TextNormalization.NormalizeOrNull(endereco);
        Bairro = bairro;
        Telefone = TextNormalization.NormalizeOrNull(telefone);
        Email = TextNormalization.NormalizeOrNull(email);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
