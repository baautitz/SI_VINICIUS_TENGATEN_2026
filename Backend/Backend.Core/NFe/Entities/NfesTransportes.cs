using Backend.Core.Logistica.Entities;
using Backend.Core.NFe.Entities.Enums;

namespace Backend.Core.NFe.Entities;

public class NfesTransportes
{
  public int Id { get; set; }
  public ModalidadeFrete ModalidadeFrete { get; set; }
  public string? TransportadoraNomeRazaosocial { get; set; }
  public string? TransportadoraCpfCnpj { get; set; }
  public string? TransportadoraRgIe { get; set; }
  public string? VeiculoPlaca { get; set; }
  public string? VeiculoUf { get; set; }
  public string? VeiculoRntrc { get; set; }
  public int? QuantidadeVolumes { get; set; }
  public string? EspecieVolume { get; set; }
  public string? MarcaVolume { get; set; }
  public string? NumeracaoVolume { get; set; }
  public decimal? PesoBruto { get; set; }
  public decimal? PesoLiquido { get; set; }

  public Veiculos? Veiculo { get; set; }
}