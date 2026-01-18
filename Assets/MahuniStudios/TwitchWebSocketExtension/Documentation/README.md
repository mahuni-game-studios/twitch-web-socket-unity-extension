# Unity Twitch Web Socket Extension by Mahuni Game Studios

[![Downloads](https://img.shields.io/github/downloads/mahuni-game-studios/twitch-web-socket-unity-extension/total.svg)](https://github.com/mahuni-game-studios/twitch-web-socket-unity-extension/releases/)
[![Latest Version](https://img.shields.io/github/v/release/mahuni-game-studios/twitch-web-socket-unity-extension)](https://github.com/mahuni-game-studios/twitch-web-socket-unity-extension/releases/tag/v1.0)

An extension to connect to the Twitch EventSub system via web socket in Unity. Including an example implementation showing websocket connection, disconnection and a subscription to reading chat.

## Code Snippet Examples

The simplest implementation to give your game permission to access and use the Twitch EventSub system!

### Authentication

The authentication logic is packed together with the Unity web request logic in a git submodule and is needed if you want to use the extension as is. Read [this](#twitch-web-request-extension) to find out how to get it.

```cs
public class YourUnityClass : MonoBehaviour
{
    private void Start()
    {        
        // Register to authentication finished event
        TwitchAuthentication.OnAuthenticated += OnAuthenticated;
        
        // Set relevant information to the connection
        TwitchAuthentication.ConnectionInformation infos = new("your-client-id", new List<string>(){TwitchEventSub.ReadChatSubscription.subscriptionPermission});
        
        // Start authentication
        TwitchAuthentication.StartAuthenticationValidation(this, infos);
    }
    
    private void OnAuthenticated(bool success)
    {
        if (success)
        {
            // TODO: Start connecting the web request handler and the web socket for to the Twitch EventSub from here!
        }
    }
}
```

### Websocket connection

```cs
public class YourUnityClass : MonoBehaviour
{   
    private TwitchWebSocket twitchWebSocket;
    
    private void Start()
    { 
        StartCoroutine(Connect());
    }
    
    private IEnumerator Connect()
    {
        twitchWebSocket = new TwitchWebSocket();
        
        // Wait for the web socket to connect
        while (string.IsNullOrEmpty(twitchWebSocket.SessionId) || !twitchWebSocket.Connected) yield return null;
        
        // TODO: Call your EventSub subscriptions from here!
    }
    
    private async void Disconnect()
    {
        if (twitchWebSocket == null) return;
        
        // Make sure to clean up the web socket, as it uses threads which would continue to run otherwise!
        await twitchWebSocket.Disconnect();
    }
}
```

### Eventsub subription

Please note that subscribing to the EventSub only works after successful authentication.

The Unity web request logic is packed in a git submodule and is needed if you want to use the extension as is. Read [this](#twitch-web-request-extension) to find out how to get it.

```cs
public class YourUnityClass : MonoBehaviour
{
    private TwitchWebRequests twitchWebRequests;
    private TwitchWebSocket twitchWebSocket;
    
    private void Start()
    { 
        // Connect to the web request class
        ConnectTwitchWebRequests(); 
    }
    
    private async void ConnectTwitchWebRequests()
    {
        twitchWebRequests = new TwitchWebRequests("channel-name");
        bool success = await twitchWebRequests.Connect("channel-name");
        if (success)
        {
            // Open a web socket connection
            StartCoroutine(ConnectWebSocket());
        }  
    }
    
    private IEnumerator ConnectWebSocket()
    {
        twitchWebSocket = new TwitchWebSocket();        
        while (string.IsNullOrEmpty(twitchWebSocket.SessionId) || !twitchWebSocket.Connected) yield return null;
        
        // Call your EventSub subscriptions from here!
        // You have 10 seconds time until the connection will be closed automatically        
        Subscribe();
    }
      
    private async void Subscribe()
    {
        // Create the subscription request
        TwitchEventSub.ReadChatSubscription readChatReadSubscription = new(twitchWebSocket.SessionId, twitchWebRequestHandler.BroadcasterID, twitchWebRequestHandler.BroadcasterID);
        
        var response = await TwitchEventSub.Subscribe(JsonUtility.ToJson(readChatReadSubscription));
        if (response.responseCode == TwitchResponseCode.ACCEPTED)
        {
            // The subscription was accepted, notifications will now be pushed to the web socket
        }
    } 
}
```

## Installation Guide

### Prerequisites

To be able to interact with the Twitch API, you need to register your Twitch application. You can follow how to do that with this [Guide from Twitch](https://dev.twitch.tv/docs/authentication/register-app/). In short:

1. Sign up to Twitch if you don't have already
2. Navigate to the [Twitch Developer Console](https://dev.twitch.tv/console/apps)
3. Create a new application and select an appropriate category, e.g. as Game Integration
4. Click on *Manage* on your application entry and you will be presented a `Client ID`. This ID will be needed to interact with Twitch.

<font color="red">The `Client ID` should stay secret, do not share or show it!</font>

#### Twitch Web Request Extension

This repository uses the [Unity Twitch Web Request Extension by Mahuni Game Studios](https://github.com/mahuni-game-studios/twitch-web-request-unity-extension) as git submodule. Be sure to either pull the submodule or grab / download / clone the extension manually.

- To clone the repository with submodules: `git clone --recurse-submodules`
- To update the cloned repository to get the submodules: `git submodule update --init --recursive`
- To download the extension, go to [GitHub](https://github.com/mahuni-game-studios/twitch-authentication-unity-extension), download it and drag and drop it somewhere into the `Assets/` folder

#### Demo scene

To use the provided `TwitchWebSocketExtension_Demo` scene, the `TextMeshPro` package is required. If you do not have it yet imported into your project, simply opening the `TwitchWebSocketExtension_Demo.scene` will ask if you want to import it. Select the `Import TMP Essentials` option, close the `TMP Importer` and you are good to go.

### Setup project
1. Either open this project directly or import it to your own project in the Unity Editor
2. Make sure the git submodules are installed, see [here](#twitch-web-request-extension)
3. Start using the `TwitchWebSocket` and the `TwitchEventSub` scripts right away, or take a look into the `TwitchWebSocketExtension_Demo` scene to find an easy example implementation.