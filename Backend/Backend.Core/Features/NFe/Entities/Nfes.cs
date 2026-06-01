using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.NFe.Entities;

public class Nfes
{
    private readonly List<NfesItens> _itens = new();
    private readonly List<NfesPagamentos> _pagamentos = new();

    public int Id { get; set; }
    public string? ChaveAcesso { get; private set; }
    public int Numero { get; private set; }
    public short Serie { get; private set; }
    public DateTime DataEmissao { get; private set; }
    public DateTime? DataSaida { get; private set; }

    public string EmitenteNomeRazaosocial { get; private set; }
    public string EmitenteCpfCnpj { get; private set; }
    public string? EmitenteRgIe { get; private set; }
    public string? EmitenteApelidoNomefantasia { get; private set; }
    public string? EmitenteEndereco { get; private set; }
    public Bairros? EmitenteBairro { get; private set; }
    public string? EmitenteTelefone { get; private set; }
    public string? EmitenteEmail { get; private set; }

    public string ClienteNomeRazaosocial { get; private set; }
    public string ClienteCpfCnpj { get; private set; }
    public string? ClienteRgIe { get; private set; }
    public string? ClienteApelidoNomefantasia { get; private set; }
    public string? ClienteEndereco { get; private set; }
    public Bairros? ClienteBairro { get; private set; }
    public string? ClienteTelefone { get; private set; }
    public string? ClienteEmail { get; private set; }

    public Emitentes Emitente { get; private set; }
    public Clientes Cliente { get; private set; }
    public NfesTransportes? NfesTransportes { get; private set; }
    public NfesInformacoesAdicionais? NfesInformacoesAdicionais { get; private set; }

    public TipoOperacao TipoOperacao { get; private set; }
    public StatusNfe StatusNfe { get; private set; }

    public decimal ValorProdutos { get; private set; }
    public decimal ValorDesconto { get; private set; }
    public decimal ValorFrete { get; private set; }
    public decimal ValorSeguro { get; private set; }
    public decimal ValorOutrasDespesas { get; private set; }
    public decimal ValorTotal { get; private set; }

    public IReadOnlyCollection<NfesItens> NfesItens => _itens.AsReadOnly();
    public IReadOnlyCollection<NfesPagamentos> NfesPagamentos => _pagamentos.AsReadOnly();

    public Nfes(
        int numero,
        short serie,
        DateTime dataEmissao,
        Emitentes emitente,
        Clientes cliente,
        TipoOperacao tipoOperacao,
        IEnumerable<NfesItens> itens,
        IEnumerable<NfesPagamentos> pagamentos,
        string? chaveAcesso = null,
        DateTime? dataSaida = null,
        decimal valorDesconto = 0m,
        decimal valorFrete = 0m,
        decimal valorSeguro = 0m,
        decimal valorOutrasDespesas = 0m,
        NfesTransportes? nfesTransportes = null,
        NfesInformacoesAdicionais? nfesInformacoesAdicionais = null)
    {
        if (numero <= 0)
            throw new DomainException("Número da nota fiscal é obrigatório.");

        if (serie < 0)
            throw new DomainException("Série da nota fiscal não pode ser negativa.");

        if (dataEmissao == default)
            throw new DomainException("Data de emissão é obrigatória.");

        if (dataSaida.HasValue && dataSaida.Value < dataEmissao)
            throw new DomainException("Data de saída não pode ser anterior à data de emissão.");

        Emitente = emitente ?? throw new DomainException("Emitente é obrigatório.");
        Cliente = cliente ?? throw new DomainException("Cliente é obrigatório.");
        TipoOperacao = tipoOperacao;

        if (itens == null || !itens.Any())
            throw new DomainException("A nota fiscal deve conter ao menos um item.");

        if (pagamentos == null || !pagamentos.Any())
            throw new DomainException("A nota fiscal deve conter ao menos uma forma de pagamento.");

        Numero = numero;
        Serie = serie;
        DataEmissao = dataEmissao;
        DataSaida = dataSaida;
        ChaveAcesso = TextNormalization.NormalizeOrNull(chaveAcesso);
        EmitenteNomeRazaosocial = TextNormalization.Normalize(emitente.NomeRazaoSocial);
        EmitenteCpfCnpj = TextNormalization.NormalizeDocument(emitente.CpfCnpj);
        EmitenteRgIe = TextNormalization.NormalizeDocumentOrNull(emitente.RgIe);
        EmitenteApelidoNomefantasia = TextNormalization.NormalizeOrNull(emitente.ApelidoNomeFantasia);
        EmitenteEndereco = TextNormalization.NormalizeOrNull(emitente.Endereco);
        EmitenteBairro = emitente.Bairro;
        EmitenteTelefone = TextNormalization.NormalizeOrNull(emitente.Telefone);
        EmitenteEmail = TextNormalization.NormalizeOrNull(emitente.Email);
        ClienteNomeRazaosocial = TextNormalization.Normalize(cliente.NomeRazaoSocial);
        ClienteCpfCnpj = TextNormalization.NormalizeDocument(cliente.CpfCnpj);
        ClienteRgIe = TextNormalization.NormalizeDocumentOrNull(cliente.RgIe);
        ClienteApelidoNomefantasia = TextNormalization.NormalizeOrNull(cliente.ApelidoNomeFantasia);
        ClienteEndereco = TextNormalization.NormalizeOrNull(cliente.Endereco);
        ClienteBairro = cliente.Bairro;
        ClienteTelefone = TextNormalization.NormalizeOrNull(cliente.Telefone);
        ClienteEmail = TextNormalization.NormalizeOrNull(cliente.Email);
        ValorDesconto = valorDesconto;
        ValorFrete = valorFrete;
        ValorSeguro = valorSeguro;
        ValorOutrasDespesas = valorOutrasDespesas;
        NfesTransportes = nfesTransportes;
        NfesInformacoesAdicionais = nfesInformacoesAdicionais;

        _itens.AddRange(itens);
        _pagamentos.AddRange(pagamentos);

        AtualizarTotais();
        ValidarPagamentos();
        StatusNfe = StatusNfe.RASCUNHO;
    }

