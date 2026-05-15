namespace backend.Modules.Logistica.Models;

public class Veiculos
{
  public int Id { get; set; }
  public required string Placa { get; set; }
  public required string Uf { get; set; }
  public string? Rntrc { get; set; }
  public string? Renavam { get; set; }
  public string? TipoVeiculo { get; set; }
  public string? MarcaModelo { get; set; }
  public bool Ativo { get; set; }
  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }
  public string? Observacao { get; set; }
}