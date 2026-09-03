namespace VoltaMarcada.Web.Operations;

public interface IComponentOperationExecutor
{
    Task ExecuteAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull;

    Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull;
}
