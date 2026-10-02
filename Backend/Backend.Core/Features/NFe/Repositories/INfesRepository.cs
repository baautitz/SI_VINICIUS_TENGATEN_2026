using Backend.Core.Common.Results;
using Backend.Core.Features.NFe.DTOs;

using Backend.Core.Features.NFe.Entities;

namespace Backend.Core.Features.NFe.Repositories;

public interface INfesRepository
{
    public Task<ResultadoPaginado<Nfes>> ObterNfes(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Nfes?> ObterNfePorId(int id);
    public Task<string?> ObterXmlDocumento(int id);
    public Task<NfeAtiva?> ObterNfeEmitidaDaVenda(int vendaId);
    // Próximo número da série para o emitente; exige transação aberta (advisory lock até o commit).
    public Task<int> ProximoNumeroSaida(int emitenteId, short serie);
    public Task CancelarNfe(Nfes nfe);
    public Task<bool> ExisteChaveAcesso(string chaveAcesso);
    public Task<Nfes> CriarNfe(Nfes nfe);
    public Task<Nfes> AtualizarNfe(int id, Nfes nfe);
    public Task<bool> DeletarNfe(int id);
    public Task<ResultadoPaginado<NfesResumo>> ObterNfesResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<NfesResumo>> PesquisarNfes(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
