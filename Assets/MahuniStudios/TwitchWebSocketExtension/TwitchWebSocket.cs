// © Copyright 2026 Mahuni Game Studios

// ReSharper disable InconsistentNaming

using Newtonsoft.Json.Linq;

namespace Mahuni.Twitch.Extension
{
    using System;
    using System.Net.WebSockets;
    using System.Threading;
    using UnityEngine;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A class to connect and handle a web socket communicating to the Twitch API
    /// --> https://dev.twitch.tv/docs/eventsub/handling-websocket-events/
    /// </summary>
    public class TwitchWebSocket
    {
        public event Action OnSession;
        public event Action<TwitchNotificationMessage> OnNotification;
        public string SessionId { get; private set; }
        public bool Connected => socket is { State: WebSocketState.Open };

        private ClientWebSocket socket;
        private CancellationTokenSource cancellationToken;
        private static SynchronizationContext unityContext;
        private const string twitchWebSocketUrl = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=";
        private const int keepAliveTimeout = 30;
        private const int receiveBufferSize = 8192;

        private enum MessageType
        {
            session_welcome,
            session_keepalive,
            session_reconnect,
            notification,
            revocation
        }

        /// <summary>
        /// Constructor, connect the web socket on instantiation
        /// </summary>
        public TwitchWebSocket()
        {
            _ = Connect();
        }

        /// <summary>
        /// Connect the web socket to the Twitch API
        /// </summary>
        public async Task Connect()
        {
            Debug.Log("TwitchWebSocket: Connecting...");

            unityContext ??= SynchronizationContext.Current;

            socket?.Dispose();
            socket = new ClientWebSocket();

            cancellationToken?.Dispose();
            cancellationToken = new CancellationTokenSource();

            await socket.ConnectAsync(new Uri(twitchWebSocketUrl + keepAliveTimeout), cancellationToken.Token).ConfigureAwait(false);
            Debug.Log("TwitchWebSocket: Connected.");

            _ = Task.Run(ReceiveLoop);
        }

        /// <summary>
        /// Disconnect the web socket
        /// </summary>
        public async Task Disconnect()
        {
            if (socket == null)
            {
                Debug.LogError("TwitchWebSocket: Cannot disconnect when web socket is null.");
                return;
            }

            Debug.Log("TwitchWebSocket: Disconnecting...");

            if (socket.State == WebSocketState.Open)
            {
                cancellationToken.CancelAfter(TimeSpan.FromSeconds(2));
                await socket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }

            socket.Dispose();
            socket = null;
            cancellationToken.Dispose();
            cancellationToken = null;

            Debug.Log("TwitchWebSocket: Disconnected.");
        }
        
        /// <summary>
        /// Read the incoming web socket data while connected 
        /// </summary>
        private async Task ReceiveLoop()
        {
            Debug.Log("TwitchWebSocket: Receive loop started.");
            byte[] buffer = new byte[receiveBufferSize];

            try
            {
                while (!cancellationToken.Token.IsCancellationRequested)
                {
                    using MemoryStream outputStream = new(receiveBufferSize);
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, cancellationToken.Token).ConfigureAwait(false);
                        if (result.MessageType != WebSocketMessageType.Close)
                        {
                            outputStream.Write(buffer, 0, result.Count);
                        }
                    } 
                    while (!result.EndOfMessage);

                    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#close-message
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.LogWarning($"TwitchWebSocket: Closing connection with status code '{result.CloseStatus.ToString()}' ({result.CloseStatus.Value}), Description '{result.CloseStatusDescription}'.");
                        break;
                    }

