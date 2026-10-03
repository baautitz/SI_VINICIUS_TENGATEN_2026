using Backend.Core.Common.Results;
using Backend.Core.Features.NFe.Services;

namespace Backend.Core.Features.Compras.Services;

// Regras de data da compra/NF-e de entrada; "hoje" é sempre a data local de Brasília (-03:00), nunca UTC.
public static class DatasCompra
{
    public const string EmissaoFutura = "DATA_EMISSAO_FUTURA";
    public const string ChegadaFutura = "DATA_CHEGADA_FUTURA";
    public const string ChegadaAnteriorEmissao = "DATA_CHEGADA_ANTERIOR_EMISSAO";

    public static DateTime Hoje(DateTime? agoraUtc = null)
        => DateTime.SpecifyKind((agoraUtc ?? DateTime.UtcNow).Add(NfeChaveAcesso.FusoBrasilia).Date, DateTimeKind.Unspecified);

    // Instantes (Utc/Local) viram o dia de Brasília; datas sem fuso já são o dia local.
    public static DateTime DiaLocal(DateTime d)
        => d.Kind == DateTimeKind.Unspecified ? d.Date : d.ToUniversalTime().Add(NfeChaveAcesso.FusoBrasilia).Date;

    // emissaoLocal: dia de emissão no fuso do XML (null na compra manual).
    public static ResultadoErro? Validar(DateTime chegada, DateTime? emissaoLocal, DateTime? agoraUtc = null)
    {
        var hoje = Hoje(agoraUtc);
        var dia = DiaLocal(chegada);
        if (emissaoLocal is { } e && e.Date > hoje)
            return new ResultadoErro(EmissaoFutura, $"A data de emissão da NF-e ({e:dd/MM/yyyy}) é posterior a hoje: o XML está incorreto.", "Xml");
        if (dia > hoje)
            return new ResultadoErro(ChegadaFutura, "A data de chegada não pode ser posterior a hoje.", "DataCompra");
        if (emissaoLocal is { } em && dia < em.Date)
            return new ResultadoErro(ChegadaAnteriorEmissao, $"A data de chegada não pode ser anterior à emissão da NF-e ({em:dd/MM/yyyy}).", "DataCompra");
        return null;
    }
}
