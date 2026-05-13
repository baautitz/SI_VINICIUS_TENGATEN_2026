namespace Modules.Auditoria.Entities;

public class Auditorias
{
  public long Id { get; set; }
  public string Tabela { get; set; } = null!;
  public OperacaoAuditoria Operacao { get; set; }
  public int UsuarioId { get; set; }
  public DateTime DataHora { get; set; }
  public string? DadosAntigos { get; set; }
  public string? DadosNovos { get; set; }
  public string? Descricao { get; set; }
  public long? SessaoId { get; set; }

  public Acesso.Entities.Usuarios Usuario { get; set; } = null!;
  public Acesso.Entities.Sessoes? Sessao { get; set; }
}