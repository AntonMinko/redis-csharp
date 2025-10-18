namespace codecrafters_redis.Subscriptions;

internal class PubSub
{
    private class Subscription(int subscriberId)
    {
        public readonly int SubsсriberId = subscriberId;
        public bool IsEventFired { get; set; }
        public string? EventPayload { get; set; }
    }

    private readonly Dictionary<string, LinkedList<Subscription>> _subscriptions = new();
    private readonly Dictionary<int, int> _subscribers = new();
    
    public int Subscribe(EventType eventType, string eventKey, int subscriberId)
    {
        var subscriptionKey = SubscriptionKey(eventType, eventKey);
        if (!_subscriptions.TryGetValue(subscriptionKey, out var subscriptions))
        {
            subscriptions = new LinkedList<Subscription>();
        }

        if (subscriptions.Any(x => x.SubsсriberId == subscriberId)) return _subscribers[subscriberId];
        
        subscriptions.AddLast(new Subscription(subscriberId));
        _subscriptions[subscriptionKey] = subscriptions;
        
        _subscribers[subscriberId] = _subscribers.ContainsKey(subscriberId) ? ++_subscribers[subscriberId] : 1;
        return _subscribers[subscriberId];
    }

    public string? Unsubscribe(EventType eventType, string eventKey, int subscriberId)
    {
        var subscriptionKey = SubscriptionKey(eventType, eventKey);
        if (!_subscriptions.TryGetValue(subscriptionKey, out var subscriptions)) return null;

        var subscription = subscriptions.FirstOrDefault(x => x.SubsсriberId == subscriberId);
        if (subscription == null) return null;
        
        subscriptions.Remove(subscription);
        return subscription.EventPayload;
    }

    public bool Publish(EventType eventType, string eventKey, string? eventPayload = null)
    {
        var subscriptionKey = SubscriptionKey(eventType, eventKey);
        if (!_subscriptions.TryGetValue(subscriptionKey, out var subscriptions)) return false;

        var subscription = subscriptions.FirstOrDefault(s => !s.IsEventFired);
        if (subscription == null) return false;

        subscription.IsEventFired = true;
        subscription.EventPayload = eventPayload;
        return true;
    }

    public bool IsEventFired(EventType eventType, string eventKey, int subscriberId)
    {
        var subscriptionKey = SubscriptionKey(eventType, eventKey);
        if (!_subscriptions.TryGetValue(subscriptionKey, out var subscriptions))
        {
            return false;
        }
        
        return subscriptions.FirstOrDefault(x => x.SubsсriberId == subscriberId)?.IsEventFired ?? false;
    }

    private string SubscriptionKey(EventType eventType, string eventKey) => $"{eventType}:{eventKey}";
}