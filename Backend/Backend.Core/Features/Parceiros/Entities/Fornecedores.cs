using Backend.Core.Common;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Parceiros.Entities;

public class Fornecedores
{
    public int Id { get; set; }
    public string NomeRazaosocial { get; private set; }
    public string CpfCnpj { get; private set; }
    public string? RgIe { get; private set; }
    public string? ApelidoNomefantasia { get; private set; }
    public string? Endereco { get; private set; }
    public Bairros? Bairro { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Observacao { get; private set; }

    public Fornecedores(string nomeRazaosocial, string cpfCnpj, string? rgIe = null, string? apelidoNomefantasia = null, string? endereco = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? observacao = null)
    {
        nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);
        var cpfCnpjVo = new CpfCnpj(cpfCnpj);
        rgIe = new DocumentoGenerico(rgIe ?? "").Valor;
        if (string.IsNullOrWhiteSpace(rgIe)) rgIe = null;
        apelidoNomefantasia = TextNormalization.NormalizeOrNull(apelidoNomefantasia);
        endereco = TextNormalization.NormalizeOrNull(endereco);
        telefone = TextNormalization.NormalizeOrNull(telefone);
        email = TextNormalization.NormalizeOrNull(email);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(nomeRazaosocial))
            throw new DomainException("Nome/razão social é obrigatório.");

        if (string.IsNullOrWhiteSpace(cpfCnpjVo))
            throw new DomainException("CPF/CNPJ é obrigatório.");

        NomeRazaosocial = nomeRazaosocial;
        CpfCnpj = cpfCnpjVo;
        RgIe = rgIe;
        ApelidoNomefantasia = apelidoNomefantasia;
        Endereco = endereco;
        Bairro = bairro;
        Telefone = telefone;
        Email = email;
        Observacao = observacao;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public Fornecedores(int id, string nomeRazaosocial, string cpfCnpj, string? rgIe = null, string? apelidoNomefantasia = null, string? endereco = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? observacao = null, bool ativo = true, DateTime? criadoEm = null)
        : this(nomeRazaosocial, cpfCnpj, rgIe, apelidoNomefantasia, endereco, bairro, telefone, email, observacao)
    {
        Id = id;
        Ativo = ativo;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
    }

    public void Atualizar(string nomeRazaosocial, string cpfCnpj, string? rgIe = null, string? apelidoNomefantasia = null, string? endereco = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? observacao = null)
    {
        nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);
        var cpfCnpjVo = new CpfCnpj(cpfCnpj);
        rgIe = new DocumentoGenerico(rgIe ?? "").Valor;
        if (string.IsNullOrWhiteSpace(rgIe)) rgIe = null;
        apelidoNomefantasia = TextNormalization.NormalizeOrNull(apelidoNomefantasia);
        endereco = TextNormalization.NormalizeOrNull(endereco);
        telefone = TextNormalization.NormalizeOrNull(telefone);
        email = TextNormalization.NormalizeOrNull(email);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(nomeRazaosocial))
            throw new DomainException("Nome/razão social é obrigatório.");

        if (string.IsNullOrWhiteSpace(cpfCnpjVo))
            throw new DomainException("CPF/CNPJ é obrigatório.");

        NomeRazaosocial = nomeRazaosocial;
        CpfCnpj = cpfCnpjVo;
        RgIe = rgIe;
        ApelidoNomefantasia = apelidoNomefantasia;
        Endereco = endereco;
        Bairro = bairro;
        Telefone = telefone;
        Email = email;
        Observacao = observacao;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
