using System.Collections.Concurrent;

namespace codecrafters_redis.Subscriptions;

internal class PubSub
{
    private class Subscription(int subscriberId)
    {
        public readonly int SubsсriberId = subscriberId;
        public bool IsEventFired { get; set; }
        public ConcurrentQueue<string> Messages { get; } = new();
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

    public IList<string> Unsubscribe(EventType eventType, string eventKey, int subscriberId)
    {
        var subscriptionKey = SubscriptionKey(eventType, eventKey);
        if (!_subscriptions.TryGetValue(subscriptionKey, out var subscriptions)) return new List<string>();

        var subscription = subscriptions.FirstOrDefault(x => x.SubsсriberId == subscriberId);
        if (subscription == null) return new List<string>();
        
        subscriptions.Remove(subscription);
        return subscription.Messages.ToList();
    }

    public int Publish(EventType eventType, string eventKey, string eventPayload)
    {
        int deliveries = 0;
        var subscriptionKey = SubscriptionKey(eventType, eventKey);
        if (!_subscriptions.TryGetValue(subscriptionKey, out var subscriptions)) return deliveries;

        return eventType switch
        {
            EventType.ListPushed => PublishListPushedMessage(subscriptions, eventPayload),
            EventType.Subscription => PublishSubscriptionMessage(subscriptions, eventPayload),
            _ => throw new Exception($"Unknown event type {eventType}")
        };

    }

    private int PublishSubscriptionMessage(LinkedList<Subscription> subscriptions, string eventPayload)
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Messages.Enqueue(eventPayload);
        }
        return subscriptions.Count;
    }

    private int PublishListPushedMessage(LinkedList<Subscription> subscriptions, string eventPayload)
    {
        var subscription = subscriptions.FirstOrDefault(s => s.Messages.Count == 0);
        if (subscription == null) return 0;

        subscription.Messages.Enqueue(eventPayload);
        return 1;
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