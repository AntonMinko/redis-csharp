using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Subscriptions;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1)]
[SupportedInSubscribedMode(IsSupported = true)]
internal class Unsubscribe(PubSub pubSub, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Unsubscribe;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var channel = command.Arguments[0];
        
        int subscriptions = pubSub.Unsubscribe(EventType.Subscription, channel, connection.Id);

        return new object[] { "unsubscribe", channel, subscriptions }.ToBulkStringArray();
    }
}