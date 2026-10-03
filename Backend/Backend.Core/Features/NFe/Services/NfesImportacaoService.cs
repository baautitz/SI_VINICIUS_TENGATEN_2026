using Backend.Core.Common;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Compras.Services;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.NFe.Commands;
using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.NFe.Services;

// Importação de NF-e de entrada: prévia stateless e confirmação (compra + NFe ENTRADA) numa transação.
public sealed class NfesImportacaoService : BaseService
{
    public const string ChaveJaImportada = "CHAVE_JA_IMPORTADA";

    private readonly INfesRepository _nfesRepository;
    private readonly IComprasRepository _comprasRepository;
    private readonly ComprasService _comprasService;
    private readonly IFornecedoresRepository _fornecedoresRepository;
    private readonly ITransportadorasRepository _transportadorasRepository;
    private readonly IEmitentesRepository _emitentesRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly IPaisesRepository _paisesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NfesImportacaoService(
        INfesRepository nfesRepository, IComprasRepository comprasRepository, ComprasService comprasService,
        IFornecedoresRepository fornecedoresRepository, ITransportadorasRepository transportadorasRepository,
        IEmitentesRepository emitentesRepository, ISkusRepository skusRepository, IPaisesRepository paisesRepository,
        IUnitOfWork unitOfWork)
    {
        _nfesRepository = nfesRepository;
        _comprasRepository = comprasRepository;
        _comprasService = comprasService;
        _fornecedoresRepository = fornecedoresRepository;
        _transportadorasRepository = transportadorasRepository;
        _emitentesRepository = emitentesRepository;
        _skusRepository = skusRepository;
        _paisesRepository = paisesRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<NfesResumo>> PesquisarNfes(string? termo, int pagina, int tamanhoDaPagina, TipoOperacao? tipoOperacao, StatusNfe? statusNfe)
        => _nfesRepository.PesquisarNfes(termo ?? "", pagina, tamanhoDaPagina, tipoOperacao?.ToString(), statusNfe?.ToString());

    public Task<Nfes?> ObterNfePorId(int id) => _nfesRepository.ObterNfePorId(id);
    public Task<string?> ObterXmlDocumento(int id) => _nfesRepository.ObterXmlDocumento(id);

    public async Task<Resultado<PreviaNfeDto>> Previa(PreviaNfeCommand command)
    {
        NfeXmlDados dados;
        try { dados = NfeXmlParser.Parse(command.Xml); }
        catch (DomainException ex) { return Resultado<PreviaNfeDto>.Falha(new ResultadoErro("XML_INVALIDO", ex.Message, "Xml")); }

        if (DatasCompra.Validar(DatasCompra.Hoje(), dados.DataEmissaoLocal) is { Code: DatasCompra.EmissaoFutura } emissaoFutura)
            return Resultado<PreviaNfeDto>.Falha(emissaoFutura);

        if (await _nfesRepository.ExisteChaveAcesso(dados.ChaveAcesso))
            return Resultado<PreviaNfeDto>.Falha(new ResultadoErro(ChaveJaImportada, "Esta NF-e já foi importada.", "Xml"));

        var avisos = new List<string>();
        var t = dados.Totais;
        if (Math.Abs(t.Nota - (t.Produtos - t.Desconto + t.Frete + t.Seguro + t.Outras)) > 0.01m)
            avisos.Add("vNF não confere com vProd - vDesc + vFrete + vSeg + vOutro (IPI/ST etc.): a diferença entra no custo como outras despesas.");
        if (dados.Parcelas.Count > 0 && Math.Abs(dados.Parcelas.Sum(p => p.Valor) - t.Nota) > 0.01m)
            avisos.Add("A soma das parcelas (cobr/dup) não confere com vNF.");
        if (dados.ModalidadeFrete == 1)
            avisos.Add("Frete por conta do destinatário (modFrete=1): informe o frete de terceiro (CT-e), se houver.");

        if (command.EmitenteId.HasValue && await _emitentesRepository.ObterEmitentePorId(command.EmitenteId.Value) is { } emitente
            && dados.Destinatario?.Documento is { } docDest && docDest != emitente.CpfCnpj.Valor)
            avisos.Add("O destinatário do XML é diferente do emitente selecionado.");

        var fornecedor = await ObterFornecedor(dados.Emitente);
        var vinculos = fornecedor is null ? new Dictionary<string, string>() : await _comprasRepository.ObterVinculosDoFornecedor(fornecedor.Id);
        var transportadora = dados.Transportadora?.Documento is { } doc ? await _transportadorasRepository.ObterTransportadoraPorCpfCnpj(doc) : null;
        if (dados.Transportadora is { } tp && transportadora is null && ValidarDocumento(tp).Erro is not null)
            avisos.Add($"CNPJ da transportadora inválido ('{tp.Documento}'): não será possível cadastrá-la; o documento é gravado como veio no XML.");

        var itens = new List<PreviaNfeItem>();
        foreach (var i in dados.Itens)
        {
            if (vinculos.TryGetValue(i.CodigoProduto, out var sku))
            {
                itens.Add(new PreviaNfeItem(i.NumeroItem, "VINCULADO", sku, null));
                continue;
            }
            // EAN só sugere, nunca vincula sozinho; "SEM GTIN" e códigos fora do padrão não geram sugestão.
            string? sugestao = null;
            if (i.Ean is { Length: 8 or 12 or 13 or 14 } ean && ean.All(char.IsAsciiDigit))
            {
                var porEan = await _skusRepository.ObterSkuPorSku(ean);
                if (porEan?.GtinEan == ean) sugestao = porEan.Sku;
            }
            itens.Add(new PreviaNfeItem(i.NumeroItem, sugestao is null ? "PENDENTE" : "SUGERIDO", null, sugestao));
        }

        return Resultado<PreviaNfeDto>.Sucesso(new PreviaNfeDto(dados, fornecedor?.Id, transportadora?.Id, itens, avisos,
            fornecedor?.CondicaoPagamento is { Ativo: true } cond ? cond.Id : null));
    }

    public async Task<Resultado<ImportarNfeResultado>> Importar(ImportarNfeCommand command)
    {
        // O servidor reparseia e revalida: nada vindo da prévia é confiável.
        NfeXmlDados dados;
        try { dados = NfeXmlParser.Parse(command.Xml); }
        catch (DomainException ex) { return Falha("XML_INVALIDO", ex.Message, "Xml"); }

        if (await _nfesRepository.ExisteChaveAcesso(dados.ChaveAcesso))
            return Falha(ChaveJaImportada, "Esta NF-e já foi importada.", "Xml");

        var chegada = command.DataChegada ?? DatasCompra.Hoje();
        if (DatasCompra.Validar(chegada, dados.DataEmissaoLocal) is { } erroData)
            return Resultado<ImportarNfeResultado>.Falha(erroData);

        var emitente = await _emitentesRepository.ObterEmitentePorId(command.EmitenteId);
        if (emitente is null)
            return Falha("EMITENTE_INEXISTENTE", "O emitente informado não existe.", "EmitenteId");

        // Frete por conta do emitente (CIF), transporte próprio ou sem transporte: frete de terceiro seria pagar duas vezes.
        if (command.ValorFreteTerceiro > 0 && dados.ModalidadeFrete is 0 or 3 or 9)
            return Falha("FRETE_DUPLICADO", $"A NF-e tem modFrete={dados.ModalidadeFrete} (frete não é do destinatário): não informe frete de terceiro.", "ValorFreteTerceiro");

        var parcelas = command.Parcelas?.Any() == true
            ? command.Parcelas
            : dados.Parcelas.Select(p => new CriarParcelaPagarCommand(p.Numero, p.Vencimento, p.Valor)).ToList();
        if (!parcelas.Any())
            return Falha("PARCELAS_OBRIGATORIAS", "O XML não traz parcelas (cobr/dup): informe as parcelas.", "Parcelas");

        try
        {
            _unitOfWork.BeginTransaction();

            var fornecedor = await ObterFornecedor(dados.Emitente);
            if (fornecedor is null)
            {
                var criado = await CriarFornecedor(dados.Emitente);
                if (!criado.Success) { _unitOfWork.Rollback(); return Resultado<ImportarNfeResultado>.Falha(criado.Errors!); }
                fornecedor = criado.Data!;
            }

            // Vínculos já existentes valem para itens sem escolha explícita.
            var vinculos = await _comprasRepository.ObterVinculosDoFornecedor(fornecedor.Id);
            var escolhas = (command.Itens ?? Array.Empty<ImportarNfeItemCommand>()).GroupBy(i => i.NumeroItem).ToDictionary(g => g.Key, g => g.Last().Sku);
            var skus = new Dictionary<int, Skus>();
            foreach (var i in dados.Itens)
            {
                if (!escolhas.TryGetValue(i.NumeroItem, out var codigo) && !vinculos.TryGetValue(i.CodigoProduto, out codigo))
                {
                    _unitOfWork.Rollback();
                    return Falha("ITEM_SEM_SKU", $"O item {i.NumeroItem} ({i.CodigoProduto}) não tem SKU vinculado.", "Itens");
                }
                var sku = await _skusRepository.ObterSkuPorSku(codigo);
                if (sku is null)
                {
                    _unitOfWork.Rollback();
                    return Falha("SKU_INEXISTENTE", $"O SKU '{codigo}' do item {i.NumeroItem} não existe.", "Itens");
                }
                skus[i.NumeroItem] = sku;
            }

            var transportadora = await ResolverTransportadora(command, dados);
            if (!transportadora.Success) { _unitOfWork.Rollback(); return Resultado<ImportarNfeResultado>.Falha(transportadora.Errors!); }

            var t = dados.Totais;
            // IPI, ST, II, FCP ST, ICMS desonerado etc. não são lidos: a diferença até o vNF entra como despesa (ou desconto),
            // integra o custo e fecha as parcelas com o vNF (compras.valor_nota = nfes.valor_total). Créditos recuperáveis ficam fora do escopo.
            var (outrasCompra, descontoCompra) = AjustarTotais(t, dados.Itens);
            var compraCommand = new CriarCompraCommand(
                // Dia de chegada preservado (meio-dia UTC não muda de dia em nenhum fuso do Brasil).
                DateTime.SpecifyKind(DatasCompra.DiaLocal(chegada).AddHours(12), DateTimeKind.Utc), command.EmitenteId, fornecedor.Id,
                t.Frete, t.Seguro, outrasCompra, descontoCompra,
                command.ValorFreteTerceiro, transportadora.Data?.Id, command.VencimentoFreteTerceiro,
                dados.InformacoesComplementares,
                dados.Itens.Select(i => new CriarCompraItemCommand(skus[i.NumeroItem].Sku, i.Quantidade, i.ValorUnitario, i.ValorDesconto, i.CodigoProduto, i.ValorProdutos)).ToList(),
                command.CondicaoPagamentoId, parcelas.ToList());

            var compra = await _comprasService.GravarCompra(compraCommand, dados.DataEmissaoLocal.Date);
            if (!compra.Success) { _unitOfWork.Rollback(); return Resultado<ImportarNfeResultado>.Falha(compra.Errors!); }

            var nfe = MontarNfe(command.Xml, dados, emitente, fornecedor, compra.Data!.Id, skus, transportadora.Data);
            nfe = await _nfesRepository.CriarNfe(nfe);

            _unitOfWork.Commit();
            return Resultado<ImportarNfeResultado>.Sucesso(new ImportarNfeResultado(compra.Data.Id, nfe.Id, dados.ChaveAcesso));
        }
        catch (UniqueConstraintException)
        {
            // Corrida: outra importação gravou a mesma chave entre a checagem e o INSERT.
            _unitOfWork.Rollback();
            return Falha(ChaveJaImportada, "Esta NF-e já foi importada.", "Xml");
        }
        catch (Exception)
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    private static Resultado<ImportarNfeResultado> Falha(string code, string message, string? field = null)
        => Resultado<ImportarNfeResultado>.Falha(new ResultadoErro(code, message, field));

    private static Nfes MontarNfe(string xml, NfeXmlDados d, Emitentes emitente, Fornecedores fornecedor, int compraId, Dictionary<int, Skus> skus, Transportadoras? transportadora)
    {
        var itens = d.Itens.Select(i => new NfesItens(
            i.NumeroItem, TextNormalization.Truncate(i.Descricao, 150)!, i.Quantidade, i.ValorUnitario, i.ValorDesconto,
            skus[i.NumeroItem], skus[i.NumeroItem].Produto!.UnidadeMedida)).ToList();

        NfesTransportes? transportes = null;
        if (d.ModalidadeFrete is { } mod)
        {
            var modalidade = mod switch
            {
                0 => ModalidadeFrete.EMITENTE, 1 => ModalidadeFrete.DESTINATARIO, 2 => ModalidadeFrete.TERCEIROS,
                3 => ModalidadeFrete.REMETENTE_PROPRIO, 4 => ModalidadeFrete.DESTINATARIO_PROPRIO, 9 => ModalidadeFrete.SEM_TRANSPORTE,
                _ => throw new DomainException($"modFrete inválido: {mod}.")
            };
            // Snapshot do XML mesmo sem transportadora cadastrada (documento como veio, mesmo inválido).
            var x = d.Transportadora;
            var v = d.Transporte;
            var uf = Cortar(v?.Uf, 2)?.ToUpperInvariant() is { Length: 2 } u ? u : null;
            transportes = new NfesTransportes(modalidade,
                Cortar(x?.Nome, 150) ?? transportadora?.NomeRazaosocial,
                Documento.CriarGenerico(Cortar(Digitos(x?.Documento), 14)) ?? transportadora?.CpfCnpj,
                Documento.CriarGenerico(Cortar(x?.Ie, 15)),
                uf is null ? null : Cortar(v?.Placa, 10), uf, Documento.CriarGenerico(Cortar(v?.Rntc, 20)),
                v?.QuantidadeVolumes, Cortar(v?.Especie, 50), Cortar(v?.Marca, 50), Cortar(v?.Numeracao, 50),
                v?.PesoBruto, v?.PesoLiquido, exigirTransportadora: false);
        }

        var t = d.Totais;
        return Nfes.CriarEntrada(d.ChaveAcesso, d.Numero, d.Serie, d.DataEmissao, emitente, fornecedor, compraId, itens,
            t.Produtos, t.Desconto, t.Frete, t.Seguro, t.Outras, t.Nota, transportes,
            string.IsNullOrWhiteSpace(d.InformacoesComplementares) ? null : new NfesInformacoesAdicionais(null, d.InformacoesComplementares), xml);
    }

    private static string? Digitos(string? v) => v is null ? null : new string(v.Where(char.IsAsciiDigit).ToArray());

    private Task<Fornecedores?> ObterFornecedor(NfeXmlParte parte)
        => Digitos(parte.Documento) is { Length: > 0 } doc ? _fornecedoresRepository.ObterFornecedorPorCpfCnpj(doc) : Task.FromResult<Fornecedores?>(null);

    private async Task<Resultado<Fornecedores>> CriarFornecedor(NfeXmlParte p)
    {
        var (tipo, documento, erro) = ValidarDocumento(p);
        if (erro is not null) return Resultado<Fornecedores>.Falha(erro);
        var brasil = await _paisesRepository.ObterPaisPorCodigoIso("BRA");
        if (brasil is null) return Resultado<Fornecedores>.Falha(new ResultadoErro("PAIS_INEXISTENTE", "País BRA não cadastrado.", "Fornecedor"));

        var fornecedor = new Fornecedores(tipo, Cortar(p.Nome, 150)!, documento!, brasil,
            Documento.CriarGenerico(Cortar(p.Ie, 20)), Cortar(p.Fantasia, 100), Cortar(p.Logradouro, 150), Cortar(p.Numero, 20),
            null, Cortar(p.Telefone, 20), Cortar(p.Email, 254), "Cadastrado na importação de NF-e.");
        return Resultado<Fornecedores>.Sucesso(await _fornecedoresRepository.CriarFornecedor(fornecedor));
    }

    private async Task<Resultado<Transportadoras?>> ResolverTransportadora(ImportarNfeCommand command, NfeXmlDados dados)
    {
        Transportadoras? t = null;
        if (command.TransportadoraId is { } id)
        {
            t = await _transportadorasRepository.ObterTransportadoraPorId(id);
            if (t is null) return Resultado<Transportadoras?>.Falha(new ResultadoErro("TRANSPORTADORA_INEXISTENTE", "A transportadora informada não existe.", "TransportadoraId"));
        }
        else if (Digitos(dados.Transportadora?.Documento) is { Length: > 0 } doc)
        {
            t = await _transportadorasRepository.ObterTransportadoraPorCpfCnpj(doc);
            if (t is null && command.CadastrarTransportadora)
            {
                var p = dados.Transportadora!;
                var (tipo, documento, erro) = ValidarDocumento(p);
                if (erro is not null) return Resultado<Transportadoras?>.Falha(erro);
                var brasil = await _paisesRepository.ObterPaisPorCodigoIso("BRA");
                if (brasil is null) return Resultado<Transportadoras?>.Falha(new ResultadoErro("PAIS_INEXISTENTE", "País BRA não cadastrado.", "Transportadora"));
                t = await _transportadorasRepository.CriarTransportadora(new Transportadoras(
                    tipo, Cortar(p.Nome, 150)!, documento!, brasil, Documento.CriarGenerico(Cortar(p.Ie, 20)),
                    logradouro: Cortar(p.Logradouro, 150), observacao: "Cadastrada na importação de NF-e."));
            }
        }
        return Resultado<Transportadoras?>.Sucesso(t);
    }

    private static (TipoPessoa Tipo, Documento? Documento, ResultadoErro? Erro) ValidarDocumento(NfeXmlParte p)
    {
        var doc = Digitos(p.Documento) ?? "";
        var tipo = doc.Length == 11 ? TipoPessoa.FISICA : TipoPessoa.JURIDICA;
        var documento = Documento.Criar(doc, "BRA", tipo);
        var erro = doc.Length is 11 or 14 && documento.EhValido() ? null
            : new ResultadoErro("DOCUMENTO_INVALIDO", $"CPF/CNPJ '{p.Documento}' de '{p.Nome}' é inválido.", "Documento");
        return (tipo, documento, erro);
    }

    private static string? Cortar(string? v, int max) => TextNormalization.Truncate(v?.Trim(), max);

    // Despesa/desconto gerais da compra para fechar com o vNF. O vDesc da nota já inclui o dos itens, então só o
    // excedente é desconto geral; a diferença não lida (IPI/ST etc.) é calculada já descontando o vDesc total.
    public static (decimal Outras, decimal Desconto) AjustarTotais(NfeXmlTotais t, IReadOnlyCollection<NfeXmlItem> itens)
    {
        var descontoItens = itens.Sum(i => i.ValorDesconto);
        var descontoTotal = Math.Max(t.Desconto, descontoItens);
        var diferenca = t.Nota - (itens.Sum(i => i.ValorProdutos) - descontoTotal + t.Frete + t.Seguro + t.Outras);
        return (t.Outras + Math.Max(diferenca, 0m), descontoTotal - descontoItens + Math.Max(-diferenca, 0m));
    }
}
