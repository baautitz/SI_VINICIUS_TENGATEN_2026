using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;

namespace Backend.Core.Features.Estoque.Entities;

public class Balancos
{
    private readonly List<BalancosItens> _itens = new();

    public int Id { get; private set; }
    public DateTime DataAbertura { get; private set; }
    public DateTime? DataFechamento { get; private set; }
    public StatusBalanco Status { get; private set; }
    public string? Observacao { get; private set; }
    public Usuarios? Usuario { get; private set; }
    public IReadOnlyCollection<BalancosItens> Itens => _itens.AsReadOnly();

    protected Balancos() { }

    public Balancos(string? observacao, Usuarios? usuario)
    {
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        Usuario = usuario;
        DataAbertura = DateTime.UtcNow;
        Status = StatusBalanco.ABERTO;
    }

    public Balancos(int id, DateTime dataAbertura, DateTime? dataFechamento, StatusBalanco status, string? observacao, Usuarios? usuario)
        : this(observacao, usuario)
    {
        Id = id;
        DataAbertura = dataAbertura;
        DataFechamento = dataFechamento;
        Status = status;
    }

    public void AdicionarItem(BalancosItens item) => _itens.Add(item);

    // Enquanto aberto, o balanço pode ter observação e itens redefinidos.
    public void Atualizar(string? observacao, IEnumerable<BalancosItens> itens)
    {
        ExigirAberto();
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        _itens.Clear();
        _itens.AddRange(itens);
    }

    public void Fechar()
    {
        ExigirAberto();
        if (!_itens.Any(i => i.QuantidadeContada.HasValue))
            throw new DomainException("Informe a contagem de pelo menos um item para fechar o balanço.");

        Status = StatusBalanco.FECHADO;
        DataFechamento = DateTime.UtcNow;
    }

    // Aberto: descarta. Fechado: o chamador deve estornar antes as movimentações geradas.
    public void Cancelar()
    {
        if (Status == StatusBalanco.CANCELADO)
            throw new DomainException("Balanço já está cancelado.");

        Status = StatusBalanco.CANCELADO;
        DataFechamento = DateTime.UtcNow;
    }

    public void ExigirAberto()
    {
        if (Status != StatusBalanco.ABERTO)
            throw new DomainException($"Apenas balanços abertos podem ser alterados. Status atual: {Status}");
    }
}
