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

    private string authURL = "https://www.fitbit.com/oauth2/authorize?";

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
        Application.OpenURL(authURL + "response_type=token&client_id=22C4ND&redirect_uri=unitydl%3A%2F%2FSteptastic&scope=" + scopes + "&expires_in=31536000");
    }

    public void onDeepLinkActivated(string url)
    {
        // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
        deeplinkURL = url;
        #region splitting returned data

        string[] returnedUrl = url.Split('&');
        string access_token = returnedUrl[0].Split('=')[1];
        string user_id = returnedUrl[1].Split('=')[1];
        int expires_in = int.Parse(returnedUrl[returnedUrl.Length - 1].Split('=')[1]);

        Debug.Log("access token: " + access_token);
        Debug.Log("user id: " + user_id);
        Debug.Log("expires in: " + expires_in);

        #endregion
    }
}