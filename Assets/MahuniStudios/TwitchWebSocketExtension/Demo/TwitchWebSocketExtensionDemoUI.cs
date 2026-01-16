// © Copyright 2026 Mahuni Game Studios

using System.Collections;
using System.Collections.Generic;
using Mahuni.Twitch.Extension;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A class to demonstrate and test the web socket connecting to the Twitch API.
/// Use it together with the TwitchWebSocketExtension_Demo scene.
/// </summary>
public class TwitchWebSocketExtensionUI : MonoBehaviour
{
    [Header("Authentication")] 
    public TMP_InputField channelNameText;
    public TMP_InputField twitchClientIdText;
    public TextMeshProUGUI authenticationDescriptionText;
    public Button authenticateButton;

    [Header("Web Socket")] 
    public Button webSocketConnectButton;
    public Button webSocketDisconnectButton;

    private TwitchWebRequestHandler twitchWebRequestHandler;
    private TwitchWebSocket twitchWebSocket;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called for the first time.
    /// </summary>
    private void Start()
    {
        Application.targetFrameRate = 60;

        TwitchAuthentication.OnAuthenticated += OnAuthenticated;
        TwitchAuthentication.Reset(); // If you authenticated before, it might be better to reset the token, to be sure the right permissions are set

        channelNameText.onValueChanged.AddListener(ValidateFields);
        twitchClientIdText.onValueChanged.AddListener(ValidateFields);
        authenticationDescriptionText.text = "Enter the channel name and your Twitch client ID to authenticate.";
        authenticateButton.onClick.AddListener(OnAuthenticationButtonClicked);
        webSocketConnectButton.onClick.AddListener(OnConnectWebSocket);
        webSocketDisconnectButton.onClick.AddListener(OnDisconnectWebSocket);

        ValidateFields();
    }

    /// <summary>
    /// Destroying the attached behaviour will result in the game or Scene receiving OnDestroy
    /// </summary>
    private void OnDestroy()
    {
        // Make sure to clean up the web socket, as it uses threads which would continue to run otherwise!
        OnDisconnectWebSocket();
    }

    #region Authentication

    /// <summary>
    /// The authentication button was clicked by the user
    /// </summary>
    private void OnAuthenticationButtonClicked()
    {
        authenticationDescriptionText.text = "<color=\"orange\">Authentication ongoing...";
        TwitchAuthentication.ConnectionInformation infos = new(twitchClientIdText.text, new List<string> { TwitchEventSub.ReadChatSubscription.subscriptionPermission });
        TwitchAuthentication.StartAuthenticationValidation(this, infos);
    }

    /// <summary>
    /// The authentication returned with a result
    /// </summary>
    /// <param name="success">True if authentication was successful</param>
    private void OnAuthenticated(bool success)
    {
        if (success)
        {
            authenticationDescriptionText.text = "<color=\"green\">Authentication successful!";
            ConnectTwitchWebRequests();
        }
        else
        {
            authenticationDescriptionText.text = "<color=\"red\">Authentication failed!";
        }
    }

    #endregion

    #region Web Request Connection

    /// <summary>
    /// Connect to the web request class
    /// </summary>
    private async void ConnectTwitchWebRequests()
    {
        twitchWebRequestHandler = new TwitchWebRequestHandler(channelNameText.text);
        bool success = await twitchWebRequestHandler.Connect(channelNameText.text);
        if (!success)
        {
            Debug.LogError("<color=\"red\">Twitch web request connection was not established!");
            return;
        }

        ValidateFields();
    }

    #endregion

    #region Web Socket Connection

    /// <summary>
    /// The user clicked on the connect button
    /// </summary>
    private void OnConnectWebSocket()
    {
        webSocketConnectButton.interactable = false;
        StartCoroutine(EstablishSession());
    }

    /// <summary>
    /// Wait for the session to be established and make one subscription to keep the connection open
    /// </summary>
    private IEnumerator EstablishSession()
    {
        twitchWebSocket = new TwitchWebSocket();
        while (string.IsNullOrEmpty(twitchWebSocket.SessionId) || !twitchWebSocket.Connected) yield return null;
        ValidateFields();

        // At least one subscription needs to be sent in the first 10 seconds of connection, else the connection is closed automatically
        SubscribeToChat();
    }

    /// <summary>
    /// The user clicked on the disconnect button or the application is stopped
    /// </summary>
    private async void OnDisconnectWebSocket()
    {
        if (twitchWebSocket == null) return;
        await twitchWebSocket.Disconnect();
        ValidateFields();
    }

    #endregion

    #region EventSub

    /// <summary>
    /// Subscribe to the EventSub
    /// https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types#subscription-types
    /// </summary>
    private async void SubscribeToChat()
    {
        TwitchEventSub.ReadChatSubscription readChatReadSubscription = new(twitchWebSocket.SessionId, twitchWebRequestHandler.BroadcasterID, twitchWebRequestHandler.BroadcasterID);
        var response = await TwitchEventSub.Subscribe(JsonUtility.ToJson(readChatReadSubscription));
        if (response.responseCode == TwitchResponseCode.ACCEPTED)
        {
            // The subscription was accepted, notifications will now be pushed to the web socket
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Update is called every frame if the MonoBehaviour is enabled
    /// </summary>
    private void Update()
    {
        // Tab through formular
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (EventSystem.current.currentSelectedGameObject == null || EventSystem.current.currentSelectedGameObject == twitchClientIdText.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(channelNameText.gameObject, new BaseEventData(EventSystem.current));
            }
            else if (EventSystem.current.currentSelectedGameObject == channelNameText.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(twitchClientIdText.gameObject, new BaseEventData(EventSystem.current));
            }
        }
    }

    /// <summary>
    /// Validate the UI elements and their interactivity
    /// </summary>
    private void ValidateFields(string value = "")
    {
        bool isAuthenticated = TwitchAuthentication.IsAuthenticated();

        channelNameText.interactable = !isAuthenticated;
        twitchClientIdText.interactable = !isAuthenticated;
        authenticateButton.interactable = !isAuthenticated && !string.IsNullOrEmpty(channelNameText.text) && !string.IsNullOrEmpty(twitchClientIdText.text);
        webSocketConnectButton.interactable = isAuthenticated && !string.IsNullOrEmpty(twitchWebRequestHandler.BroadcasterID) && (twitchWebSocket == null || !twitchWebSocket.Connected);
        webSocketDisconnectButton.interactable = isAuthenticated && twitchWebSocket != null && twitchWebSocket.Connected;

        // Channel name or Client ID input is missing
        if (!isAuthenticated && (string.IsNullOrEmpty(channelNameText.text) || string.IsNullOrEmpty(twitchClientIdText.text)))
        {
            authenticationDescriptionText.text = "Enter the channel name and your Twitch client ID to authenticate.";
        }
    }

    #endregion
}