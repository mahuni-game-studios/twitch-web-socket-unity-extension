// © Copyright 2026 Mahuni Game Studios

namespace Mahuni.Twitch.Extension
{
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// A Twitch event subscription data class for predictions
    /// </summary>
    public static class PredictionSubscription
    {
        
        #region Lock
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpredictionlock
        public class Lock : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.prediction.lock";
            public Lock(string sessionId, string broadcasterUserId) : base(SUBSCRIPTION)
            {
                jsonObject = JObject.FromObject(new
                {
                    type = SUBSCRIPTION,
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
        
        //https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-prediction-lock-event
        public class LockEvent : TwitchSubscriptionEvent
        {
            public readonly string title;
            public readonly List<(string title, int channelPoints)> outcomes = new ();
            
            public LockEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken titleToken = root.SelectToken("title");
                if (titleToken != null)
                {
                    title = titleToken.ToString();
                }
                else
                {
                    Debug.LogError($"{nameof(LockEvent)}: Could not get title");
                    return;
                }
                
                List<JToken> outcomesList = root["outcomes"]?.Children().ToList();
                if (outcomesList == null || !outcomesList.Any())
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get outcomes array");
                    return;
                }

                foreach (JToken jToken in outcomesList)
                {
                    JObject entry = JObject.Parse(jToken.ToString());
                    JToken outcomeTitleToken = entry.SelectToken("title");
                    if (outcomeTitleToken == null)
                    {
                        Debug.LogError($"{nameof(LockEvent)}: Could not get outcome title");
                        return;
                    }
                    
                    JToken channelPointToken = entry.SelectToken("channel_points");
                    if (channelPointToken == null)
                    {
                        Debug.LogError($"{nameof(LockEvent)}: Could not get outcome title");
                        return;
                    }

                    outcomes.Add((outcomeTitleToken.ToString(), (int)channelPointToken));
                }
                
                onSubscriptionEvent?.Invoke(this);
            }
        }
        
        #endregion

        #region End
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpredictionend
        public class End : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.prediction.end";
            public End(string sessionId, string broadcasterUserId) : base(SUBSCRIPTION)
            {
                jsonObject = JObject.FromObject(new
                {
                    type = SUBSCRIPTION,
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
        
        //https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-prediction-end-event
        public class EndEvent : TwitchSubscriptionEvent
        {
            public readonly string title;
            public readonly string winningOutcomeId;
            
            public EndEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken titleToken = root.SelectToken("title");
                if (titleToken != null)
                {
                    title = titleToken.ToString();
                }
                else
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get title");
                    return;
                }
                
                JToken winningOutcomeToken = root.SelectToken("winning_outcome_id");
                if (winningOutcomeToken != null)
                {
                    winningOutcomeId = winningOutcomeToken.ToString();
                }
                else
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get winning outcome");
                    return;
                }
              
                List<JToken> outcomes = root["outcomes"]?.Children().ToList();
                if (outcomes == null || !outcomes.Any())
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get outcomes array");
                    return;
                }

                foreach (JToken jToken in outcomes)
                {
                    JObject entry = JObject.Parse(jToken.ToString());
                    JToken outcomeTitleToken = entry.SelectToken("title");
                    JToken userToken = entry.SelectToken("users");
                }
                
                onSubscriptionEvent?.Invoke(this);
            }
        }
        
        #endregion
    }
}