using Microsoft.Extensions.DependencyInjection;
using VoltaMarcada.Web.Operations;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Operations;

public sealed class ComponentOperationExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldKeepTheScopedServiceAliveUntilTheOperationCompletes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DisposalTracker>();
        services.AddScoped<TrackedScopedService>();
        services.AddSingleton<IComponentOperationExecutor, ComponentOperationExecutor>();
        await using var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetRequiredService<IComponentOperationExecutor>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var execution = executor.ExecuteAsync<TrackedScopedService>(async (service, _) =>
        {
            await completion.Task;
            Assert.False(service.IsDisposed);
        });

        completion.SetResult();
        await execution;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseAndDisposeADifferentScopedServiceForEachOperation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DisposalTracker>();
        services.AddScoped<TrackedScopedService>();
        services.AddSingleton<IComponentOperationExecutor, ComponentOperationExecutor>();
        await using var serviceProvider = services.BuildServiceProvider();
        var executor = serviceProvider.GetRequiredService<IComponentOperationExecutor>();

        var firstId = await executor.ExecuteAsync<TrackedScopedService, Guid>((service, _) => Task.FromResult(service.Id));
        var secondId = await executor.ExecuteAsync<TrackedScopedService, Guid>((service, _) => Task.FromResult(service.Id));
        var tracker = serviceProvider.GetRequiredService<DisposalTracker>();

        Assert.NotEqual(firstId, secondId);
        Assert.Equal(new[] { firstId, secondId }.OrderBy(id => id), tracker.DisposedIds.OrderBy(id => id));
    }

    private sealed class DisposalTracker
    {
        public List<Guid> DisposedIds { get; } = [];
    }

    private sealed class TrackedScopedService(DisposalTracker tracker) : IAsyncDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            tracker.DisposedIds.Add(Id);
            return ValueTask.CompletedTask;
        }
    }
}
