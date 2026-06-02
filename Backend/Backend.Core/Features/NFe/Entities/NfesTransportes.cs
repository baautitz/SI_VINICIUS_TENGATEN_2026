using Backend.Core.Common;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Logistica.Entities;
using Backend.Core.Features.NFe.Entities.Enums;

namespace Backend.Core.Features.NFe.Entities;

public class NfesTransportes
{
    public int Id { get; set; }
    public ModalidadeFrete ModalidadeFrete { get; private set; }
    public string? TransportadoraNomeRazaosocial { get; private set; }
    public string? TransportadoraCpfCnpj { get; private set; }
    public string? TransportadoraRgIe { get; private set; }
    public string? VeiculoPlaca { get; private set; }
    public string? VeiculoUf { get; private set; }
    public string? VeiculoRntrc { get; private set; }
    public int? QuantidadeVolumes { get; private set; }
    public string? EspecieVolume { get; private set; }
    public string? MarcaVolume { get; private set; }
    public string? NumeracaoVolume { get; private set; }
    public decimal? PesoBruto { get; private set; }
    public decimal? PesoLiquido { get; private set; }
    public Veiculos? Veiculo { get; private set; }

    public NfesTransportes(
        ModalidadeFrete modalidadeFrete,
        string? transportadoraNomeRazaosocial = null,
        string? transportadoraCpfCnpj = null,
        string? transportadoraRgIe = null,
        string? veiculoPlaca = null,
        string? veiculoUf = null,
        string? veiculoRntrc = null,
        int? quantidadeVolumes = null,
        string? especieVolume = null,
        string? marcaVolume = null,
        string? numeracaoVolume = null,
        decimal? pesoBruto = null,
        decimal? pesoLiquido = null,
        Veiculos? veiculo = null)
    {
        TransportadoraNomeRazaosocial = TextNormalization.NormalizeOrNull(transportadoraNomeRazaosocial);
        TransportadoraCpfCnpj = string.IsNullOrWhiteSpace(transportadoraCpfCnpj) ? null : new CpfCnpj(transportadoraCpfCnpj).Valor;
        TransportadoraRgIe = string.IsNullOrWhiteSpace(transportadoraRgIe) ? null : new DocumentoGenerico(transportadoraRgIe).Valor;
        VeiculoPlaca = TextNormalization.NormalizeOrNull(veiculoPlaca);
        VeiculoUf = TextNormalization.NormalizeOrNull(veiculoUf);
        VeiculoRntrc = string.IsNullOrWhiteSpace(veiculoRntrc) ? null : new DocumentoGenerico(veiculoRntrc).Valor;
        QuantidadeVolumes = quantidadeVolumes;
        EspecieVolume = TextNormalization.NormalizeOrNull(especieVolume);
        MarcaVolume = TextNormalization.NormalizeOrNull(marcaVolume);
        NumeracaoVolume = TextNormalization.NormalizeOrNull(numeracaoVolume);
        PesoBruto = pesoBruto;
        PesoLiquido = pesoLiquido;
        Veiculo = veiculo;
        ModalidadeFrete = modalidadeFrete;

        if (modalidadeFrete != ModalidadeFrete.SEM_TRANSPORTE)
        {
            if (string.IsNullOrWhiteSpace(TransportadoraNomeRazaosocial) || string.IsNullOrWhiteSpace(TransportadoraCpfCnpj))
                throw new DomainException("Transportadora é obrigatória para modalidade de frete.");
        }

        if (!string.IsNullOrWhiteSpace(VeiculoPlaca) && string.IsNullOrWhiteSpace(VeiculoUf))
            throw new DomainException("UF do veículo é obrigatória quando placa for informada.");

        if (PesoBruto < 0)
            throw new DomainException("Peso bruto não pode ser negativo.");

        if (PesoLiquido < 0)
            throw new DomainException("Peso líquido não pode ser negativo.");
    }
}
