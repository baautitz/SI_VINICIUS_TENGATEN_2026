using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;

namespace Backend.Core.Features.Financeiro.Entities;

public class CondicoesPagamentos
{
    private readonly List<CondicoesPagamentosParcelas> _parcelas = new();

    public int Id { get; set; }
    public string Descricao { get; private set; }
    public decimal EntradaMinimaPercentual { get; private set; }
    public decimal DescontoPercentual { get; private set; }
    public decimal AcrescimoPercentual { get; private set; }
    public decimal MultaPercentual { get; private set; }
    public decimal TaxaJurosPercentual { get; private set; }
    public bool Ativo { get; private set; }

    public MetodosPagamentos MetodoPagamento { get; private set; }
    public IReadOnlyCollection<CondicoesPagamentosParcelas> CondicoesPagamentosParcelas => _parcelas.AsReadOnly();

    protected CondicoesPagamentos()
    {
        Descricao = null!;
        MetodoPagamento = null!;
    }

    public CondicoesPagamentos(
        string descricao,
        MetodosPagamentos metodoPagamento,
        IEnumerable<CondicoesPagamentosParcelas> parcelas,
        decimal entradaMinimaPercentual = 0m,
        decimal descontoPercentual = 0m,
        decimal acrescimoPercentual = 0m,
        decimal multaPercentual = 0m,
        decimal taxaJurosPercentual = 0m,
        bool ativo = true)
    {
        descricao = TextNormalization.Normalize(descricao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da condição de pagamento é obrigatória.");

        MetodoPagamento = metodoPagamento ?? throw new DomainException("Método de pagamento é obrigatório.");
        EntradaMinimaPercentual = entradaMinimaPercentual;
        DescontoPercentual = descontoPercentual;
        AcrescimoPercentual = acrescimoPercentual;
        MultaPercentual = multaPercentual;
        TaxaJurosPercentual = taxaJurosPercentual;
        Ativo = ativo;

        if (EntradaMinimaPercentual < 0 || EntradaMinimaPercentual > 100)
            throw new DomainException("Entrada mínima percentual deve estar entre 0 e 100.");

        if (DescontoPercentual < 0 || DescontoPercentual > 100)
            throw new DomainException("Desconto percentual deve estar entre 0 e 100.");

        if (AcrescimoPercentual < 0)
            throw new DomainException("Acréscimo percentual não pode ser negativo.");

        if (MultaPercentual < 0)
            throw new DomainException("Multa percentual não pode ser negativa.");

        if (TaxaJurosPercentual < 0)
            throw new DomainException("Taxa de juros percentual não pode ser negativa.");

        if (entradaMinimaPercentual != 100m && (parcelas == null || !parcelas.Any()))
            throw new DomainException("Condição de pagamento deve conter ao menos uma parcela.");

        if (parcelas != null)
        {
            _parcelas.AddRange(parcelas);
        }
        ValidarParcelas();
        Descricao = descricao;
    }

    public void Atualizar(
        string descricao,
        MetodosPagamentos metodo,
        decimal entradaMinimaPercentual,
        decimal descontoPercentual,
        decimal acrescimoPercentual,
        decimal multaPercentual,
        decimal taxaJurosPercentual,
        IEnumerable<CondicoesPagamentosParcelas> parcelas)
    {
        descricao = TextNormalization.Normalize(descricao);
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da condição de pagamento é obrigatória.");

        MetodoPagamento = metodo ?? throw new DomainException("Método de pagamento é obrigatório.");

        if (entradaMinimaPercentual < 0 || entradaMinimaPercentual > 100)
            throw new DomainException("Entrada mínima percentual deve estar entre 0 e 100.");

        if (descontoPercentual < 0 || descontoPercentual > 100)
            throw new DomainException("Desconto percentual deve estar entre 0 e 100.");

        if (acrescimoPercentual < 0)
            throw new DomainException("Acréscimo percentual não pode ser negativo.");

        if (multaPercentual < 0)
            throw new DomainException("Multa percentual não pode ser negativa.");

        if (taxaJurosPercentual < 0)
            throw new DomainException("Taxa de juros percentual não pode ser negativa.");

        Descricao = descricao;
        EntradaMinimaPercentual = entradaMinimaPercentual;
        DescontoPercentual = descontoPercentual;
        AcrescimoPercentual = acrescimoPercentual;
        MultaPercentual = multaPercentual;
        TaxaJurosPercentual = taxaJurosPercentual;

        _parcelas.Clear();
        if (parcelas != null)
        {
            _parcelas.AddRange(parcelas);
        }

        ValidarParcelas();
    }

    public void AtualizarDescricao(string descricao)
    {
        descricao = TextNormalization.Normalize(descricao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da condição de pagamento é obrigatória.");

        Descricao = descricao;
    }

    public void AtualizarMetodoPagamento(MetodosPagamentos metodoPagamento)
    {
        MetodoPagamento = metodoPagamento ?? throw new DomainException("Método de pagamento é obrigatório.");
    }

    public void AtualizarPercentuais(
        decimal entradaMinimaPercentual,
        decimal descontoPercentual,
        decimal acrescimoPercentual,
        decimal multaPercentual,
        decimal taxaJurosPercentual)
    {
        if (entradaMinimaPercentual < 0 || entradaMinimaPercentual > 100)
            throw new DomainException("Entrada mínima percentual deve estar entre 0 e 100.");

        if (descontoPercentual < 0 || descontoPercentual > 100)
            throw new DomainException("Desconto percentual deve estar entre 0 e 100.");

        if (acrescimoPercentual < 0)
            throw new DomainException("Acréscimo percentual não pode ser negativo.");

        if (multaPercentual < 0)
            throw new DomainException("Multa percentual não pode ser negativa.");

        if (taxaJurosPercentual < 0)
            throw new DomainException("Taxa de juros percentual não pode ser negativa.");

        EntradaMinimaPercentual = entradaMinimaPercentual;
        DescontoPercentual = descontoPercentual;
        AcrescimoPercentual = acrescimoPercentual;
        MultaPercentual = multaPercentual;
        TaxaJurosPercentual = taxaJurosPercentual;
        ValidarParcelas();
    }

    public void AdicionarParcela(CondicoesPagamentosParcelas parcela)
    {
        if (parcela == null)
            throw new DomainException("Parcela é obrigatória.");

        if (_parcelas.Any(p => p.NumeroParcela == parcela.NumeroParcela))
            throw new DomainException("Já existe uma parcela com o mesmo número.");

        _parcelas.Add(parcela);
        ValidarParcelas();
    }

    public void CarregarParcelas(IEnumerable<CondicoesPagamentosParcelas> parcelas)
    {
        _parcelas.Clear();
        if (parcelas != null)
        {
            _parcelas.AddRange(parcelas);
        }
    }

    public void RemoverParcela(CondicoesPagamentosParcelas parcela)
    {
        if (parcela == null)
            throw new DomainException("Parcela é obrigatória.");

        _parcelas.Remove(parcela);
        ValidarParcelas();
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    private void ValidarParcelas()
    {
        if (EntradaMinimaPercentual == 100m)
        {
            if (_parcelas.Any())
                throw new DomainException("Uma condição de pagamento à vista não deve possuir parcelas.");
            return;
        }

        if (!_parcelas.Any())
            throw new DomainException("Condição de pagamento deve conter ao menos uma parcela.");

        var totalParcelas = _parcelas.Sum(p => p.Percentual);

        if (totalParcelas <= 0m)
            throw new DomainException("O percentual total das parcelas deve ser maior que zero.");

        if (totalParcelas > 100m)
            throw new DomainException("O percentual total das parcelas não pode exceder 100%.");

        if (EntradaMinimaPercentual + totalParcelas != 100m)
            throw new DomainException("A soma da entrada mínima e das parcelas deve ser exatamente igual a 100%.");

        if (_parcelas.GroupBy(p => p.NumeroParcela).Any(g => g.Count() > 1))
            throw new DomainException("Não pode haver parcelas com números repetidos.");
    }
}
