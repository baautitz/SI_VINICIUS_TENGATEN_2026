using Backend.Core.Common;
using Backend.Core.Features.Acesso.Entities;

namespace Backend.Core.Features.Auditoria.Entities;

public class Auditorias
{
    public long Id { get; private set; }
    public string Tabela { get; private set; }
    public OperacaoAuditoria Operacao { get; private set; }
    public DateTime DataHora { get; private set; }
    public string? DadosAntigos { get; private set; }
    public string? DadosNovos { get; private set; }
    public string? Descricao { get; private set; }

    public Usuarios Usuario { get; private set; }
    public Sessoes? Sessao { get; private set; }

    public Auditorias(string tabela, OperacaoAuditoria operacao, Usuarios usuario, string? dadosAntigos = null, string? dadosNovos = null, string? descricao = null, Sessoes? sessao = null)
    {
        if (string.IsNullOrWhiteSpace(tabela))
        {
            throw new DomainException("Tabela é obrigatória para auditoria.");
        }

        Usuario = usuario ?? throw new DomainException("Usuário é obrigatório para auditoria.");
        Tabela = TextNormalization.Normalize(tabela);
        Operacao = operacao;
        DataHora = DateTime.UtcNow;
        DadosAntigos = TextNormalization.NormalizeOrNull(dadosAntigos);
        DadosNovos = TextNormalization.NormalizeOrNull(dadosNovos);
        Descricao = TextNormalization.NormalizeOrNull(descricao);
        Sessao = sessao;
    }

    public void VincularSessao(Sessoes sessao)
    {
        Sessao = sessao ?? throw new DomainException("Sessão de auditoria não pode ser nula.");
    }
}
