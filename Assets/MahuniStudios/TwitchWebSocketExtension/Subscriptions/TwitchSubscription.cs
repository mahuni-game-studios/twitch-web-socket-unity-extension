// © Copyright 2026 Mahuni Game Studios

using System;

namespace Mahuni.Twitch.Extension
{
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// Base subscription class, providing the type and JSON object
    /// </summary>
    public class TwitchSubscription
    {
        public readonly string subscriptionType;
        protected JObject jsonObject;

        protected TwitchSubscription(string subscriptionType)
        {
            this.subscriptionType = subscriptionType;
        }
            
        public string ToJson()
        {
            return jsonObject.ToString();
        }
    }
    
    /// <summary>
    /// Base subscription event class, providing the subscription event call
    /// </summary>
    public class TwitchSubscriptionEvent
    {
        public static Action<TwitchSubscriptionEvent> onSubscriptionEvent;
    }
}