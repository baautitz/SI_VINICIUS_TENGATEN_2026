namespace Modules.ProjetoSistemas.Models
{
  public class Cidades
  {
    public int Id { get; set; }

    public required string Cidade { get; set; }

    public short Ddd { get; set; }

    public int EstadoId { get; set; }

    public required Estados Estado { get; set; }

  }
}