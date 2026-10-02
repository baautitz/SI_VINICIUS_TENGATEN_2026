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

    public void Fechar()
    {
        ExigirAberto();
        if (!_itens.Any(i => i.QuantidadeContada.HasValue))
            throw new DomainException("Informe a contagem de pelo menos um item para fechar o balanço.");

        Status = StatusBalanco.FECHADO;
        DataFechamento = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        ExigirAberto();
        Status = StatusBalanco.CANCELADO;
        DataFechamento = DateTime.UtcNow;
    }

    public void ExigirAberto()
    {
        if (Status != StatusBalanco.ABERTO)
            throw new DomainException($"Apenas balanços abertos podem ser alterados. Status atual: {Status}");
    }
}
