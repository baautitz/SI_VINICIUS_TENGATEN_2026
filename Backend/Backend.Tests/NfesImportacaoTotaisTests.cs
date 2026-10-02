using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Services;

namespace Backend.Tests;

public class NfesImportacaoTotaisTests
{
    private static NfeXmlItem Item(decimal vProd, decimal vDesc) => new(1, "A", null, "x", "UN", 2m, vProd / 2, vDesc, vProd);

    [Theory]
    [InlineData(20, 0, 2, 18, 0, 2)]    // vDesc só na nota (sem vDesc no item): desconto uma única vez
    [InlineData(20, 2, 2, 18, 0, 0)]    // vDesc igual ao dos itens: sem desconto geral
    [InlineData(20, 1, 3, 17, 0, 2)]    // parte nos itens, excedente na nota
    [InlineData(20, 0, 0, 22, 2, 0)]    // IPI/ST: diferença positiva vira outras despesas
    public void Valor_da_compra_fecha_com_o_vNF(decimal vProd, decimal descItens, decimal descNota, decimal vNF, decimal outrasEsp, decimal descGeralEsp)
    {
        var t = new NfeXmlTotais(vProd, descNota, 0m, 0m, 0m, vNF);

        var (outras, descGeral) = NfesImportacaoService.AjustarTotais(t, new[] { Item(vProd, descItens) });

        Assert.Equal(outrasEsp, outras);
        Assert.Equal(descGeralEsp, descGeral);
        Assert.Equal(vNF, vProd - descItens + outras - descGeral); // compras.valor_nota
    }
}