                    OnResponseReceived(outputStream);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"TwitchWebSocket: Receive loop error: {ex}");
            }

            Debug.Log("TwitchWebSocket: Receive loop stopped.");
        }

        /// <summary>
        /// Data was received from the web socket
        /// </summary>
        /// <param name="stream">The stream holding the data</param>
        private void OnResponseReceived(Stream stream)
        {
            stream.Position = 0;
            using StreamReader reader = new(stream, Encoding.UTF8);

            string data = reader.ReadToEnd();
            MessageType messageType = GetMessageType(data);
            Debug.Log($"TwitchWebSocket: {messageType.ToString().ToUpper()}: '{data}'");

            switch (messageType)
            {
                case MessageType.session_welcome:
                    TwitchWelcomeMessage welcome = new(data);
                    SessionId = welcome.sessionId;
                    // Pass information to the Unity thread
                    unityContext.Post(_ => { OnSession?.Invoke(); }, null);
                    break;
                case MessageType.session_keepalive:
                    TwitchKeepaliveMessage keepalive = new(data);
                    break;
                case MessageType.session_reconnect:
                    TwitchReconnectMessage reconnect = new(data);
                    break;
                case MessageType.notification:
                    TwitchNotificationMessage notificationMessage = new(data);
                    // Pass information to the Unity thread
                    unityContext.Post(_ => { OnNotification?.Invoke(notificationMessage); }, null);
                    break;
                case MessageType.revocation:
                    TwitchRevocationMessage revocation = new(data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            stream.Dispose();
        }

        /// <summary>
        /// Get the type of the message which can be found in the metadata
        /// </summary>
        /// <param name="data">The data to interpret</param>
        /// <returns>The type of message that could be read out of the data</returns>
        private static MessageType GetMessageType(string data)
        {
            try
            {
                JObject root = JObject.Parse(data);
                JToken typeToken = root.SelectToken("metadata.message_type");
                string messageType = typeToken == null ? MessageType.revocation.ToString() : typeToken.ToString();
                return Enum.TryParse(messageType, out MessageType type) ? type : MessageType.revocation;
            }
            catch (Exception e)
            {
                Debug.LogError($"TwitchWebSocket: Exception while trying to parse message to JSON: {e}");
            }

            return MessageType.revocation;
        }
    }

    #region Message classes

    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events#welcome-message
    public class TwitchWelcomeMessage
    {
        public readonly string sessionId;

        public TwitchWelcomeMessage(string content)
        {
            JObject root = JObject.Parse(content);
            JToken sessionToken = root.SelectToken("payload.session.id");
            if (sessionToken != null)
            {
                sessionId = sessionToken.ToString();
            }
            else
            {
                Debug.LogError($"TwitchWebSocket: Could not get event content from TwitchWelcomeMessage");
            }
        }
    }

    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#keepalive-message
    public class TwitchKeepaliveMessage
    {
        public TwitchKeepaliveMessage(string content)
        {
            JObject root = JObject.Parse(content);
            //JToken token = root.SelectToken("metadata");
        }
    }

    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#notification-message
    public class TwitchNotificationMessage
    {
        public readonly string subscriptionType;
        public readonly string eventContent;

        public TwitchNotificationMessage(string content)
        {
            JObject root = JObject.Parse(content);

            JToken typeToken = root.SelectToken("metadata.subscription_type");
            if (typeToken != null)
            {
                subscriptionType = typeToken.ToString();
            }
            else
            {
                Debug.LogError($"TwitchWebSocket: Could not get subscription type from TwitchNotificationMessage");
            }

            JToken eventToken = root.SelectToken("payload.event");
            if (eventToken != null)
            {
                eventContent = eventToken.ToString();
            }
            else
            {
                Debug.LogError($"TwitchWebSocket: Could not get event content from TwitchNotificationMessage");
            }
        }
    }

    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#reconnect-message
    public class TwitchReconnectMessage
    {
        public TwitchReconnectMessage(string content)
        {
            JObject root = JObject.Parse(content);
            //JToken token = root.SelectToken("metadata");
        }
    }

    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#revocation-message
    public class TwitchRevocationMessage
    {
        public TwitchRevocationMessage(string content)
        {
            JObject root = JObject.Parse(content);
            //JToken token = root.SelectToken("metadata");
        }
    }

    #endregion
}