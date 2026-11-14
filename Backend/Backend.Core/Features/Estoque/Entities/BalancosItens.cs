using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Estoque.Entities.Enums;

namespace Backend.Core.Features.Estoque.Entities;

public class BalancosItens
{
    public int Id { get; private set; }
    public string Sku { get; private set; } = null!;
    public string ProdutoNome { get; private set; } = null!;
    public string UnidadeMedidaSigla { get; private set; } = null!;
    public decimal QuantidadeSistema { get; private set; }
    public decimal? QuantidadeContada { get; private set; }
    public decimal? Diferenca => QuantidadeContada - QuantidadeSistema;
    // Por que o item diverge do saldo e se foi recontado; o fechamento confere os dois contra o sinal da diferença.
    public MotivoMovimentacaoEstoque? MotivoDivergencia { get; private set; }
    public bool Recontado { get; private set; }

    protected BalancosItens() { }

    public BalancosItens(int id, string sku, string produtoNome, string unidadeMedidaSigla, decimal quantidadeSistema, decimal? quantidadeContada,
        MotivoMovimentacaoEstoque? motivoDivergencia = null, bool recontado = false)
    {
        MotivoDivergencia = motivoDivergencia;
        Recontado = recontado;
        Id = id;
        Sku = sku;
        ProdutoNome = produtoNome;
        UnidadeMedidaSigla = unidadeMedidaSigla;
        QuantidadeSistema = quantidadeSistema;
        QuantidadeContada = quantidadeContada;
    }

    public void InformarContagem(decimal? quantidadeContada)
    {
        if (quantidadeContada < 0)
            throw new DomainException("Quantidade contada não pode ser negativa.");

        QuantidadeContada = quantidadeContada;
    }

    // No fechamento a diferença é calculada contra o saldo vigente (vendas durante a contagem não distorcem o ajuste).
    public void AtualizarQuantidadeSistema(decimal quantidadeSistema) => QuantidadeSistema = quantidadeSistema;
}
