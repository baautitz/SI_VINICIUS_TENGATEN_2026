using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;

namespace Backend.Core.Features.Estoque.Entities;

// Linha imutável do razão de estoque: depois de lançada nunca é alterada; o estorno é outra movimentação.
public class MovimentacoesEstoques
{
    private readonly List<MovimentacoesEstoquesItens> _itens = new();

    public int Id { get; private set; }
    public DateTime DataMovimentacao { get; private set; }
    public TipoMovimentacaoEstoque TipoMovimentacao { get; private set; }
    public OrigemMovimentacaoEstoque OrigemTipo { get; private set; }
    public int? OrigemId { get; private set; }
    public string? Motivo { get; private set; }
    public string? Observacao { get; private set; }
    public Usuarios? Usuario { get; private set; }
    public bool Estornada { get; private set; }
    public IReadOnlyCollection<MovimentacoesEstoquesItens> MovimentacoesEstoquesItens => _itens.AsReadOnly();

    protected MovimentacoesEstoques() { }

    public MovimentacoesEstoques(
        TipoMovimentacaoEstoque tipoMovimentacao,
        OrigemMovimentacaoEstoque origemTipo,
        int? origemId = null,
        string? motivo = null,
        string? observacao = null,
        Usuarios? usuario = null)
    {
        if (origemTipo != OrigemMovimentacaoEstoque.MANUAL && origemId is null)
            throw new DomainException("A origem da movimentação é obrigatória.");

        motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (origemTipo is OrigemMovimentacaoEstoque.MANUAL or OrigemMovimentacaoEstoque.ESTORNO
            && (motivo is null || motivo.Length < 5))
            throw new DomainException("Motivo é obrigatório e deve ter pelo menos 5 caracteres.");

        TipoMovimentacao = tipoMovimentacao;
        OrigemTipo = origemTipo;
        OrigemId = origemId;
        Motivo = motivo;
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Usuario = usuario;
        DataMovimentacao = DateTime.UtcNow;
    }

    public MovimentacoesEstoques(
        int id,
        DateTime dataMovimentacao,
        TipoMovimentacaoEstoque tipoMovimentacao,
        OrigemMovimentacaoEstoque origemTipo,
        int? origemId,
        string? motivo,
        string? observacao,
        Usuarios? usuario,
        bool estornada)
        : this(tipoMovimentacao, origemTipo, origemId, motivo, observacao, usuario)
    {
        Id = id;
        DataMovimentacao = dataMovimentacao;
        Estornada = estornada;
    }

    public void AdicionarItemExistente(MovimentacoesEstoquesItens item)
    {
        if (item == null)
            throw new DomainException("Item é obrigatório.");

        _itens.Add(item);
    }

    public decimal TotalCusto => _itens.Sum(item => item.Quantidade * item.CustoUnitario);

    public void AdicionarItem(Skus sku, decimal quantidade, decimal custoUnitario)
    {
        if (sku == null)
            throw new DomainException("SKU é obrigatório para item de movimentação de estoque.");

        if (_itens.Any(x => x.Sku.Sku == sku.Sku))
            throw new DomainException("Já existe um item com este SKU na movimentação.");

        _itens.Add(new MovimentacoesEstoquesItens(sku, quantidade, custoUnitario, sku.NomeExibicao, sku.Produto!.UnidadeMedida.Sigla));
    }
}
