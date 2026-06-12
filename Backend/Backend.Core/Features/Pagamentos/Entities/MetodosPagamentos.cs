using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
namespace Backend.Core.Features.Pagamentos.Entities;

public class MetodosPagamentos
{
    public int Id { get; set; }
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }
    public bool Ativo { get; private set; }

    protected MetodosPagamentos() 
    { 
        Codigo = null!; 
        Descricao = null!; 
    }

    public MetodosPagamentos(string codigo, string descricao, bool ativo = true)
    {
        codigo = TextNormalization.Normalize(codigo);
        descricao = TextNormalization.Normalize(descricao);

        if (string.IsNullOrWhiteSpace(codigo))
            throw new DomainException("Código do método de pagamento é obrigatório.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição do método de pagamento é obrigatória.");

        Codigo = codigo;
        Descricao = descricao;
        Ativo = ativo;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    public void AtualizarDescricao(string descricao)
    {
        descricao = TextNormalization.Normalize(descricao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição do método de pagamento é obrigatória.");

        Descricao = descricao;
    }
}
