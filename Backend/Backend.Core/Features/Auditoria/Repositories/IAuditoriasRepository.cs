using Backend.Core.Common.Results;
using Backend.Core.Features.Auditoria.Entities;

namespace Backend.Core.Features.Auditoria.Repositories;

public interface IAuditoriasRepository
{
    public Task<ResultadoPaginado<Auditorias>> ObterAuditorias(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Auditorias?> ObterAuditoriaPorId(long id);
    public Task<Auditorias> CriarAuditoria(Auditorias auditoria);
    public Task<ResultadoPaginado<Auditorias>> ObterAuditoriasPorTabela(string tabela, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<Auditorias>> ObterAuditoriasPorUsuario(int usuarioId, int pagina = 1, int tamanhoDaPagina = 20);
}
