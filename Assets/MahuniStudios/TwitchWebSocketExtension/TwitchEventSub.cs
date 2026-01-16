// © Copyright 2026 Mahuni Game Studios

// ReSharper disable InconsistentNaming

namespace Mahuni.Twitch.Extension
{
    using System;
    using UnityEngine;
    
    /// <summary>
    /// Holds subscription request structures needed for the Twitch EventSub
    /// https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types#subscription-types
    /// </summary>
    public static class TwitchEventSub
    {
        /// <summary>
        /// Make a subscription to the EventSub
        /// </summary>
        /// <param name="subscription">The subscription type to pass</param>
        public static async Awaitable<(TwitchResponseCode responseCode, string responseBody)> Subscribe(string subscription)
        {
            return await TwitchRequest.AwaitablePost("eventsub/subscriptions", subscription);
        }
        
        [Serializable]
        public class BaseSubscription
        {
            public string type;
            public string version;

            public BaseSubscription(string type, string version = "1")
            {
                this.type = type;
                this.version = version;
            }
        }
        
        [Serializable]
        public class BaseCondition
        {
            public string broadcaster_user_id;

            public BaseCondition(string broadcaster_user_id)
            {
                this.broadcaster_user_id = broadcaster_user_id;
            }
        }
        
        [Serializable]
        public class Transport
        {
            public string method;
            public string session_id;

            public Transport(string sessionId)
            {
                method = "websocket";
                session_id = sessionId;
            }
        }

        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessage
        [Serializable]
        public class ReadChatSubscription : BaseSubscription
        {
            public static string subscriptionPermission = "user:read:chat";
            public Condition condition;
            public Transport transport;

            [Serializable]
            public class Condition : BaseCondition
            {
                public string user_id;

                public Condition(string broadcaster_user_id, string user_id) : base(broadcaster_user_id)
                {
                    this.user_id = user_id;
                }
            }

            public ReadChatSubscription(string sessionId, string broadcasterUserId, string userId) : base("channel.chat.message")
            {
                condition = new Condition(broadcasterUserId, userId);
                transport = new Transport(sessionId);
            }
        }

        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpollbegin
        [Serializable]
        public class BeginPollSubscription : BaseSubscription
        {
            public static string subscriptionPermission = TwitchAuthentication.ConnectionInformation.CHANNEL_MANAGE_POLLS;
            public BaseCondition condition;
            public Transport transport;

            public BeginPollSubscription(string sessionId, string broadcasterUserId) : base("channel.poll.begin")
            {
                condition = new BaseCondition(broadcasterUserId);
                transport = new Transport(sessionId);
            }
        }
    }
}