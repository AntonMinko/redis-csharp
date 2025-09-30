namespace codecrafters_redis.Models;

public record RedisValue(RedisTypes Type, byte[] Value)
{
    public static RedisValue ReplicaConnectionResponse = new(ReplicaConnection, []);
    public bool Success => Type != ErrorString;
}