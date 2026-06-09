using Backend.Core.Common.Results;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Parceiros.Repositories;

public interface IEmitentesRepository
{
    public Task<ResultadoPaginado<Emitentes>> ObterEmitentes(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Emitentes?> ObterEmitentePorId(int id);
    public Task<Emitentes> CriarEmitente(Emitentes emitente);
    public Task<Emitentes> AtualizarEmitente(int id, Emitentes emitente);
    public Task<bool> DeletarEmitente(int id);
    public Task<ResultadoPaginado<Emitentes>> PesquisarEmitentes(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteEmitenteCpfCnpj(string cpfCnpj, int nacionalidadeId, int? ignorarId = null);
}
