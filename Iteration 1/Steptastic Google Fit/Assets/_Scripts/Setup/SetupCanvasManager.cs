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

    public void onUserLoggedIn(string _accessToken)
    {
        loginWindow.gameObject.SetActive(false);
        statsWindow.gameObject.SetActive(true);

        authCode.text = _accessToken;        

        WebRequestManager.GoogleFit.getAccessToken(setAccessToken);
    }

    private void setAccessToken(JsonData json)
    {
        accessToken.text = json["access_token"].ToString();
    }
}
