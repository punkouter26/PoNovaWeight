using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace PoNovaWeight.Api.Tests.TestAuth;

/// <summary>
/// A fake IMemoryCache implementation for unit testing.
/// Always calls the factory to get fresh data - no actual caching occurs.
/// </summary>
public sealed class FakeMemoryCache : IMemoryCache
{
    public ICacheEntry CreateEntry(object key)
    {
        return new FakeCacheEntry();
    }

    public void Dispose()
    {
        // No-op for tests
    }

    public bool TryGetValue(object key, out object? value)
    {
        value = null;
        return false;
    }

    public Task<TItem?> GetOrCreateAsync<TItem>(string key, Func<ICacheEntry, Task<TItem>> factory)
    {
        var entry = new FakeCacheEntry();
        return factory(entry)!;
    }

    public TItem? Get<TItem>(string key) where TItem : class
    {
        return null;
    }

    public void Remove(object key)
    {
        // No-op for tests
    }

    public bool TryGetValue<TItem>(string key, out TItem? value) where TItem : class
    {
        value = null;
        return false;
    }
}

/// <summary>
/// A fake ICacheEntry implementation.
/// </summary>
public sealed class FakeCacheEntry : ICacheEntry
{
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public long? Size { get; set; }
    public object? Value { get; set; }
    public CacheItemPriority Priority { get; set; }
    public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
    public IDictionary<object, object?> Properties { get; } = new Dictionary<object, object?>();
    public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();
    
    // Explicitly implemented interface members - Key is read-only in newer versions
    object ICacheEntry.Key { get; } = null!;

    public void Dispose()
    {
        // No-op for tests
    }
}
