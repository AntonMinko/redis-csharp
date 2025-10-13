using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 2, Max = 2)]
[ReplicationRole(Role = ReplicationRole.Master)]
internal class Wait(MasterManager masterManager, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Wait;
    public override bool SupportsReplication => false;

    protected override async Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        int expectReplicas = int.Parse(command.Arguments[0]);
        int timeoutMs = int.Parse(command.Arguments[1]);
        $"Handling wait command. Expected replicas: {expectReplicas}, timeout: {timeoutMs} ms, Last command offset: {connection.LastCommandOffset}".WriteLineEncoded();
        
        int upToDateReplicas = masterManager.CountReplicasWithAckOffset(connection.LastCommandOffset);
        if (upToDateReplicas < expectReplicas)
        {
            var delayTask = Task.Delay(timeoutMs);
            await masterManager.UpdateReplicasOffsets();

            await delayTask;
        }
        
        // on timeout, return the actual number of sync replicas
        upToDateReplicas = masterManager.CountReplicasWithAckOffset(connection.LastCommandOffset);
        return upToDateReplicas.ToIntegerString();
    }
}