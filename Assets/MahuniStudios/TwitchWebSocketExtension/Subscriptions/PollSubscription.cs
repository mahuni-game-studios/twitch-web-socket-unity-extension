// © Copyright 2026 Mahuni Game Studios

namespace Mahuni.Twitch.Extension
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A Twitch event subscription data class for polls
    /// </summary>
    public static class PollSubscription
    {
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
    }
}