using System.Collections.Concurrent;
using VoltaMarcada.Application.PublicBooking;

namespace VoltaMarcada.Web.Configuration;

public sealed class PublicBookingSubmissionLimiter(TimeProvider timeProvider) : IPublicBookingSubmissionLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private const int PermitLimit = 5;
    private readonly ConcurrentDictionary<string, Counter> counters = new(StringComparer.Ordinal);

    public bool TryAcquire(string partitionKey)
    {
        var now = timeProvider.GetUtcNow();
        if (counters.Count >= 10_000 && !counters.ContainsKey(partitionKey))
        {
            foreach (var expired in counters.Where(item => now - item.Value.WindowStartedAtUtc >= Window).Take(1_000))
                counters.TryRemove(expired.Key, out _);
            if (counters.Count >= 10_000) return false;
        }
        var counter = counters.GetOrAdd(partitionKey, _ => new Counter(now));
        lock (counter)
        {
            if (now - counter.WindowStartedAtUtc >= Window)
            {
                counter.WindowStartedAtUtc = now;
                counter.Count = 0;
            }
            if (counter.Count >= PermitLimit) return false;
            counter.Count++;
            return true;
        }
    }

    private sealed class Counter(DateTimeOffset windowStartedAtUtc)
    {
        public DateTimeOffset WindowStartedAtUtc { get; set; } = windowStartedAtUtc;
        public int Count { get; set; }
    }
}
