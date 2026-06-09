namespace Backend.Core.Features.Localizacao.Commands;

public record AtualizarPaisCommand(string Pais, string SiglaIso, string Ddi, string Moeda, string SimboloMoeda);
