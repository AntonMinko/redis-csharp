using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands.Handlers.Validation;

internal class SupportsAttribute : Attribute
{
    public ValueType StorageType { get; set; }
}