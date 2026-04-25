// © Copyright 2026 Mahuni Game Studios

namespace Mahuni.Twitch.Extension
{
    using Newtonsoft.Json.Linq;
    using UnityEngine;
    
    /// <summary>
    /// A Twitch event subscription data class for rewards
    /// </summary>
    public static class RewardSubscription
    {
        # region Redemption Add

        // https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types#channelchannel_points_custom_reward_redemptionadd
        public class RedemptionAdd : TwitchSubscription
        {
            public const string SUBSCRIPTION = "channel.channel_points_custom_reward_redemption.add";
            public RedemptionAdd(string sessionId, string broadcasterUserId) : base(SUBSCRIPTION)
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

        // https://dev.twitch.tv/docs/eventsub/eventsub-reference/#channel-points-custom-reward-redemption-add-event
        public class RedemptionAddEvent : TwitchSubscriptionEvent
        {
            public readonly string id;
            public readonly string userName;
            public readonly string userInput;
            public readonly Reward reward;

            public RedemptionAddEvent(string data)
            {
                JObject root = JObject.Parse(data);
                
                JToken idToken = root.SelectToken("id");
                if (idToken == null)
                {
                    Debug.LogError($"{nameof(RedemptionAddEvent)}: Could not get redemption ID");
                    return;
                }
                id = idToken.ToString();

                JToken userNameToken = root.SelectToken("user_name");
                if (userNameToken == null)
                {
                    Debug.LogError($"{nameof(RedemptionAddEvent)}: Could not get user name");
                    return;
                }
                userName = userNameToken.ToString();
                
                JToken userInputToken = root.SelectToken("user_input");
                if (userInputToken == null)
                {
                    Debug.LogError($"{nameof(RedemptionAddEvent)}: Could not get user input");
                    return;
                }
                userInput = userInputToken.ToString();
                
                JToken rewardIdToken = root.SelectToken("reward.id");
                JToken rewardTitleToken = root.SelectToken("reward.title");
                JToken rewardCostToken = root.SelectToken("reward.cost");
                JToken rewardPromptToken = root.SelectToken("reward.prompt");
                if (rewardIdToken == null || rewardTitleToken == null || rewardCostToken == null || rewardPromptToken == null)
                {
                    Debug.LogError($"{nameof(RedemptionAddEvent)}: Could not get reward object");
                    return;
                }
                reward = new Reward
                {
                    id = rewardIdToken.ToString(),
                    title = rewardTitleToken.ToString(),
                    cost = rewardCostToken.ToObject<int>(),
                    prompt = rewardPromptToken.ToString()
                };
                
                onSubscriptionEvent?.Invoke(this);
            }
        }

        #endregion
    }
}