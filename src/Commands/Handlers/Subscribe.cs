using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Subscriptions;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1)]
[SupportedInSubscribedMode(IsSupported = true)]
internal class Subscribe(PubSub pubSub, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Subscribe;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var channel = command.Arguments[0];
        
        int subscriptions = pubSub.Subscribe(EventType.Subscription, channel, connection);
        connection.EnterSubscribedMode();

        return new object[] { "subscribe", channel, subscriptions }.ToBulkStringArray();
    }
}