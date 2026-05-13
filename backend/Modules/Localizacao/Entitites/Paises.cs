namespace Modules.ProjetoSistemas.Models
{
  public class Paises
  {
    public int Id { get; set; }

    public string Ddi { get; set; } = null!;

    public string SiglaIso { get; set; } = null!;

    public string Moeda { get; set; } = null!;

    public string SimboloMoeda { get; set; } = null!;

    public string Pais { get; set; } = null!;

    public ICollection<Estados> Estados { get; set; } = new List<Estados>();
  }
}