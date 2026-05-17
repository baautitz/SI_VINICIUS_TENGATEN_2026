namespace Backend.Core.Acesso.Entities;

public class Sessoes
{
  public long Id { get; set; }
  public required string Token { get; set; }
  public DateTime DataCriacao { get; set; }
  public DateTime? DataExpiracao { get; set; }
  public bool Ativo { get; set; }

  public required Usuarios Usuario { get; set; }
}