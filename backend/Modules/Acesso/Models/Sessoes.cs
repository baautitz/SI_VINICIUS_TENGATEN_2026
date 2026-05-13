namespace Modules.Acesso.Models;

public class Sessoes
{
  public long Id { get; set; }
  public int UsuarioId { get; set; }
  public string Token { get; set; } = null!;
  public DateTime DataCriacao { get; set; }
  public DateTime? DataExpiracao { get; set; }
  public bool Ativo { get; set; }

  public Usuarios Usuario { get; set; } = null!;
}