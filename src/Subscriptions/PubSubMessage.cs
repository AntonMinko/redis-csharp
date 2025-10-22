namespace codecrafters_redis.Subscriptions;

internal record PubSubMessage(EventType Type, string Channel, string Message);