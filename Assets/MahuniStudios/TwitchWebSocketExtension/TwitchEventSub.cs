// © Copyright 2026 Mahuni Game Studios

// ReSharper disable InconsistentNaming

namespace Mahuni.Twitch.Extension
{
    using System.Threading.Tasks;
    
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
    }
}