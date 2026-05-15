using backend.Modules.Auditoria.Models;

namespace backend.Modules.Auditoria.Repositories;

public interface IAuditoriasRepository
{
    public Task<IEnumerable<Auditorias>> ObterAuditorias();
    public Task<Auditorias?> ObterAuditoriaPorId(long id);
    public Task<Auditorias> CriarAuditoria(Auditorias auditoria);
    public Task<IEnumerable<Auditorias>> ObterAuditoriasPorTabela(string tabela);
    public Task<IEnumerable<Auditorias>> ObterAuditoriasPorUsuario(int usuarioId);
}
