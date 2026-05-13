namespace Modules.Estoque.Models;

public class MovimentacoesEstoques
{
    public int Id { get; set; }
    public DateTime DataMovimentacao { get; set; }
    public Enums.TipoMovimentacaoEstoque TipoMovimentacao { get; set; }
    public int? UsuarioId { get; set; }
    public int? NfeId { get; set; }
    public string? Observacao { get; set; }

    public Acesso.Models.Usuarios? Usuario { get; set; }
    public NFe.Models.Nfes? Nfe { get; set; }

    public ICollection<MovimentacoesEstoquesItens> MovimentacoesEstoquesItens { get; set; } = new List<MovimentacoesEstoquesItens>();
}