namespace Backend.Core.Features.Acesso.DTOs;

public record UsuariosResumo
{
    public int Id { get; init; }
    public string Nome { get; init; } = null!;
    public string CpfCnpj { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Usuario { get; init; } = null!;

    public UsuariosResumo() { }

    public UsuariosResumo(int id, string nome, string cpfCnpj, string email, string usuario)
    {
        Id = id;
        Nome = nome;
        CpfCnpj = cpfCnpj;
        Email = email;
        Usuario = usuario;
    }
}
