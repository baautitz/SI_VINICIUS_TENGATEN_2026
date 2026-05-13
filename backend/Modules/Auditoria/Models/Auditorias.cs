namespace Modules.Auditoria.Models;

public class Auditorias
{
  public long Id { get; set; }
  public required string Tabela { get; set; }
  public required OperacaoAuditoria Operacao { get; set; }
  public int UsuarioId { get; set; }
  public required DateTime DataHora { get; set; }
  public string? DadosAntigos { get; set; }
  public string? DadosNovos { get; set; }
  public string? Descricao { get; set; }
  public long? SessaoId { get; set; }

  public required Acesso.Models.Usuarios Usuario { get; set; }
  public required Acesso.Models.Sessoes? Sessao { get; set; }
}