using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.Vendas.Entities;

namespace Backend.Core.Features.Estoque.Entities;

public class MovimentacoesEstoques
{
    private readonly List<MovimentacoesEstoquesItens> _itens = new();

    public int Id { get; private set; }
    public DateTime DataMovimentacao { get; private set; }
    public TipoMovimentacaoEstoque TipoMovimentacao { get; private set; }
    public StatusMovimentacaoEstoque Status { get; private set; }
    public string? Observacao { get; private set; }
    public Usuarios? Usuario { get; private set; }
    public Nfes? Nfe { get; private set; }
    public Venda? Venda { get; private set; }
    public IReadOnlyCollection<MovimentacoesEstoquesItens> MovimentacoesEstoquesItens => _itens.AsReadOnly();

    protected MovimentacoesEstoques() { }

    public MovimentacoesEstoques(
        TipoMovimentacaoEstoque tipoMovimentacao,
        Usuarios? usuario = null,
        Nfes? nfe = null,
        Venda? venda = null,
        string? observacao = null,
        StatusMovimentacaoEstoque status = StatusMovimentacaoEstoque.RASCUNHO)
    {
        TipoMovimentacao = tipoMovimentacao;
        Usuario = usuario;
        Nfe = nfe;
        Venda = venda;
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        DataMovimentacao = DateTime.UtcNow;
        Status = status;
    }

    public MovimentacoesEstoques(
        int id,
        DateTime dataMovimentacao,
        TipoMovimentacaoEstoque tipoMovimentacao,
        Usuarios? usuario = null,
        Nfes? nfe = null,
        Venda? venda = null,
        string? observacao = null,
        StatusMovimentacaoEstoque status = StatusMovimentacaoEstoque.RASCUNHO)
        : this(tipoMovimentacao, usuario, nfe, venda, observacao, status)
    {
        Id = id;
        DataMovimentacao = dataMovimentacao;
    }

    public void AdicionarItemExistente(MovimentacoesEstoquesItens item)
    {
        if (item == null)
            throw new DomainException("Item é obrigatório.");

        _itens.Add(item);
    }

    public decimal TotalCusto => _itens.Sum(item => item.Quantidade * item.CustoUnitario);

    public void AdicionarItem(Skus sku, decimal quantidade, decimal custoUnitario, string produtoNome, string unidadeMedidaSigla)
    {
        if (sku == null)
            throw new DomainException("SKU é obrigatório para item de movimentação de estoque.");

        if (string.IsNullOrWhiteSpace(produtoNome))
            throw new DomainException("Nome do produto é obrigatório.");

        if (string.IsNullOrWhiteSpace(unidadeMedidaSigla))
            throw new DomainException("Unidade de medida é obrigatória.");

        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        if (custoUnitario < 0)
            throw new DomainException("Custo unitário não pode ser negativo.");

        if (_itens.Any(x => x.Sku.Sku == sku.Sku))
            throw new DomainException("Já existe um item com este SKU na movimentação.");

        _itens.Add(new MovimentacoesEstoquesItens(sku, quantidade, custoUnitario, produtoNome, unidadeMedidaSigla));
    }

    public void RemoverItem(MovimentacoesEstoquesItens item)
    {
        if (item == null)
            throw new DomainException("Item de movimentação é obrigatório.");

        if (!_itens.Contains(item))
            throw new DomainException("Item não pertence a esta movimentação.");

        _itens.Remove(item);
    }

    public void AtualizarObservacao(string? observacao)
    {
        Observacao = TextNormalization.NormalizeOrNull(observacao);
    }

    public void Confirmar()
    {
        if (Status != StatusMovimentacaoEstoque.RASCUNHO)
            throw new DomainException($"Apenas movimentações em rascunho podem ser confirmadas. Status atual: {Status}");

        if (!_itens.Any())
            throw new DomainException("A movimentação deve conter pelo menos um item para ser confirmada.");

        Status = StatusMovimentacaoEstoque.CONFIRMADA;
        DataMovimentacao = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        if (Status != StatusMovimentacaoEstoque.CONFIRMADA)
            throw new DomainException($"Apenas movimentações confirmadas podem ser canceladas. Status atual: {Status}");

        Status = StatusMovimentacaoEstoque.CANCELADA;
    }
}
