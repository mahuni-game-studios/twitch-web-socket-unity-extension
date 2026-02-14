// © Copyright 2026 Mahuni Game Studios

// ReSharper disable InconsistentNaming
namespace Mahuni.Twitch.Extension
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    
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
        public static async Task<(TwitchResponseCode responseCode, string responseBody)> Subscribe(string subscription)
        {
            return await TwitchRequest.AsyncPost("eventsub/subscriptions", subscription).ConfigureAwait(false);
        }

        #region Subscriptions
        
        /// <summary>
        /// Base subscription class to inherit from for convenience
        /// </summary>
        public class TwitchSubscription
        {
            protected JObject jsonObject;
            public string ToJson()
            {
                return jsonObject.ToString();
            }
        }

        #region Chat Subscription
        
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
        
        #endregion

        #region Poll
        
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

        #region Prediction Subscription
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpredictionlock
        public class PredictionLockSubscription : TwitchSubscription
        {
            public const string subscriptionType = "channel.prediction.lock";
            public PredictionLockSubscription(string sessionId, string broadcasterUserId)
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
        
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpredictionend
        public class PredictionEndSubscription : TwitchSubscription
        {
            public const string subscriptionType = "channel.prediction.end";
            public PredictionEndSubscription(string sessionId, string broadcasterUserId)
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
        
        #endregion

        #region Notification Events

        #region Chat Events

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

        #region Prediction Events
        
        //https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-prediction-end-event
        public class PredictionEndEvent
        {
            public readonly string title;
            public readonly string winningOutcomeId;
            
            public PredictionEndEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken titleToken = root.SelectToken("title");
                if (titleToken != null)
                {
                    title = titleToken.ToString();
                }
                else
                {
                    Debug.LogError($"TwitchEventSub: Could not get title from PredictionEndEvent");
                }
                
                JToken winningOutcomeToken = root.SelectToken("winning_outcome_id");
                if (winningOutcomeToken != null)
                {
                    winningOutcomeId = winningOutcomeToken.ToString();
                }
                else
                {
                    Debug.LogError($"TwitchEventSub: Could not get winning outcome from PredictionEndEvent");
                }
              
                List<JToken> outcomes = root["outcomes"]?.Children().ToList();
                if (outcomes == null || !outcomes.Any())
                {
                    Debug.LogError($"TwitchEventSub: Could not get outcomes array from PredictionEndEvent");
                }
                else
                {
                    foreach (JToken jToken in outcomes)
                    {
                        JObject entry = JObject.Parse(jToken.ToString());
                        JToken titleToken2 = entry.SelectToken("title");
                        JToken userToken = entry.SelectToken("users");
                        Debug.Log($"Outcome with title {titleToken2} and was voted by {userToken} users.");
                    }
                }
            }
        }
        
        //https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-prediction-lock-event
        public class PredictionLockEvent
        {
            public readonly string title;
            public PredictionLockEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken titleToken = root.SelectToken("title");
                if (titleToken != null)
                {
                    title = titleToken.ToString();
                }
                else
                {
                    Debug.LogError($"TwitchEventSub: Could not get title from PredictionLockEvent");
                }
            }
        }
        
        #endregion

        #endregion
    }
}