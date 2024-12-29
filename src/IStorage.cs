namespace codecrafters_redis;

public interface IStorage
{
    void Set(string key, string value, int? expireAfterMs = null);
    string? Get(string key);
}