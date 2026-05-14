namespace Modules.ProjetoSistemas.Models
{
  public class Estados
  {
    public int Id { get; set; }

    public int PaisId { get; set; }

    public required Paises Pais { get; set; }

    public required string Estado { get; set; }

    public required string Uf { get; set; }

  }
}