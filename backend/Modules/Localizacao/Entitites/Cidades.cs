namespace Modules.ProjetoSistemas.Models
{
  public class Cidades
  {
    public int Id { get; set; }

    public string Cidade { get; set; } = null!;

    public short Ddd { get; set; }

    public int EstadoId { get; set; }

    public Estados Estado { get; set; } = null!;

    public ICollection<Bairros> Bairros { get; set; } = new List<Bairros>();
  }
}