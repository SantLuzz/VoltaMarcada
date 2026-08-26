using System.Collections.Concurrent;
using VoltaMarcada.Application.PublicBooking;

namespace VoltaMarcada.Web.Configuration;

public sealed class PublicQuoteActionLimiter(TimeProvider timeProvider)
{
    private const int PermitLimit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, Counter> counters = new(StringComparer.Ordinal);

    public bool TryAcquire(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var tokenHash = PublicTokenGenerator.Hash(token);
        var now = timeProvider.GetUtcNow();
        var counter = counters.GetOrAdd(tokenHash, _ => new Counter(now));
        lock (counter)
        {
            if (now - counter.StartedAtUtc >= Window)
            {
                counter.StartedAtUtc = now;
                counter.Count = 0;
            }

            if (counter.Count >= PermitLimit) return false;
            counter.Count++;
            return true;
        }
    }

    private sealed class Counter(DateTimeOffset startedAtUtc)
    {
        public DateTimeOffset StartedAtUtc { get; set; } = startedAtUtc;
        public int Count { get; set; }
    }
}
