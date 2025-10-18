using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 2, Max = 2)]
[ReplicationRole(Role = ReplicationRole.Master)]
internal class ReplConf(MasterManager masterManager, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.ReplConf;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        string commandSubtype = command.Arguments[0];
        switch (commandSubtype)
        {
            case "ACK":
                // replica to master
                long ackOffset = long.Parse(command.Arguments[1]);
                masterManager.SetReplicaAckOffset(ackOffset, connection);
                return NoResponse;
            default:
                return OkBytes;
        }
    }
}