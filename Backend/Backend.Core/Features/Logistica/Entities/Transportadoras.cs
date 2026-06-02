using Backend.Core.Common;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Logistica.Entities;

public class Transportadoras
{
    private readonly List<Veiculos> _veiculos = new();

    public int Id { get; set; }
    public string NomeRazaosocial { get; private set; }
    public string CpfCnpj { get; private set; }
    public string? RgIe { get; private set; }
    public string? ApelidoNomefantasia { get; private set; }
    public string? Endereco { get; private set; }
    public Bairros? Bairro { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public string? Rntrc { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Observacao { get; private set; }

    public IReadOnlyCollection<Veiculos> Veiculos => _veiculos.AsReadOnly();

    protected Transportadoras()
    {
        NomeRazaosocial = null!;
        CpfCnpj = null!;
    }
public Transportadoras(string nomeRazaosocial, string cpfCnpj, string? rgIe = null, string? apelidoNomefantasia = null, string? endereco = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? rntrc = null, string? observacao = null)
{
    nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);
    var cpfCnpjVo = new CpfCnpj(cpfCnpj);
    var rgIeVo = string.IsNullOrWhiteSpace(rgIe) ? null : new DocumentoGenerico(rgIe);
    var rntrcVo = string.IsNullOrWhiteSpace(rntrc) ? null : new DocumentoGenerico(rntrc);
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
    RgIe = rgIeVo?.Valor;
    ApelidoNomefantasia = apelidoNomefantasia;
    Endereco = endereco;
    Bairro = bairro;
    Telefone = telefone;
    Email = email;
    Rntrc = rntrcVo?.Valor;
    Observacao = observacao;
    Ativo = true;
    CriadoEm = DateTime.UtcNow;
}


    public void AdicionarVeiculo(Veiculos veiculo)
    {
        if (veiculo == null)
            throw new DomainException("Veículo é obrigatório.");

        if (_veiculos.Any(v => v.Placa == veiculo.Placa && v.EstadoId == veiculo.EstadoId))
            throw new DomainException("Já existe um veículo com esta placa e estado.");

        _veiculos.Add(veiculo);
    }

    public void RemoverVeiculo(Veiculos veiculo)
    {
        if (veiculo == null)
            throw new DomainException("Veículo é obrigatório.");

        _veiculos.Remove(veiculo);
    }
public void Atualizar(string nomeRazaosocial, string cpfCnpj, string? rgIe = null, string? apelidoNomefantasia = null, string? endereco = null, Bairros? bairro = null, string? telefone = null, string? email = null, string? rntrc = null, string? observacao = null)
{
    nomeRazaosocial = TextNormalization.Normalize(nomeRazaosocial);
    var cpfCnpjVo = new CpfCnpj(cpfCnpj);
    var rgIeVo = string.IsNullOrWhiteSpace(rgIe) ? null : new DocumentoGenerico(rgIe);
    var rntrcVo = string.IsNullOrWhiteSpace(rntrc) ? null : new DocumentoGenerico(rntrc);
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
    RgIe = rgIeVo?.Valor;
    ApelidoNomefantasia = apelidoNomefantasia;
    Endereco = endereco;
    Bairro = bairro;
    Telefone = telefone;
    Email = email;
    Rntrc = rntrcVo?.Valor;
    Observacao = observacao;
}


    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
