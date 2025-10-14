namespace codecrafters_redis;

enum ValueType
{
    Unknown,
    String,
    StringArray
}

internal record struct TypedValue(ValueType Type, object Value)
{
    public string GetAsString() => (string)Value;
    public List<string> GetAsStringList() => (List<string>)Value;
}

internal record struct StorageValue(TypedValue Value, DateTime? ExpireAt = null);


internal interface IStorage
{
    void Set(string key, TypedValue value, int? expireAfterMs = null);
    TypedValue? Get(string key);
    void Initialize(IDictionary<string, StorageValue> loadedData);
    IEnumerable<string> GetAllKeys();
}