namespace codecrafters_redis.Models;

public record RedisValue(RedisTypes Type, byte[] Value)
{
    public bool Success => Type != ErrorString;
}