using backend.Modules.Parceiros.Models;

namespace backend.Modules.NFe.Models;

public class Nfes
{
  public int Id { get; set; }
  public string? ChaveAcesso { get; set; }
  public int Numero { get; set; }
  public short Serie { get; set; }
  public DateTime DataEmissao { get; set; }
  public DateTime? DataSaida { get; set; }

  public required string EmitenteNomeRazaosocial { get; set; }
  public required string EmitenteCpfCnpj { get; set; }
  public string? EmitenteRgIe { get; set; }
  public string? EmitenteApelidoNomefantasia { get; set; }
  public string? EmitenteEndereco { get; set; }
  public string? EmitenteBairro { get; set; }
  public string? EmitenteTelefone { get; set; }
  public string? EmitenteEmail { get; set; }

  public required string ClienteNomeRazaosocial { get; set; }
  public required string ClienteCpfCnpj { get; set; }
  public string? ClienteRgIe { get; set; }
  public string? ClienteApelidoNomefantasia { get; set; }
  public string? ClienteEndereco { get; set; }
  public string? ClienteBairro { get; set; }
  public string? ClienteTelefone { get; set; }
  public string? ClienteEmail { get; set; }

  public required Emitentes Emitente { get; set; }
  public required Clientes Cliente { get; set; }

  public Enums.TipoOperacao TipoOperacao { get; set; }
  public Enums.StatusNfe StatusNfe { get; set; }

  public decimal ValorProdutos { get; set; }
  public decimal ValorDesconto { get; set; }
  public decimal ValorFrete { get; set; }
  public decimal ValorSeguro { get; set; }
  public decimal ValorOutrasDespesas { get; set; }
  public decimal ValorTotal { get; set; }

  public ICollection<NfesItens> NfesItens { get; set; } = new List<NfesItens>();
  public ICollection<NfesPagamentos> NfesPagamentos { get; set; } = new List<NfesPagamentos>();
  public NfesInformacoesAdicionais? NfesInformacoesAdicionais { get; set; }
}