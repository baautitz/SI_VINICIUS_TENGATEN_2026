using Backend.Core.Features.Financeiro.Commands;

namespace Backend.Core.Features.NFe.Commands;

public record PreviaNfeCommand(string Xml, int? EmitenteId);

// Item do XML (nItem) -> SKU escolhido pelo usuário; itens omitidos usam o vínculo fornecedor-SKU já gravado.
public record ImportarNfeItemCommand(int NumeroItem, string Sku);

public record ImportarNfeCommand(
    string Xml,
    int EmitenteId,
    IEnumerable<ImportarNfeItemCommand>? Itens,
    IEnumerable<CriarParcelaPagarCommand>? Parcelas,
    int? CondicaoPagamentoId,
    decimal ValorFreteTerceiro,
    DateTime? VencimentoFreteTerceiro,
    int? TransportadoraId,
    bool CadastrarTransportadora
);
