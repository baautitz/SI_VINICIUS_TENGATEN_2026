using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Parceiros.Entities;

public class Veiculos
{
    public int Id { get; set; }
    public Transportadoras? Transportadora { get; private set; }
    public Estados Estado { get; private set; } = null!;
    public string Placa { get; private set; }
    public string? Rntrc { get; private set; }
    public string? Renavam { get; private set; }
    public string? TipoVeiculo { get; private set; }
    public string? MarcaModelo { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public string? Observacao { get; private set; }

    protected Veiculos()
    {
        Placa = null!;
    }

    public Veiculos(string placa, Estados estado, Transportadoras? transportadora = null, string? rntrc = null, string? renavam = null, string? tipoVeiculo = null, string? marcaModelo = null, string? observacao = null)
    {
        placa = TextNormalization.Normalize(placa);
        rntrc = TextNormalization.NormalizeOrNull(rntrc);
        renavam = TextNormalization.NormalizeOrNull(renavam);
        tipoVeiculo = TextNormalization.NormalizeOrNull(tipoVeiculo);
        marcaModelo = TextNormalization.NormalizeOrNull(marcaModelo);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(placa))
            throw new DomainException("Placa é obrigatória.");

        Estado = estado ?? throw new DomainException("Estado é obrigatório.");

        Placa = placa;
        Transportadora = transportadora;
        Rntrc = rntrc;
        Renavam = renavam;
        TipoVeiculo = tipoVeiculo;
        MarcaModelo = marcaModelo;
        Observacao = observacao;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(string placa, Estados estado, Transportadoras? transportadora = null, string? rntrc = null, string? renavam = null, string? tipoVeiculo = null, string? marcaModelo = null, string? observacao = null)
    {
        placa = TextNormalization.Normalize(placa);
        rntrc = TextNormalization.NormalizeOrNull(rntrc);
        renavam = TextNormalization.NormalizeOrNull(renavam);
        tipoVeiculo = TextNormalization.NormalizeOrNull(tipoVeiculo);
        marcaModelo = TextNormalization.NormalizeOrNull(marcaModelo);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(placa))
            throw new DomainException("Placa é obrigatória.");

        Estado = estado ?? throw new DomainException("Estado é obrigatório.");

        Placa = placa;
        Transportadora = transportadora;
        Rntrc = rntrc;
        Renavam = renavam;
        TipoVeiculo = tipoVeiculo;
        MarcaModelo = marcaModelo;
        Observacao = observacao;
    }

    public void VincularTransportadora(Transportadoras transportadora)
    {
        Transportadora = transportadora;
    }

    public void VincularEstado(Estados estado)
    {
        Estado = estado;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
