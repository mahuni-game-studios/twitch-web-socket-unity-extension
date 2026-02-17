// © Copyright 2026 Mahuni Game Studios

namespace Mahuni.Twitch.Extension
{
    using Newtonsoft.Json.Linq;
    using UnityEngine;
    
    /// <summary>
    /// A Twitch event subscription data class for chat
    /// </summary>
    public static class ChatSubscription
    {
        #region Read
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelchatmessage
        public class Read : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.chat.message";
            public Read(string sessionId, string broadcasterUserId, string userId) : base(SUBSCRIPTION)
            {
                jsonObject = JObject.FromObject(new
                {
                    type = SUBSCRIPTION,
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
        
        //https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-chat-message-event
        public class ReadEvent : TwitchSubscriptionEvent
        {
            public readonly string userName;
            public readonly string message;
            
            public ReadEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken nameToken = root.SelectToken("chatter_user_name");
                if (nameToken != null)
                {
                    userName = nameToken.ToString();
                }
                else
                {
                    Debug.LogError($"{nameof(ReadEvent)}: Could not get user name");
                    return;
                }
                
                JToken msgToken = root.SelectToken("message.text");
                if (msgToken != null)
                {
                    message = msgToken.ToString();
                }
                else
                {
                    Debug.LogError($"{nameof(ReadEvent)}: Could not get message");
                    return;
                }
                
                onSubscriptionEvent?.Invoke(this);
            }
        }
        
        #endregion
    }
}