namespace Backend.Core.Features.NFe.DTOs;

public record NfeXmlParte(
    string? Documento, string Nome, string? Fantasia, string? Ie,
    string? Logradouro, string? Numero, string? Bairro, string? Telefone, string? Email);

public record NfeXmlItem(
    int NumeroItem, string CodigoProduto, string? Ean, string Descricao, string Unidade,
    decimal Quantidade, decimal ValorUnitario, decimal ValorDesconto, decimal ValorProdutos);

public record NfeXmlTotais(decimal Produtos, decimal Desconto, decimal Frete, decimal Seguro, decimal Outras, decimal Nota);

// Volumes (vol, somados) e veículo (veicTransp) do bloco transp.
public record NfeXmlTransporte(
    int? QuantidadeVolumes, string? Especie, string? Marca, string? Numeracao, decimal? PesoLiquido, decimal? PesoBruto,
    string? Placa, string? Uf, string? Rntc);

public record NfeXmlParcela(int Numero, DateTime Vencimento, decimal Valor);

// Dados lidos do XML da NF-e (4.0). ModalidadeFrete = modFrete (0, 1, 2, 3, 4, 9) quando presente.
public record NfeXmlDados(
    string ChaveAcesso, int Numero, short Serie, DateTime DataEmissao,
    NfeXmlParte Emitente, NfeXmlParte? Destinatario,
    IReadOnlyList<NfeXmlItem> Itens, NfeXmlTotais Totais,
    int? ModalidadeFrete, NfeXmlParte? Transportadora,
    IReadOnlyList<NfeXmlParcela> Parcelas, string? InformacoesComplementares,
    // Data de emissão no fuso do próprio XML (sem converter para UTC), para não deslocar o dia.
    DateTime DataEmissaoLocal = default, NfeXmlTransporte? Transporte = null);
