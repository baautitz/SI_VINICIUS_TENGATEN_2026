namespace backend.Modules.Acesso.Models;

public class Usuarios
{
  public int Id { get; set; }
  public required string Nome { get; set; }
  public required string CpfCnpj { get; set; }
  public required string Email { get; set; }
  public string? Telefone { get; set; }
  public required string Usuario { get; set; }
  public required string Senha { get; set; }
  public bool Ativo { get; set; }

  public ICollection<Sessoes> Sessoes { get; set; } = new List<Sessoes>();
}