namespace Modules.ProjetoSistemas.Models
{
  public class Estados
  {
    public int Id { get; set; }

    public int PaisId { get; set; }

    public Paises Pais { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string Uf { get; set; } = null!;

    public ICollection<Cidades> Cidades { get; set; } = new List<Cidades>();
  }
}