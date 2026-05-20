using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.NFe.Entities;

namespace Backend.Core.Features.Estoque.Entities;

public class MovimentacoesEstoques
{
    public int Id { get; set; }
    public DateTime DataMovimentacao { get; set; }
    public TipoMovimentacaoEstoque TipoMovimentacao { get; set; }
    public string? Observacao { get; set; }

    public Usuarios? Usuario { get; set; }
    public Nfes? Nfe { get; set; }

    public required IEnumerable<MovimentacoesEstoquesItens> MovimentacoesEstoquesItens { get; set; }
}