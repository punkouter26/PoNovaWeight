using Microsoft.Extensions.Caching.Hybrid;

namespace PoNovaWeight.Api.Tests.TestAuth;

/// <summary>
/// A fake HybridCache implementation for unit testing that bypasses caching.
/// Always calls the factory to get fresh data - no actual caching occurs.
/// </summary>
public sealed class FakeHybridCache : HybridCache
{
    public override ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        // Always call the factory - no caching
        return factory(state, cancellationToken);
    }

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
