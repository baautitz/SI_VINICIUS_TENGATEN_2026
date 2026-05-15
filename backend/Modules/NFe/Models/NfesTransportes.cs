using backend.Modules.Logistica.Models;

namespace backend.Modules.NFe.Models;

public class NfesTransportes
{
  public int Id { get; set; }
  public Enums.ModalidadeFrete ModalidadeFrete { get; set; }
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