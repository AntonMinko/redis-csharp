namespace codecrafters_redis.Models;

public enum RedisTypes
{
    Unknown,
    SimpleString,
    BulkString,
    BulkStringArray,
    ErrorString,
    BinaryContent
}