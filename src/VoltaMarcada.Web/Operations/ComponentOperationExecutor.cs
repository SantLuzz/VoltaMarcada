using Microsoft.Extensions.DependencyInjection;

namespace VoltaMarcada.Web.Operations;

public sealed class ComponentOperationExecutor(IServiceScopeFactory scopeFactory) : IComponentOperationExecutor
{
    public Task ExecuteAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull =>
        ExecuteAsync<TService, object?>(
            async (service, token) =>
            {
                await operation(service, token);
                return null;
            },
            cancellationToken);

    public async Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await operation(service, cancellationToken);
    }
}
