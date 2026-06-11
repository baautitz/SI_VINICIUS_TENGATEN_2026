namespace Backend.Core.Features.Localizacao.Commands;

public record AtualizarPaisCommand(string Pais, string CodigoIsoPais, string Ddi, string CodigoIsoMoeda, string SimboloMoeda);
