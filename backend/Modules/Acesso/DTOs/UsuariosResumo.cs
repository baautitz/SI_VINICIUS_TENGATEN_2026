namespace backend.Modules.Acesso.DTOs;

public record UsuariosResumo(
    int Id,
    string Nome,
    string CpfCnpj,
    string Email,
    string Usuario
);
