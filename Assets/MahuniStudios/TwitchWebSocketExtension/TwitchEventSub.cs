// © Copyright 2026 Mahuni Game Studios

// ReSharper disable InconsistentNaming

using Newtonsoft.Json.Linq;

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

        #region Subscriptions
        
        public class TwitchSubscription
        {
            protected JObject jsonObject;
            public string ToJson()
            {
                return jsonObject.ToString();
            }
        }
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessage
        public class ChatReadSubscription : TwitchSubscription
        {
            public const string subscriptionType = "channel.chat.message";
            public ChatReadSubscription(string sessionId, string broadcasterUserId, string userId)
            {
                jsonObject = JObject.FromObject(new
                {
                    type = subscriptionType,
                    version = "1",
                    condition = new
                    {
                        broadcaster_user_id = broadcasterUserId,
                        user_id = userId
                    },
                    transport = new
                    {
                        method = "websocket",
                        session_id = sessionId
                    }
                });
            }
        }
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpollbegin
        public class PollBeginSubscription : TwitchSubscription
        {
            public const string subscriptionType = "channel.poll.begin";
            public PollBeginSubscription(string sessionId, string broadcasterUserId)
            {
                jsonObject = JObject.FromObject(new
                {
                    type = subscriptionType,
                    version = "1",
                    condition = new
                    {
                        broadcaster_user_id = broadcasterUserId
                    },
                    transport = new
                    {
                        method = "websocket",
                        session_id = sessionId
                    }
                });
            }
        }
        
        #endregion

        #region Notification Events

        //https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-chat-message-event
        public class ChatReadEvent
        {
            public readonly string userName;
            public readonly string message;
            
            public ChatReadEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken nameToken = root.SelectToken("chatter_user_name");
                if (nameToken != null)
                {
                    userName = nameToken.ToString();
                }
                else
                {
                    Debug.LogError($"TwitchEventSub: Could not get chatter name from ChatReadSubscription");
                }
                
                JToken msgToken = root.SelectToken("message.text");
                if (msgToken != null)
                {
                    message = msgToken.ToString();
                }
                else
                {
                    Debug.LogError($"TwitchEventSub: Could not get chatter message from ChatReadSubscription");
                }
            }
        }

        #endregion
    }
}