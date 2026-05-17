namespace Backend.Core.Acesso.DTOs;

public record UsuariosResumo(
    int Id,
    string Nome,
    string CpfCnpj,
    string Email,
    string Usuario
);
