using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitJson;

public class SetupCanvasManager : MonoBehaviour
{
    public static SetupCanvasManager instance;

    public RectTransform loginWindow;
    public RectTransform statsWindow;

    public TMP_InputField authCode;
    public TMP_InputField accessToken;

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

    public void onUserLoggedIn(string _authCode)
    {
        loginWindow.gameObject.SetActive(false);
        statsWindow.gameObject.SetActive(true);

        authCode.text = _authCode;

        StartCoroutine(WebRequestManager.GoogleFit.Authorization.exchangeAuthCodeForToken(collectedAccessToken));

    }

    private void collectedAccessToken()
    {
        //StartCoroutine(WebRequestManager.GoogleFit.getStepsBetweenMillis("{\"aggregateBy\":[{\"dataTypeName\":\"com.google.step_count.delta\",\"dataSourceId\":\"derived:com.google.step_count.delta:com.google.android.gms:estimated_steps\"}],\"bucketByTime\":{\"durationMillis\":86400000},\"startTimeMillis\":1695205815838,\"endTimeMillis\":1695305815838}", setAccessToken));
    }

    private void setAccessToken(JsonData json)
    {
        Debug.Log(json.ToString());
    }
}
