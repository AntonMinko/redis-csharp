using System.Collections.Concurrent;

namespace codecrafters_redis;

public class KvpStorage : IStorage
{
    private record struct StorageValue(string Value, DateTime? ExpireAt = null);
    
    private readonly ConcurrentDictionary<string, StorageValue> _store = new();

    public void Set(string key, string value, int? expireAfterMs = null)
    {
        var val = expireAfterMs.HasValue ? new StorageValue(value, DateTime.Now.AddMilliseconds(expireAfterMs.Value)) : new StorageValue(value);
        _store.AddOrUpdate(key, val, (k, v) => val);
    }

    public string? Get(string key)
    {
        var val = _store.GetValueOrDefault(key);
        if (val == default || (val.ExpireAt ?? DateTime.MaxValue) < DateTime.Now) return null;
        return val.Value;
    }
}