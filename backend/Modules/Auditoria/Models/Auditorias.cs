using backend.Modules.Acesso.Models;

namespace backend.Modules.Auditoria.Models;

public class Auditorias
{
  public long Id { get; set; }
  public required string Tabela { get; set; }
  public required OperacaoAuditoria Operacao { get; set; }
  public required DateTime DataHora { get; set; }
  public string? DadosAntigos { get; set; }
  public string? DadosNovos { get; set; }
  public string? Descricao { get; set; }

  public required Usuarios Usuario { get; set; }
  public Sessoes? Sessao { get; set; }
}