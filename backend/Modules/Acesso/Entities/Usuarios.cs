namespace Modules.Acesso.Entities;

public class Usuarios
{
  public int Id { get; set; }
  public string Nome { get; set; } = null!;
  public string CpfCnpj { get; set; } = null!;
  public string Email { get; set; } = null!;
  public string? Telefone { get; set; }
  public string Usuario { get; set; } = null!;
  public string Senha { get; set; } = null!;
  public bool Ativo { get; set; }

  public ICollection<Sessoes> Sessoes { get; set; } = new List<Sessoes>();
}