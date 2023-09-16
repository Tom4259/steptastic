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



    private string clientID = "452921919955-6lqch4h3ii4u9dgckneq9t13u6muf21b.apps.googleusercontent.com";
    //private string clientSecret = "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";


    [TextArea]
    public string scopes = "https://www.googleapis.com/auth/fitness.activity.read";


    private string authURL = "https://accounts.google.com/o/oauth2/v2/auth";

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

    public void startLoginToGoogle()
    {
        Application.OpenURL(authURL + "?client_id=" + clientID + "&redirect_uri=unitydl://steptastic&response_type=code&scope=" + scopes);        
    }

    public void onDeepLinkActivated(string url)
    {
        // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
        deeplinkURL = url;

        // Decode the URL to determine action. 
        // In this example, the app expects a link formatted like this:
        // unitydl://mylink?scene1 
        #region splitting returned data

        string[] returnedUrl = url.Split('&');
        string access_token = returnedUrl[0].Split('=')[1];
        string user_id = returnedUrl[1].Split('=')[1];
        int expires_in = int.Parse(returnedUrl[returnedUrl.Length - 1].Split('=')[1]);

        //Debug.Log("access token: " + access_token);
        //Debug.Log("user id: " + user_id);
        //Debug.Log("expires in: " + expires_in);

        #endregion

        #region saving data

        //PlayerPrefsX.SetString(PlayerPrefsKeyStorage.Fitbit.fitbitAccessToken, access_token);
        //PlayerPrefsX.SetString(PlayerPrefsKeyStorage.Fitbit.fitbitUserID, user_id);
        //PlayerPrefsX.SetInt(PlayerPrefsKeyStorage.Fitbit.fitbitAccessTokenExpires, expires_in);

        PlayerPrefs.Save();

        #endregion

        //UserSetupManager.instance.userSuccessfullyLoggedIntoFitbit();
    }
}