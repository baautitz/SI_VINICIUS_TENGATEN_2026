using Backend.Core.Parceiros.DTOs;
using Backend.Core.Parceiros.Entities;

namespace Backend.Core.Parceiros.Repositories;

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
