namespace Modules.NFe.Models;

public class NfesInformacoesAdicionais
{
  public int Id { get; set; }
  public int NfeId { get; set; }
  public string? InformacoesFisco { get; set; }
  public string? InformacoesComplementares { get; set; }

  public required Nfes Nfe { get; set; }
}