namespace Modules.Parceiros.Entities;

public class Emitentes
{
  public int Id { get; set; }
  public string NomeRazaoSocial { get; set; } = null!;
  public string CpfCnpj { get; set; } = null!;
  public string? ApelidoNomeFantasia { get; set; }
  public string? Endereco { get; set; }
  public string? Bairro { get; set; }
  public string? Telefone { get; set; }
  public string? Email { get; set; }
  public string? RgIe { get; set; }
  public string? InscricaoMunicipal { get; set; }
  public string? RegimeTributario { get; set; }
  public bool Ativo { get; set; }
  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }
  public string? Observacao { get; set; }
}