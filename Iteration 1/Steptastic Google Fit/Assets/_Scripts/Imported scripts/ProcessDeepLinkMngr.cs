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
    public string editorToken;

    private string authURL = "https://accounts.google.com/o/oauth2/v2/auth";
    private string clientID = "452921919955-n5pr35harq133jfkf2kosvq4kbc724ps.apps.googleusercontent.com";
    private string scope = "https://www.googleapis.com/auth/fitness.activity.read https://www.googleapis.com/auth/fitness.location.read";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            Application.deepLinkActivated += onDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                onDeepLinkActivated(Application.absoluteURL);
            }
            // Initialize DeepLink Manager global variable.
            else deeplinkURL = "[none]";
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// opens the oauth2 screen for the user to login to their google account and authorize my app to access their data
    /// </summary>
    public void startLoginToGoogleFit()
    {
        //add an editor token, so don't need to keep logging in to google account on emulator
#if UNITY_EDITOR

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, editorToken);

        SetupCanvasManager.instance.editorHasCode();

#else
        APIManager.GoogleFit.Authorization.GetAuthorizationCode(authURL +
                "?client_id=" + clientID +
                "&redirect_uri=https://steptastic-ad9d9.web.app" +
                "&scope=" + scope +
                "&response_type=code" +
                "&access_type=offline");
#endif
    }

    /// <summary>
    /// when the user finishes authorizing my app, they will be directed to another site (which i have hosted) which 
    /// points and shows that the site is associated with my application
    /// </summary>
    //demo return link
    //https://steptastic-ad9d9.web.app/?code=4%2F0AfJohXlF9uJL5yPoEbD7LOZUhwzT5pIVfjN86bjd1kEWownIpAdUAcxrftkAo9Ky4op9Xg&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Ffitness.activity.read
    public void onDeepLinkActivated(string url)
    {
        // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
        deeplinkURL = url;

        //Debug.Log(url);

        #region splitting returned data

        string[] returnedUrl = url.Split('&');
        string authCode = returnedUrl[0].Split('=')[1];

        //Debug.Log("authCode: " + authCode);

        saveValuesAndContinue(authCode);

        #endregion
    }

    /// <summary>
    /// after splitting the values, the important ones are saved
    /// </summary>
    private void saveValuesAndContinue(string authCode)
    {
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, authCode);

        StartCoroutine(APIManager.GoogleFit.Authorization.ExchangeAuthCodeForToken(SetupCanvasManager.instance.onUserLoggedIn));
    }
}