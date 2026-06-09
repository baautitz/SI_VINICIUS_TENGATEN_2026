namespace Backend.Core.Features.Localizacao.Commands;

public record CriarPaisCommand(string Pais, string SiglaIso, string Ddi, string Moeda, string SimboloMoeda);
