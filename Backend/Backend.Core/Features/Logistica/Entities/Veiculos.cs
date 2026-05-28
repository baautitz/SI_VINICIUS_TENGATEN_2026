using Backend.Core.Common;

namespace Backend.Core.Features.Logistica.Entities;

public class Veiculos
{
    public int Id { get; private set; }
    public string Placa { get; private set; }
    public string Uf { get; private set; }
    public string? Rntrc { get; private set; }
    public string? Renavam { get; private set; }
    public string? TipoVeiculo { get; private set; }
    public string? MarcaModelo { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Observacao { get; private set; }

    public Veiculos(string placa, string uf, string? rntrc = null, string? renavam = null, string? tipoVeiculo = null, string? marcaModelo = null, string? observacao = null)
    {
        placa = TextNormalization.Normalize(placa);
        uf = TextNormalization.Normalize(uf);
        rntrc = TextNormalization.NormalizeOrNull(rntrc);
        renavam = TextNormalization.NormalizeOrNull(renavam);
        tipoVeiculo = TextNormalization.NormalizeOrNull(tipoVeiculo);
        marcaModelo = TextNormalization.NormalizeOrNull(marcaModelo);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(placa))
            throw new DomainException("Placa é obrigatória.");

        if (string.IsNullOrWhiteSpace(uf))
            throw new DomainException("UF é obrigatória.");

        Placa = placa;
        Uf = uf;
        Rntrc = rntrc;
        Renavam = renavam;
        TipoVeiculo = tipoVeiculo;
        MarcaModelo = marcaModelo;
        Observacao = observacao;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(string placa, string uf, string? rntrc = null, string? renavam = null, string? tipoVeiculo = null, string? marcaModelo = null, string? observacao = null)
    {
        placa = TextNormalization.Normalize(placa);
        uf = TextNormalization.Normalize(uf);
        rntrc = TextNormalization.NormalizeOrNull(rntrc);
        renavam = TextNormalization.NormalizeOrNull(renavam);
        tipoVeiculo = TextNormalization.NormalizeOrNull(tipoVeiculo);
        marcaModelo = TextNormalization.NormalizeOrNull(marcaModelo);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(placa))
            throw new DomainException("Placa é obrigatória.");

        if (string.IsNullOrWhiteSpace(uf))
            throw new DomainException("UF é obrigatória.");

        Placa = placa;
        Uf = uf;
        Rntrc = rntrc;
        Renavam = renavam;
        TipoVeiculo = tipoVeiculo;
        MarcaModelo = marcaModelo;
        Observacao = observacao;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