    public void AtualizarStatus(StatusNfe status)
    {
        if (StatusNfe == StatusNfe.CANCELADA)
            throw new DomainException("Não é possível alterar o status de uma nota fiscal cancelada.");

        StatusNfe = status;
    }

    public void AtualizarEmitente(Emitentes emitente)
    {
        Emitente = emitente ?? throw new DomainException("Emitente é obrigatório.");
    }

    public void AtualizarCliente(Clientes cliente)
    {
        Cliente = cliente ?? throw new DomainException("Cliente é obrigatório.");
    }

    public void AdicionarItem(NfesItens item)
    {
        if (item == null)
            throw new DomainException("Item é obrigatório.");

        if (_itens.Any(x => x.NumeroItem == item.NumeroItem))
            throw new DomainException("Já existe um item com o mesmo número.");

        _itens.Add(item);
        AtualizarTotais();
        ValidarPagamentos();
    }

    public void AdicionarPagamento(NfesPagamentos pagamento)
    {
        if (pagamento == null)
            throw new DomainException("Pagamento é obrigatório.");

        _pagamentos.Add(pagamento);
        ValidarPagamentos();
    }

    public void AtualizarTransportes(NfesTransportes transportes)
    {
        NfesTransportes = transportes ?? throw new DomainException("Transporte é obrigatório.");
    }

    public void AtualizarInformacoesAdicionais(NfesInformacoesAdicionais informacoes)
    {
        NfesInformacoesAdicionais = informacoes;
    }
    public void DefinirItens(IEnumerable<NfesItens> itens)
    {
        if (itens == null)
            throw new DomainException("Itens da nota fiscal são obrigatórios.");

        _itens.Clear();
        _itens.AddRange(itens);
        AtualizarTotais();
    }

    public void DefinirPagamentos(IEnumerable<NfesPagamentos> pagamentos)
    {
        if (pagamentos == null)
            throw new DomainException("Pagamentos da nota fiscal são obrigatórios.");

        _pagamentos.Clear();
        _pagamentos.AddRange(pagamentos);
        ValidarPagamentos();
    }
    private void AtualizarTotais()
    {
        ValorProdutos = _itens.Sum(item => item.Quantidade * item.ValorUnitario);
        ValorTotal = ValorProdutos - ValorDesconto + ValorFrete + ValorSeguro + ValorOutrasDespesas;

        if (ValorTotal < 0)
            throw new DomainException("Valor total da nota fiscal não pode ser negativo.");
    }

    private void ValidarPagamentos()
    {
        var totalPagamentos = _pagamentos.Sum(p => p.ValorPagamento);

        if (totalPagamentos != ValorTotal)
            throw new DomainException("O total dos pagamentos deve ser igual ao valor total da nota fiscal.");
    }
}
