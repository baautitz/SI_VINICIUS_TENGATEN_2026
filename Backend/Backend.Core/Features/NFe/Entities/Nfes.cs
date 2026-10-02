using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.ValueObjects;
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
    public Documento EmitenteCpfCnpj { get; private set; }
    public Documento? EmitenteRgIe { get; private set; }
    public string? EmitenteApelidoNomefantasia { get; private set; }
    public string? EmitenteEndereco { get; private set; }
    public string? EmitenteBairro { get; private set; }
    public string? EmitenteTelefone { get; private set; }
    public string? EmitenteEmail { get; private set; }

    public string ClienteNomeRazaosocial { get; private set; }
    public Documento ClienteCpfCnpj { get; private set; }
    public Documento? ClienteRgIe { get; private set; }
    public string? ClienteApelidoNomefantasia { get; private set; }
    public string? ClienteEndereco { get; private set; }
    public string? ClienteBairro { get; private set; }
    public string? ClienteTelefone { get; private set; }
    public string? ClienteEmail { get; private set; }

    public Emitentes Emitente { get; private set; }
    public Clientes? Cliente { get; private set; }
    public Fornecedores? Fornecedor { get; private set; }
    public int? VendaId { get; private set; }
    public int? CompraId { get; private set; }
    // Só preenchido na criação; as consultas não carregam a coluna (use ObterXmlDocumento).
    [System.Text.Json.Serialization.JsonIgnore]
    public string? XmlDocumento { get; private set; }
    public string? JustificativaCancelamento { get; private set; }
    public DateTime? DataCancelamento { get; private set; }
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

    protected Nfes()
    {
        EmitenteNomeRazaosocial = null!;
        EmitenteCpfCnpj = null!;
        ClienteNomeRazaosocial = null!;
        ClienteCpfCnpj = null!;
        Emitente = null!;
    }

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
        NfesInformacoesAdicionais? nfesInformacoesAdicionais = null,
        int? vendaId = null)
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
        VendaId = vendaId;

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
        EmitenteCpfCnpj = emitente.CpfCnpj;
        EmitenteRgIe = emitente.RgIe;
        EmitenteApelidoNomefantasia = TextNormalization.NormalizeOrNull(emitente.ApelidoNomeFantasia);
        EmitenteEndereco = TextNormalization.NormalizeOrNull(emitente.Logradouro + (string.IsNullOrWhiteSpace(emitente.Numero) ? "" : $", {emitente.Numero}"));
        EmitenteBairro = TextNormalization.Truncate(emitente.Bairro?.Bairro, 100);
        EmitenteTelefone = TextNormalization.NormalizeOrNull(emitente.Telefone);
        EmitenteEmail = TextNormalization.NormalizeOrNull(emitente.Email);
        ClienteNomeRazaosocial = TextNormalization.Normalize(cliente.NomeRazaoSocial);
        ClienteCpfCnpj = cliente.CpfCnpj;
        ClienteRgIe = cliente.RgIe;
        ClienteApelidoNomefantasia = TextNormalization.NormalizeOrNull(cliente.ApelidoNomeFantasia);
        ClienteEndereco = TextNormalization.NormalizeOrNull(cliente.Logradouro + (string.IsNullOrWhiteSpace(cliente.Numero) ? "" : $", {cliente.Numero}"));
        ClienteBairro = TextNormalization.Truncate(cliente.Bairro?.Bairro, 100);
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

    // NF-e de entrada importada do XML do fornecedor: números, chave e totais são os declarados na nota;
    // o fornecedor ocupa os snapshots cliente_* e não há pagamentos.
    public static Nfes CriarEntrada(
        string chaveAcesso, int numero, short serie, DateTime dataEmissao,
        Emitentes emitente, Fornecedores fornecedor, int compraId,
        IEnumerable<NfesItens> itens,
        decimal valorProdutos, decimal valorDesconto, decimal valorFrete, decimal valorSeguro, decimal valorOutrasDespesas, decimal valorTotal,
        NfesTransportes? transportes = null, NfesInformacoesAdicionais? informacoesAdicionais = null, string? xmlDocumento = null)
    {
        if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso.Length != 44)
            throw new DomainException("Chave de acesso deve ter 44 dígitos.");
        if (numero <= 0)
            throw new DomainException("Número da nota fiscal é obrigatório.");
        if (itens == null || !itens.Any())
            throw new DomainException("A nota fiscal deve conter ao menos um item.");

        var nfe = new Nfes
        {
            TipoOperacao = TipoOperacao.ENTRADA,
            StatusNfe = StatusNfe.EMITIDA,
            ChaveAcesso = chaveAcesso,
            Numero = numero,
            Serie = serie,
            DataEmissao = dataEmissao,
            Emitente = emitente ?? throw new DomainException("Emitente é obrigatório."),
            Fornecedor = fornecedor ?? throw new DomainException("Fornecedor é obrigatório."),
            CompraId = compraId,
            XmlDocumento = xmlDocumento,
            ValorProdutos = valorProdutos,
            ValorDesconto = valorDesconto,
            ValorFrete = valorFrete,
            ValorSeguro = valorSeguro,
            ValorOutrasDespesas = valorOutrasDespesas,
            ValorTotal = valorTotal,
            NfesTransportes = transportes,
            NfesInformacoesAdicionais = informacoesAdicionais,
            EmitenteNomeRazaosocial = TextNormalization.Truncate(TextNormalization.Normalize(emitente.NomeRazaoSocial), 150)!,
            EmitenteCpfCnpj = emitente.CpfCnpj,
            EmitenteRgIe = emitente.RgIe,
            EmitenteApelidoNomefantasia = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(emitente.ApelidoNomeFantasia), 100),
            EmitenteEndereco = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(emitente.Logradouro + (string.IsNullOrWhiteSpace(emitente.Numero) ? "" : $", {emitente.Numero}")), 150),
            EmitenteBairro = TextNormalization.Truncate(emitente.Bairro?.Bairro, 100),
            EmitenteTelefone = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(emitente.Telefone), 20),
            EmitenteEmail = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(emitente.Email), 254),
            ClienteNomeRazaosocial = TextNormalization.Truncate(TextNormalization.Normalize(fornecedor.NomeRazaosocial), 150)!,
            ClienteCpfCnpj = fornecedor.CpfCnpj,
            ClienteRgIe = fornecedor.RgIe,
            ClienteApelidoNomefantasia = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(fornecedor.ApelidoNomefantasia), 100),
            ClienteEndereco = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(fornecedor.Logradouro + (string.IsNullOrWhiteSpace(fornecedor.Numero) ? "" : $", {fornecedor.Numero}")), 150),
            ClienteBairro = TextNormalization.Truncate(fornecedor.Bairro?.Bairro, 100),
            ClienteTelefone = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(fornecedor.Telefone), 20),
            ClienteEmail = TextNormalization.Truncate(TextNormalization.NormalizeOrNull(fornecedor.Email), 254),
        };
        nfe._itens.AddRange(itens);
        return nfe;
    }

    // Saída: RASCUNHO com chave -> EMITIDA, guardando o XML gerado na emissão.
    public void Emitir(string xmlDocumento)
    {
        if (TipoOperacao != TipoOperacao.SAIDA || StatusNfe != StatusNfe.RASCUNHO)
            throw new DomainException("Somente NF-e de saída em rascunho pode ser emitida.");
        if (ChaveAcesso is not { Length: 44 })
            throw new DomainException("Chave de acesso deve ter 44 dígitos.");
        if (string.IsNullOrWhiteSpace(xmlDocumento))
            throw new DomainException("XML da NF-e é obrigatório.");

        XmlDocumento = xmlDocumento;
        StatusNfe = StatusNfe.EMITIDA;
    }

    // Cancelamento fiscal: não desfaz a venda, o estoque nem o financeiro.
    public void Cancelar(string justificativa, DateTime? quando = null)
    {
        if (TipoOperacao != TipoOperacao.SAIDA)
            throw new DomainException("NF-e de entrada não pode ser cancelada.");
        if (StatusNfe != StatusNfe.EMITIDA)
            throw new DomainException("Somente NF-e emitida pode ser cancelada.");

        justificativa = TextNormalization.Normalize(justificativa);
        if (justificativa.Length < 15 || justificativa.Length > 255)
            throw new DomainException("A justificativa do cancelamento deve ter entre 15 e 255 caracteres.");

        JustificativaCancelamento = justificativa;
        DataCancelamento = quando ?? DateTime.UtcNow;
        StatusNfe = StatusNfe.CANCELADA;
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

    public void AtualizarFornecedor(Fornecedores fornecedor)
    {
        Fornecedor = fornecedor ?? throw new DomainException("Fornecedor é obrigatório.");
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

        // Hidratação: os totais persistidos prevalecem (recalcular aqui zera o desconto antes de os itens chegarem).
        _itens.Clear();
        _itens.AddRange(itens);
    }

    public void DefinirPagamentos(IEnumerable<NfesPagamentos> pagamentos)
    {
        if (pagamentos == null)
            throw new DomainException("Pagamentos da nota fiscal são obrigatórios.");

        _pagamentos.Clear();
        _pagamentos.AddRange(pagamentos);
    }
    private void AtualizarTotais()
    {
        if (TipoOperacao == TipoOperacao.ENTRADA) return; // totais da entrada são os declarados no XML

        ValorProdutos = _itens.Sum(item => item.Quantidade * item.ValorUnitario);
        ValorTotal = ValorProdutos - ValorDesconto + ValorFrete + ValorSeguro + ValorOutrasDespesas;

        if (ValorTotal < 0)
            throw new DomainException("Valor total da nota fiscal não pode ser negativo.");
    }

    private void ValidarPagamentos()
    {
        if (TipoOperacao == TipoOperacao.ENTRADA) return; // XML de entrada: pag é ignorado

        var totalPagamentos = _pagamentos.Sum(p => p.ValorPagamento);

        if (totalPagamentos != ValorTotal)
            throw new DomainException("O total dos pagamentos deve ser igual ao valor total da nota fiscal.");
    }
}
