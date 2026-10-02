namespace Backend.Core.Features.Compras.Commands;

public record EstornarCompraCommand(string? Motivo, int? UsuarioId = null);
