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
        #region Progress
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpredictionprogress
        public class Progress : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.prediction.progress";
            public Progress(string sessionId, string broadcasterUserId) : base(SUBSCRIPTION)
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

        // https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-prediction-progress-event
        public class ProgressEvent : TwitchSubscriptionEvent
        {
            public readonly List<Prediction.Outcome> outcomes = new();

            public ProgressEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                List<JToken> outcomesToken = root.SelectToken("outcomes")?.Children().ToList();
                if (outcomesToken == null || !outcomesToken.Any())
                {
                    Debug.LogError($"{nameof(ProgressEvent)}: Could not get outcomes array");
                    return;
                }
                outcomes = GetOutcomes(outcomesToken);
                
                onSubscriptionEvent?.Invoke(this);
            }
        }

        #endregion
        
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
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-prediction-lock-event
        public class LockEvent : TwitchSubscriptionEvent
        {
            public readonly string title;
            public readonly List<Prediction.Outcome> outcomes = new();
            
            public LockEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken titleToken = root.SelectToken("title");
                if (titleToken == null)
                {
                    Debug.LogError($"{nameof(LockEvent)}: Could not get title");
                    return;
                }
                title = titleToken.ToString();
                
                List<JToken> outcomesToken = root.SelectToken("outcomes")?.Children().ToList();
                if (outcomesToken == null || !outcomesToken.Any())
                {
                    Debug.LogError($"{nameof(LockEvent)}: Could not get outcomes array");
                    return;
                }
                outcomes = GetOutcomes(outcomesToken);
                
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
            public readonly List<Prediction.Outcome> outcomes = new();
            
            public EndEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken titleToken = root.SelectToken("title");
                if (titleToken == null)
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get title");
                    return;
                }
                title = titleToken.ToString();
                
                JToken winningOutcomeToken = root.SelectToken("winning_outcome_id");
                if (winningOutcomeToken == null)
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get winning outcome");
                    return;
                }
                winningOutcomeId = winningOutcomeToken.ToString();
              
                List<JToken> outcomesToken = root.SelectToken("outcomes")?.Children().ToList();
                if (outcomesToken == null || !outcomesToken.Any())
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get outcomes array");
                    return;
                }
                outcomes = GetOutcomes(outcomesToken);

                onSubscriptionEvent?.Invoke(this);
            }
        }
        
        #endregion

        #region Helpers

        private static List<Prediction.Outcome> GetOutcomes(List<JToken> outcomes)
        {
            List<Prediction.Outcome> result = new();
            foreach (JToken jToken in outcomes)
            {
                JObject entry = JObject.Parse(jToken.ToString());

                JToken outcomeTitleToken = entry.SelectToken("title");
                if (outcomeTitleToken == null)
                {
                    Debug.LogError($"{nameof(PredictionSubscription)}: Could not get outcome title");
                    return result;
                }

                JToken channelPointToken = entry.SelectToken("channel_points");
                if (channelPointToken == null)
                {
                    Debug.LogError($"{nameof(PredictionSubscription)}: Could not get outcome title");
                    return result;
                }

                Prediction.Outcome outcome = new()
                {
                    title = outcomeTitleToken.ToString(),
                    channel_points = (int)channelPointToken
                };
                
                result.Add(outcome);
            }
            
            return result;
        }

        #endregion
    }
}