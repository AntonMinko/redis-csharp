namespace codecrafters_redis;

internal record struct StorageValue(string Value, DateTime? ExpireAt = null);

internal interface IStorage
{
    void Set(string key, string value, int? expireAfterMs = null);
    string? Get(string key);
    void Initialize(IDictionary<string, StorageValue> settings);
    IEnumerable<string> GetAllKeys();
}