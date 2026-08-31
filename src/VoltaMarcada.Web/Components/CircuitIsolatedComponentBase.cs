using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace VoltaMarcada.Web.Components;

public abstract class CircuitIsolatedComponentBase : OwningComponentBase
{
    protected TService GetScopedService<TService>() where TService : notnull =>
        ScopedServices.GetRequiredService<TService>();
}
