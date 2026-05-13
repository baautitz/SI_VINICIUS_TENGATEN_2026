namespace Modules.Logistica.Models;

public class Veiculos
{
  public int Id { get; set; }
  public int? TransportadoraId { get; set; }
  public string Placa { get; set; } = null!;
  public string Uf { get; set; } = null!;
  public string? Rntrc { get; set; }
  public string? Renavam { get; set; }
  public string? TipoVeiculo { get; set; }
  public string? MarcaModelo { get; set; }
  public bool Ativo { get; set; }
  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }
  public string? Observacao { get; set; }

  public Transportadoras? Transportadora { get; set; }
}