using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitJson;
using System;

public class SetupCanvasManager : MonoBehaviour
{
    public static SetupCanvasManager instance;

    public RectTransform loginWindow;
    public RectTransform statsWindow;

    public TMP_InputField authCode;
    public TMP_InputField accessToken;
    public TMP_Text todaysStepCount;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        if (PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Account.authenticated, false))
        {
            if (DateTime.Compare(PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, DateTime.Now.AddDays(-1)), DateTime.Now) < 0)
            {
                Debug.Log("refreshing token");

                StartCoroutine(APIManager.GoogleFit.Authorization.RefreshAccessToken(refreshCallback));
            }
            else
            {
                //in a new script, preferably in iteration 2 for non-spaghetti code, load the graphs and data
                onUserLoggedIn();
            }
        }
        else
        {
            //open the setup window if the user hasnt logged in yet
            loginWindow.gameObject.SetActive(true);
            statsWindow.gameObject.SetActive(false);
        }
    }

    private void refreshCallback(JsonData j)
    {
        Debug.Log(j.ToJson());

        onUserLoggedIn();
    }

    /// <summary>
    /// this method changes the screens that my application displays, and sets text boxes to important codes used for 
    /// retrieving data
    /// </summary>
    public void onUserLoggedIn(string _ = "")
    {
        loginWindow.gameObject.SetActive(false);
        statsWindow.gameObject.SetActive(true);

        authCode.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        accessToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);

        getTodaySteps();
    }

#if UNITY_EDITOR
    /// <summary>
    /// this method is only for the unity editor, as it i would like it to behave differently to an actual android 
    /// device
    /// </summary>
    public void editorHasCode()
    {
        loginWindow.gameObject.SetActive(false);
        statsWindow.gameObject.SetActive(true);

        authCode.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        accessToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);

        getTodaySteps();
    }

#endif

    /// <summary>
    /// this method genereates the api body, and sends a web request to google, fetching the number of steps the user has done so far today.
    /// this is then displayed on the screen
    /// </summary>
    private void getTodaySteps()
    {
        DateTime date = DateTime.Now;
        TimeSpan t = new TimeSpan(0, date.Hour, date.Minute, date.Second);

        APIManager.apiData body = APIManager.GenerateAPIbody(date.Subtract(t), DateTime.Now);

        Debug.Log(body.startTimeMillis);
        Debug.Log(body.endTimeMillis);
        Debug.Log(body.durationMillis);

        StartCoroutine(APIManager.GoogleFit.GetStepsBetweenMillis(body, getTodaySteps)); ;
    }

    private void getTodaySteps(JsonData json)
    {
        todaysStepCount.text = "today's step coount: " + json["bucket"][0]["dataset"][0]["point"][0]["value"][0]["intVal"].ToString();
    }
}