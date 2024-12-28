using System.Collections.Concurrent;

namespace codecrafters_redis;

public class KvpStorage : IStorage
{
    private ConcurrentDictionary<string, string> _store = new();

    public void Set(string key, string value)
    {
        _store.AddOrUpdate(key, value, (k, v) => value);
    }

    public string? Get(string key)
    {
        return _store.GetValueOrDefault(key);
    }
}