namespace Modules.NFe.Entities;

public class Nfes
{
  public int Id { get; set; }
  public string? ChaveAcesso { get; set; }
  public int Numero { get; set; }
  public short Serie { get; set; }
  public DateTime DataEmissao { get; set; }
  public DateTime? DataSaida { get; set; }

  public int EmitenteId { get; set; }
  public string EmitenteNomeRazaosocial { get; set; } = null!;
  public string EmitenteCpfCnpj { get; set; } = null!;
  public string? EmitenteRgIe { get; set; }
  public string? EmitenteApelidoNomefantasia { get; set; }
  public string? EmitenteEndereco { get; set; }
  public string? EmitenteBairro { get; set; }
  public string? EmitenteTelefone { get; set; }
  public string? EmitenteEmail { get; set; }

  public int ClienteId { get; set; }
  public string ClienteNomeRazaosocial { get; set; } = null!;
  public string ClienteCpfCnpj { get; set; } = null!;
  public string? ClienteRgIe { get; set; }
  public string? ClienteApelidoNomefantasia { get; set; }
  public string? ClienteEndereco { get; set; }
  public string? ClienteBairro { get; set; }
  public string? ClienteTelefone { get; set; }
  public string? ClienteEmail { get; set; }

  public Enums.TipoOperacao TipoOperacao { get; set; }
  public Enums.StatusNfe StatusNfe { get; set; }
  public int? TransportadoraId { get; set; }

  public decimal ValorProdutos { get; set; }
  public decimal ValorDesconto { get; set; }
  public decimal ValorFrete { get; set; }
  public decimal ValorSeguro { get; set; }
  public decimal ValorOutrasDespesas { get; set; }
  public decimal ValorTotal { get; set; }

  public ICollection<NfesProdutos> NfesProdutos { get; set; } = new List<NfesProdutos>();
  public ICollection<NfesPagamentos> NfesPagamentos { get; set; } = new List<NfesPagamentos>();
  public NfesInformacoesAdicionais? NfesInformacoesAdicionais { get; set; }
}