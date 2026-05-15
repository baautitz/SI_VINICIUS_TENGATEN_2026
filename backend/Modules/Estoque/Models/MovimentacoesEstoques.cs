using backend.Modules.Acesso.Models;
using backend.Modules.NFe.Models;

namespace backend.Modules.Estoque.Models;

public class MovimentacoesEstoques
{
    public int Id { get; set; }
    public DateTime DataMovimentacao { get; set; }
    public Enums.TipoMovimentacaoEstoque TipoMovimentacao { get; set; }
    public string? Observacao { get; set; }

    public Usuarios? Usuario { get; set; }
    public Nfes? Nfe { get; set; }

    public ICollection<MovimentacoesEstoquesItens> MovimentacoesEstoquesItens { get; set; } = new List<MovimentacoesEstoquesItens>();
}