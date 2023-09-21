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
        loginWindow.gameObject.SetActive(true);
        statsWindow.gameObject.SetActive(false);
    }

    public void onUserLoggedIn(string _)
    {
        loginWindow.gameObject.SetActive(false);
        statsWindow.gameObject.SetActive(true);

        authCode.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        accessToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);

        getTodaySteps();
    }

#if UNITY_EDITOR

    public void editorHasCode()
    {
        loginWindow.gameObject.SetActive(false);
        statsWindow.gameObject.SetActive(true);

        authCode.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        accessToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);

        getTodaySteps();
    }

#endif

    private void getTodaySteps()
    {
        long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        long startTime = milliseconds - (DateTimeOffset.Now.Hour * 3600000);
        long endTime = milliseconds;

        string body = "{\"aggregateBy\":[{\"dataTypeName\":\"com.google.step_count.delta\",\"dataSourceId\":\"derived:com.google.step_count.delta:com.google.android.gms:estimated_steps\"}],\"bucketByTime\":{\"durationMillis\":86400000},\"startTimeMillis\":" + startTime + ",\"endTimeMillis\":" + endTime + "}";

        StartCoroutine(WebRequestManager.GoogleFit.getStepsBetweenMillis(body, getTodaySteps)); ;
    }

    private void getTodaySteps(JsonData json)
    {
        todaysStepCount.text = "today's step coount: " + json["bucket"][0]["dataset"][0]["point"][0]["value"][0]["intVal"].ToString();
    }
}
