using Backend.Core.NFe.DTOs;
using Backend.Core.NFe.Entities;

namespace Backend.Core.NFe.Repositories;

public interface INfesRepository
{
    public Task<IEnumerable<Nfes>> ObterNfes();
    public Task<Nfes?> ObterNfePorId(int id);
    public Task<Nfes> CriarNfe(Nfes nfe);
    public Task<Nfes> AtualizarNfe(int id, Nfes nfe);
    public Task<bool> DeletarNfe(int id);
    public Task<IEnumerable<NfesResumo>> ObterNfesResumo();
    public Task<IEnumerable<NfesResumo>> PesquisarNfes(string termo);
}
