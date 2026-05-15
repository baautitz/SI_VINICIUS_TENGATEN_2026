using backend.Modules.Parceiros.DTOs;
using backend.Modules.Parceiros.Models;

namespace backend.Modules.Parceiros.Repositories;

public interface IEmitentesRepository
{
    public Task<IEnumerable<Emitentes>> ObterEmitentes();
    public Task<Emitentes?> ObterEmitentePorId(int id);
    public Task<Emitentes> CriarEmitente(Emitentes emitente);
    public Task<Emitentes> AtualizarEmitente(int id, Emitentes emitente);
    public Task<bool> DeletarEmitente(int id);
    public Task<IEnumerable<EmitentesResumo>> ObterEmitentesResumo();
    public Task<IEnumerable<EmitentesResumo>> PesquisarEmitentes(string termo);
}
