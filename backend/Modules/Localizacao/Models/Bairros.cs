namespace backend.Modules.Localizacao.Models;

public class Bairros
{
    public int Id { get; set; }

    public required string Bairro { get; set; }

    public required Cidades Cidade { get; set; }
}