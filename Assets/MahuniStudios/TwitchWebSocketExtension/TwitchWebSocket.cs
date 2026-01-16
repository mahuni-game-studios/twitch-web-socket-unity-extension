// © Copyright 2026 Mahuni Game Studios

// ReSharper disable InconsistentNaming

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
        public event Action OnSession; // TODO: this does not work as invoking it from the thread blocks the listening method!
        public string SessionId { get; private set; }
        public bool Connected => socket is { State: WebSocketState.Open };

        private ClientWebSocket socket;
        private CancellationTokenSource cancellationToken;
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
            
            if (socket != null)
            {
                if (socket.State == WebSocketState.Open)
                {
                    Debug.Log("TwitchWebSocket: Connected.");
                    return;
                }
                
                socket.Dispose();
            }
            
            socket = new ClientWebSocket();
            cancellationToken?.Dispose();
            cancellationToken = new CancellationTokenSource();
            
            await socket.ConnectAsync(new Uri(twitchWebSocketUrl + keepAliveTimeout), cancellationToken.Token);
            Debug.Log("TwitchWebSocket: Connected.");
            
            await Task.Factory.StartNew(ReceiveLoop, cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
            MemoryStream outputStream = null;
            byte[] buffer = new byte[receiveBufferSize];

            try
            {
                while (socket != null && !cancellationToken.Token.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(receiveBufferSize);
                    
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        receiveResult = await socket.ReceiveAsync(buffer, cancellationToken.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                        {
                            outputStream.Write(buffer, 0, receiveResult.Count);
                        }
                    } 
                    while (!receiveResult.EndOfMessage);

                    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#close-message
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.LogWarning($"TwitchWebSocket: Closing connection with status code '{receiveResult.CloseStatus.ToString()}' ({receiveResult.CloseStatus.Value}), Description '{receiveResult.CloseStatusDescription}'.");
                        break;
                    }
                    
                    OnResponseReceived(outputStream);
                }
            }
            catch (TaskCanceledException e)
            {
                Debug.LogError($"TwitchWebSocket: Receive loop exception --> {e}");
            }
            finally
            {
                outputStream?.Dispose();
                Debug.Log("TwitchWebSocket: Receive loop stopped.");
            }
        }

        /// <summary>
        /// Data was received from the web socket
        /// </summary>
        /// <param name="stream">The stream holding the data</param>
        private void OnResponseReceived(Stream stream)
        {
            stream.Position = 0;
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

            string data = reader.ReadToEnd();
            MessageType messageType = GetMessageType(data);
            Debug.Log($"TwitchWebSocket: {messageType.ToString().ToUpper()}: '{data}'");

            switch (messageType)
            {
                case MessageType.session_welcome:
                    TwitchWelcomeMessage welcome = GetWelcomeMessage(data);
                    SessionId = welcome.payload.session.id;
                    Debug.Log($"TwitchWebSocket: Session ID: '{SessionId}'");
                    
                    // TODO: seems like invoke is synchronous and blocks the listening methods?! Also BeginInvoke does not solve the issue...
                    // OnSession?.BeginInvoke(OnSession.EndInvoke, null);
                    break;
                case MessageType.session_keepalive:
                    TwitchKeepaliveMessage keepalive = GetKeepAliveMessage(data);
                    break;
                case MessageType.session_reconnect:
                    TwitchReconnectMessage reconnect = GetReconnectMessage(data);
                    break;
                case MessageType.notification:
                    TwitchNotificationMessage notification = GetNotificationMessage(data);
                    break;
                case MessageType.revocation:
                    TwitchRevocationMessage revokation = GetRevocationMessage(data);
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
                TwitchMessage message = JsonUtility.FromJson<TwitchMessage>(data);
                string messageType = message.metadata.message_type;
                
                if (messageType.Equals(MessageType.session_welcome.ToString())) return MessageType.session_welcome;
                if (messageType.Equals(MessageType.session_keepalive.ToString())) return MessageType.session_keepalive;
                if (messageType.Equals(MessageType.session_reconnect.ToString())) return MessageType.session_reconnect;
                if (messageType.Equals(MessageType.notification.ToString())) return MessageType.notification;
                if (messageType.Equals(MessageType.revocation.ToString())) return MessageType.revocation;
                
                Debug.LogError($"TwitchWebSocket: Message string does not match any known message type! Passed string: '{data}'");
            }
            catch (Exception e)
            {
                Debug.LogError($"TwitchWebSocket: Exception while trying to parse message to JSON: {e}");
            }
            
            return MessageType.revocation;
        }

        /// <summary>
        /// Get the welcome message class from the raw message data
        /// </summary>
        /// <param name="message">The raw message to interpret</param>
        /// <returns>The message as class object when successful, else returns null</returns>
        private static TwitchWelcomeMessage GetWelcomeMessage(string message)
        {
            try
            {
                TwitchWelcomeMessage result = JsonUtility.FromJson<TwitchWelcomeMessage>(message);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TwitchWebSocket: Error while trying to parse welcome message to JSON: {e}");
            }

            return null;
        }
        
        /// <summary>
        /// Get the keep alive message class from the raw message data
        /// </summary>
        /// <param name="message">The raw message to interpret</param>
        /// <returns>The message as class object when successful, else returns null</returns>
        private static TwitchKeepaliveMessage GetKeepAliveMessage(string message)
        {
            try
            {
                TwitchKeepaliveMessage result = JsonUtility.FromJson<TwitchKeepaliveMessage>(message);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TwitchWebSocket: Error while trying to parse welcome message to JSON: {e}");
            }

            return null;
        }
        
        /// <summary>
        /// Get the notification message class from the raw message data
        /// </summary>
        /// <param name="message">The raw message to interpret</param>
        /// <returns>The message as class object when successful, else returns null</returns>
        private static TwitchNotificationMessage GetNotificationMessage(string message)
        {
            try
            {
                TwitchNotificationMessage result = JsonUtility.FromJson<TwitchNotificationMessage>(message);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TwitchWebSocket: Error while trying to parse welcome message to JSON: {e}");
            }

            return null;
        }
        
        /// <summary>
        /// Get the reconnect message class from the raw message data
        /// </summary>
        /// <param name="message">The raw message to interpret</param>
        /// <returns>The message as class object when successful, else returns null</returns>
        private static TwitchReconnectMessage GetReconnectMessage(string message)
        {
            try
            {
                TwitchReconnectMessage result = JsonUtility.FromJson<TwitchReconnectMessage>(message);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TwitchWebSocket: Error while trying to parse welcome message to JSON: {e}");
            }

            return null;
        }
        
        /// <summary>
        /// Get the revocation message class from the raw message data
        /// </summary>
        /// <param name="message">The raw message to interpret</param>
        /// <returns>The message as class object when successful, else returns null</returns>
        private static TwitchRevocationMessage GetRevocationMessage(string message)
        {
            try
            {
                TwitchRevocationMessage result = JsonUtility.FromJson<TwitchRevocationMessage>(message);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"TwitchWebSocket: Error while trying to parse welcome message to JSON: {e}");
            }

            return null;
        }
    }

    #region Message classes
    
    /// <summary>
    /// Each Twitch message contains the metadata which we can use to get the message type
    /// </summary>
    [Serializable]
    public class TwitchMessage
    {
        public BaseMetaData metadata;
    }

    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events#welcome-message
    [Serializable]
    public class TwitchWelcomeMessage
    {
        public BaseMetaData metadata;
        public SessionPayload payload;
    }
    
    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#keepalive-message
    [Serializable]
    public class TwitchKeepaliveMessage
    {
        public BaseMetaData metadata;
        public BasePayload payload;
    }
    
    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#notification-message
    [Serializable]
    public class TwitchNotificationMessage
    {
        public SubscriptionMetaData metadata;
        public SubscriptionEventPayload payload;
    }
    
    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#reconnect-message
    [Serializable]
    public class TwitchReconnectMessage
    {
        public BaseMetaData metadata;
        public SessionPayload payload;
    }
    
    // https://dev.twitch.tv/docs/eventsub/handling-websocket-events/#revocation-message
    [Serializable]
    public class TwitchRevocationMessage
    {
        public SubscriptionMetaData metadata;
        public SubscriptionPayload payload;
    }
    
    [Serializable]
    public class BaseMetaData
    {
        public string message_id;
        public string message_type;
        public string message_timestamp;
    }

    [Serializable]
    public class SubscriptionMetaData : BaseMetaData
    {
        public string subscription_type;
        public string subscription_version;
    }
    
    [Serializable]
    public class BasePayload
    {
    }
    
    [Serializable]
    public class SessionPayload : BasePayload
    {
        public Session session;
        
        [Serializable]
        public class Session
        {
            public string id;
            public string status;
            public string connected_at;
            public int keepalive_timeout_seconds;
            public string reconnect_url;
        }
    }
    
    [Serializable]
    public class SubscriptionPayload : BasePayload
    {
        public Subscription subscription;

        [Serializable]
        public class Subscription
        {
            public string id;
            public string status;
            public string type;
            public string version;
            public int cost;
            public Condition condition;
            public Transport transport;
            public string created_at;
            
            [Serializable]
            public class Condition
            {
                public string broadcaster_user_id;
            }

            [Serializable]
            public class Transport
            {
                public string method;
                public string session_id;
            }
        }
    }

    [Serializable]
    public class SubscriptionEventPayload : SubscriptionPayload
    {
        public Event @event; // we need to use the prefix @ to allow the variable name to be called "event"
        
        [Serializable]
        public class Event
        {
            public string user_id;
            public string user_login;
            public string user_name;
            public string broadcaster_user_id;
            public string broadcaster_user_login;
            public string broadcaster_user_name;
            public string followed_at;
        }
    }

    #endregion
}