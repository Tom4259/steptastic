using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LitJson;
using UnityEngine.Events;

public class ProcessDeepLinkMngr : MonoBehaviour
{
    public static ProcessDeepLinkMngr Instance { get; private set; }
    public string deeplinkURL = "unitydl://Steptastic";

    [Space]
    [TextArea]
    public string editorAuth;
    [TextArea]
    public string editorToken;
    [TextArea]
    public string editorRefresh;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                OnDeepLinkActivated(Application.absoluteURL);
            }
            // Initialize DeepLink Manager global variable.
            else deeplinkURL = "[none]";
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if(PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, "_steptastic_!") != "_steptastic_!")
        {
            if (editorToken != PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken))
            {
                editorToken = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);
            }
        }
    }


    /// <summary>
    /// when the user finishes authorizing my app, they will be directed to another site (which i have hosted) which 
    /// points and shows that the site is associated with my application
    /// </summary>
    //demo return link
    //https://steptastic-ad9d9.web.app/?code=4%2F0AfJohXlF9uJL5yPoEbD7LOZUhwzT5pIVfjN86bjd1kEWownIpAdUAcxrftkAo9Ky4op9Xg&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Ffitness.activity.read
    public void OnDeepLinkActivated(string url)
    {
        // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
        deeplinkURL = url;

        Debug.Log("[" + GetType().Name + "]", () => url);

        string[] returnedUrl = url.Split('&');
        string authCode = returnedUrl[0].Split('=')[1];

        Debug.Log("[" + GetType().Name + "]", () => authCode);

        SaveValuesAndContinue(authCode);
    }

    /// <summary>
    /// after splitting the values, the important ones are saved
    /// </summary>
    private async void SaveValuesAndContinue(string authCode)
    {
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, authCode);

        PlayerPrefsX.Save();

        await APIManager.GoogleFit.Authorization.ExchangeAuthCodeForToken();

        CanvasManager.instance.authenticateWindow.ExchangedAuthForToken();
    }
}