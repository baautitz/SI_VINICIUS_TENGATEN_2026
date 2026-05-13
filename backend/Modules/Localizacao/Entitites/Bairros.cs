namespace Modules.ProjetoSistemas.Entities
{
  public class Bairros
  {
    public int Id { get; set; }

    public string Bairro { get; set; } = null!;

    public int CidadeId { get; set; }

    public Cidades Cidade { get; set; } = null!;
  }
}