// © Copyright 2026 Mahuni Game Studios

namespace Mahuni.Twitch.Extension
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// A Twitch event subscription data class for polls
    /// </summary>
    public static class PollSubscription
    {
        #region Begin
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpollbegin
        public class Begin : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.poll.begin";
            public Begin(string sessionId, string broadcasterUserId) : base(SUBSCRIPTION)
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
        
        // todo begin event
        
        #endregion
        
        #region Progress
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpollprogress
        public class Progress : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.poll.progress";
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
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-poll-progress-event
        public class ProgressEvent : TwitchSubscriptionEvent
        {
            public readonly string title;
            public readonly List<Poll.Choices> choices = new();

            public ProgressEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken titleToken = root.SelectToken("title");
                if (titleToken == null)
                {
                    Debug.LogError($"{nameof(ProgressEvent)}: Could not get poll title");
                    return;
                }
                title = titleToken.ToString();
                
                List<JToken> choicesList = root["choices"]?.Children().ToList();
                if (choicesList == null || !choicesList.Any())
                {
                    Debug.LogError($"{nameof(ProgressEvent)}: Could not get choices array");
                    return;
                }

                foreach (JToken jToken in choicesList)
                {
                    JObject entry = JObject.Parse(jToken.ToString());
                    JToken choiceTitleToken = entry.SelectToken("title");
                    if (choiceTitleToken == null)
                    {
                        Debug.LogError($"{nameof(ProgressEvent)}: Could not get choice title");
                        return;
                    }
                    
                    JToken channelPointToken = entry.SelectToken("channel_points_votes");
                    if (channelPointToken == null)
                    {
                        Debug.LogError($"{nameof(ProgressEvent)}: Could not get channel points votes");
                        return;
                    }
                    
                    JToken votesToken = entry.SelectToken("votes");
                    if (votesToken == null)
                    {
                        Debug.LogError($"{nameof(ProgressEvent)}: Could not get votes");
                        return;
                    }

                    Poll.Choices choice = new()
                    {
                        title = choiceTitleToken.ToString(),
                        channel_points_votes = (int)channelPointToken,
                        votes = (int)votesToken
                    };
                    choices.Add(choice);
                }
                
                onSubscriptionEvent?.Invoke(this);
            }
        }
        
        #endregion

        #region End
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/#channelpollend
        public class End : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.poll.end";
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
        
        // https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-poll-end-event
        public class EndEvent : TwitchSubscriptionEvent
        {
            public readonly string title;
            public readonly List<Poll.Choices> choices = new();
            public readonly Poll.ChannelPointsVoting channelPointsVoting = new();

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

                List<JToken> choicesToken = root.SelectToken("choices")?.Children().ToList();
                if (choicesToken == null || !choicesToken.Any())
                {
                    Debug.LogError($"{nameof(EndEvent)}: Could not get choices array");
                    return;
                }

                foreach (JToken jToken in choicesToken)
                {
                    JObject entry = JObject.Parse(jToken.ToString());
                    JToken idToken = entry.SelectToken("id");
                    JToken choiceTitleToken = entry.SelectToken("title");
                    JToken channelPointVotesToken = entry.SelectToken("channel_points_votes");
                    JToken votesToken = entry.SelectToken("votes");
                    
                    Poll.Choices choice = new()
                    {
                        id = idToken.ToString(),
                        title = choiceTitleToken.ToString(),
                        votes = votesToken.ToObject<int>(),
                        channel_points_votes = channelPointVotesToken.ToObject<int>()
                    };
                    
                    choices.Add(choice);
                }
                
                JToken isEnabledToken = root.SelectToken("channel_points_voting.is_enabled");
                JToken voteAmountToken = root.SelectToken("channel_points_voting.amount_per_vote");
                channelPointsVoting = new Poll.ChannelPointsVoting
                {
                    isEnabled = isEnabledToken.ToObject<bool>(),
                    amountPerVote = voteAmountToken.ToObject<int>()
                };

                onSubscriptionEvent?.Invoke(this);
            }
        }

        #endregion
    }
}