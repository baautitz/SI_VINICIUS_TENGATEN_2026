namespace Backend.Core.Features.Localizacao.Entities;

public class Bairros
{
    public int Id { get; set; }

    public required string Bairro { get; set; }

    public required Cidades Cidade { get; set; }
}