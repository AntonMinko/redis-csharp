using System.Collections.Concurrent;

namespace codecrafters_redis;

internal class KvpStorage : IStorage
{
    private ConcurrentDictionary<string, StorageValue> _store = new();

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

    public void Initialize(IDictionary<string, StorageValue> loadedData) =>
        _store = new ConcurrentDictionary<string, StorageValue>(loadedData);

    public IEnumerable<string> GetAllKeys() => _store.Keys;
}