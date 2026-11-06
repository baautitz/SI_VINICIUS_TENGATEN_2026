using System.Reflection;
using Backend.Core.Common.Interfaces;

namespace Backend.Tests.Common;

// Stub genérico: métodos retornam default (Task incluso) salvo o que o handler devolver.
public class Stub : DispatchProxy
{
    public Dictionary<string, Func<object?[]?, object?>> Handlers { get; } = new();

    protected override object? Invoke(MethodInfo? m, object?[]? args)
    {
        object? valor = Handlers.TryGetValue(m!.Name, out var h) ? h(args) : null;
        var ret = m.ReturnType;
        if (ret == typeof(Task)) return Task.CompletedTask;
        if (ret.IsGenericType && ret.GetGenericTypeDefinition() == typeof(Task<>))
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(ret.GetGenericArguments()[0]).Invoke(null, new[] { valor ?? Padrao(ret.GetGenericArguments()[0]) });
        return valor;
    }

    private static object? Padrao(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;
}

public static class Fakes
{
    public static T Criar<T>(Action<Dictionary<string, Func<object?[]?, object?>>>? config = null) where T : class
    {
        var p = DispatchProxy.Create<T, Stub>();
        config?.Invoke(((Stub)(object)p).Handlers);
        return p;
    }

    public static T Instancia<T>() => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
}

public class UowFake : IUnitOfWork
{
    public int Commits, Rollbacks;
    public void BeginTransaction() { }
    public void Commit() => Commits++;
    public void Rollback() => Rollbacks++;
    public void Dispose() { }
}
