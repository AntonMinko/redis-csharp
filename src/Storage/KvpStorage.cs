using System.Collections.Concurrent;

namespace codecrafters_redis.Storage;

internal record struct StorageStringValue(string Value, DateTime? ExpireAt = null);

internal class KvpStorage
{
    private ConcurrentDictionary<string, StorageStringValue> _store = new();

    public void Set(string key, string value, int? expireAfterMs = null)
    {
        var val = expireAfterMs.HasValue 
            ? new StorageStringValue(value, DateTime.Now.AddMilliseconds(expireAfterMs.Value)) 
            : new StorageStringValue(value);
        _store.AddOrUpdate(key, val, (_, _) => val);
    }

    public string? Get(string key)
    {
        var val = _store.GetValueOrDefault(key);
        if (val == default || (val.ExpireAt ?? DateTime.MaxValue) < DateTime.Now) return null;
        return val.Value;
    }

    public void Remove(string key)
    {
        _store.TryRemove(key, out _);
    }

    public void Initialize(IDictionary<string, StorageStringValue> loadedData) =>
        _store = new ConcurrentDictionary<string, StorageStringValue>(loadedData);

    public IEnumerable<string> Keys => _store.Keys;
}