namespace Modules.Parceiros.Models;

public class Clientes
{
  public int Id { get; set; }
  public required string NomeRazaoSocial { get; set; }
  public required string CpfCnpj { get; set; }
  public string? RgIe { get; set; }
  public string? ApelidoNomeFantasia { get; set; }
  public string? Endereco { get; set; }
  public string? Bairro { get; set; }
  public string? Telefone { get; set; }
  public string? Email { get; set; }
  public decimal LimiteCredito { get; set; }
  public bool Ativo { get; set; }
  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }
  public string? Observacao { get; set; }
}