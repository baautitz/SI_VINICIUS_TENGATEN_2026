namespace Backend.Core.Features.NFe.DTOs;

public record PreviaNfeItem(int NumeroItem, string Status, string? Sku, string? SugestaoSku);

// Status do item: VINCULADO (vínculo fornecedor-SKU), SUGERIDO (EAN confere, precisa confirmação) ou PENDENTE.
public record PreviaNfeDto(
    NfeXmlDados Dados,
    int? FornecedorId,
    int? TransportadoraId,
    IReadOnlyList<PreviaNfeItem> Itens,
    IReadOnlyList<string> Avisos,
    int? CondicaoPagamentoId = null);

public record ImportarNfeResultado(int CompraId, int NfeId, string ChaveAcesso);
