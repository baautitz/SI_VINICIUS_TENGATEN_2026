using Backend.Core.Common.Interfaces;

namespace Backend.Infrastructure.PostgreSQL.Common;

public sealed class PostgreSQLUnitOfWork : IUnitOfWork
{
    private readonly DbSession _session;

    public PostgreSQLUnitOfWork(DbSession session)
    {
        _session = session;
    }

    public void BeginTransaction()
    {
        _session.Transaction = _session.Connection.BeginTransaction();
    }

    public void Commit()
    {
        _session.Transaction?.Commit();
        Dispose();
    }

    public void Rollback()
    {
        _session.Transaction?.Rollback();
        Dispose();
    }

    public void Dispose()
    {
        _session.Transaction?.Dispose();
        _session.Transaction = null;
    }
}