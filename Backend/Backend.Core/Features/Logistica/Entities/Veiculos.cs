using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Logistica.Entities;

public class Veiculos
{
    public int Id { get; set; }
    public int? TransportadoraId { get; private set; }
    public Transportadoras? Transportadora { get; private set; }
    public int EstadoId { get; private set; }
    public Estados? Estado { get; private set; }
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

    public Veiculos(string placa, int estadoId, int? transportadoraId = null, string? rntrc = null, string? renavam = null, string? tipoVeiculo = null, string? marcaModelo = null, string? observacao = null)
    {
        placa = TextNormalization.Normalize(placa);
        rntrc = TextNormalization.NormalizeOrNull(rntrc);
        renavam = TextNormalization.NormalizeOrNull(renavam);
        tipoVeiculo = TextNormalization.NormalizeOrNull(tipoVeiculo);
        marcaModelo = TextNormalization.NormalizeOrNull(marcaModelo);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(placa))
            throw new DomainException("Placa é obrigatória.");

        if (estadoId <= 0)
            throw new DomainException("Estado é obrigatório.");

        Placa = placa;
        EstadoId = estadoId;
        TransportadoraId = transportadoraId;
        Rntrc = rntrc;
        Renavam = renavam;
        TipoVeiculo = tipoVeiculo;
        MarcaModelo = marcaModelo;
        Observacao = observacao;
        Ativo = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(string placa, int estadoId, int? transportadoraId = null, string? rntrc = null, string? renavam = null, string? tipoVeiculo = null, string? marcaModelo = null, string? observacao = null)
    {
        placa = TextNormalization.Normalize(placa);
        rntrc = TextNormalization.NormalizeOrNull(rntrc);
        renavam = TextNormalization.NormalizeOrNull(renavam);
        tipoVeiculo = TextNormalization.NormalizeOrNull(tipoVeiculo);
        marcaModelo = TextNormalization.NormalizeOrNull(marcaModelo);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(placa))
            throw new DomainException("Placa é obrigatória.");

        if (estadoId <= 0)
            throw new DomainException("Estado é obrigatório.");

        Placa = placa;
        EstadoId = estadoId;
        TransportadoraId = transportadoraId;
        Rntrc = rntrc;
        Renavam = renavam;
        TipoVeiculo = tipoVeiculo;
        MarcaModelo = marcaModelo;
        Observacao = observacao;
    }

    public void VincularTransportadora(Transportadoras transportadora)
    {
        Transportadora = transportadora;
        TransportadoraId = transportadora.Id;
    }

    public void VincularEstado(Estados estado)
    {
        Estado = estado;
        EstadoId = estado.Id;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
