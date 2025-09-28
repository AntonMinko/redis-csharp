namespace codecrafters_redis.Models;

public record RedisValue(RedisTypes Type, byte[] Value)
{
    public static RedisValue EmptyResponse = new(BinaryContent, []);
    public bool Success => Type != ErrorString;
}