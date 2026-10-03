using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Parceiros.Commands;

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
    bool CadastrarTransportadora,
    // Dia de chegada da mercadoria; omitida = hoje (Brasília).
    DateTime? DataChegada = null,
    // Só valem quando o cadastro será criado (fornecedor/transportadora do XML ainda inexistente); substituem os dados do XML.
    CriarFornecedorCommand? FornecedorDados = null,
    CriarTransportadoraCommand? TransportadoraDados = null
);
