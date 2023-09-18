using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProcessDeepLinkMngr : MonoBehaviour
{
    public static ProcessDeepLinkMngr Instance { get; private set; }
    public string deeplinkURL = "unitydl://Steptastic";
    [TextArea]
    public string scopes = "activity%20profile%20settings";

    [Space]
    [TextArea]
    public string editorAuthToken = "";

    private string authURL = "https://accounts.google.com/o/oauth2/v2/auth";
    private string clientID = "452921919955-n5pr35harq133jfkf2kosvq4kbc724ps.apps.googleusercontent.com";

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

    public void startLoginToFitbit()
    {
        //add an editor token, so don't need to keep logging in to fitbit account on run
        Application.OpenURL(authURL + "?client_id=" + clientID + "&redirect_uri=https://steptastic-ad9d9.web.app&scope=https://www.googleapis.com/auth/fitness.activity.read&response_type=code") ;
    }


    //demo return link
    //https://steptastic-ad9d9.web.app/?code=4%2F0AfJohXlF9uJL5yPoEbD7LOZUhwzT5pIVfjN86bjd1kEWownIpAdUAcxrftkAo9Ky4op9Xg&scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Ffitness.activity.read
    public void onDeepLinkActivated(string url)
    {
        // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
        deeplinkURL = url;

        Debug.Log(url);

        #region splitting returned data

        string[] returnedUrl = url.Split('&');
        string authCode = returnedUrl[0].Split('=')[1];
        //string user_id = returnedUrl[1].Split('=')[1];
        //int expires_in = int.Parse(returnedUrl[returnedUrl.Length - 1].Split('=')[1]);

        Debug.Log("authCode: " + authCode);
        //Debug.Log("user id: " + user_id);
        //Debug.Log("expires in: " + expires_in);

        saveValuesAndContinue(authCode);

        #endregion
    }

    private void saveValuesAndContinue(string authCode)
    {
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.authorizationCode, authCode);

        SetupCanvasManager.instance.onUserLoggedIn(authCode);
    }
}