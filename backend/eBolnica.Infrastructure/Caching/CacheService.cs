using eBolnica.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace eBolnica.Infrastructure.Caching;

public sealed class CacheService : ICacheService
{
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default) where T : class
    {
        // No caching — call the factory directly. Redis is disabled for the exam.
        return await factory(cancellationToken);
    }
}
