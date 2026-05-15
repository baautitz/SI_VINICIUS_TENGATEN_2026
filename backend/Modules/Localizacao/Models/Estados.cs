namespace backend.Modules.Localizacao.Models
{
    public class Estados
    {
        public int Id { get; set; }

        public required string Estado { get; set; }

        public required string Uf { get; set; }

        public required Paises Pais { get; set; }

    }
}