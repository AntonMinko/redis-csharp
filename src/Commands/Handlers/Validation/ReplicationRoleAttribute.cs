namespace codecrafters_redis.Commands.Handlers.Validation;

internal class ReplicationRoleAttribute : Attribute
{
    public ReplicationRole Role { get; set; }
}