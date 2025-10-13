using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[ReplicationRole(Role = ReplicationRole.Master)]
internal class PSync(MasterManager masterManager, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.PSync;
    public override bool SupportsReplication => false;

    protected override async Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var masterReplId = Settings.Replication.MasterReplicaSettings!.MasterReplId;
        var masterReplOffset = Settings.Replication.MasterReplicaSettings.MasterReplOffset;
        var fullResync = $"FULLRESYNC {masterReplId} {masterReplOffset}".ToSimpleString();
            
        masterManager.InitReplicaConnection(connection, fullResync);
            
        return ReplicaConnectionResponse;
    }
}