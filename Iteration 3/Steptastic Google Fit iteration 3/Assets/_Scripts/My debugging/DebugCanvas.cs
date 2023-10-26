using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using LitJson;
using System;
using System.Threading.Tasks;
using IngameDebugConsole;

public class DebugCanvas : MonoBehaviour
{
    public static DebugCanvas instance;

    public DebugLogManager logManager;

    [Space]
    public GameObject objectsToHide;
    public GameObject[] androidOnlyObjects;
    public GameObject[] iosOnlyObjects;

    [Space(20)]
    public CustomInputField authToken;
    public CustomInputField accessToken;
    public CustomInputField refreshToken;


    [Space]
    public CustomInputField dateInput;

    [Space]
    public TMP_Text versionText;


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

        //if the build is a development build, then show the debug options
        //gameObject.SetActive(Debug.isDebugBuild);
        //logManager.gameObject.SetActive(Debug.isDebugBuild);        

#if UNITY_ANDROID || UNITY_EDITOR

        for (int i = 0; i < iosOnlyObjects.Length; i++)
        {
            iosOnlyObjects[i].SetActive(false);
        }

#elif UNITY_IOS

        for (int i = 0; i < androidOnlyObjects.Length; i++)
        {
            androidOnlyObjects[i].SetActive(false);
        }
#endif
    }


    public void EnableDebugControls()
    {
        gameObject.SetActive(true); 
        logManager.gameObject.SetActive(true);
    }

    public void DisableDebugControls()
    {
        gameObject.SetActive(false);
        logManager.gameObject.SetActive(false);
    }




    public void Start()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        ReloadTokens();
#endif

        LoadDate();
        versionText.text = "V " + Application.version;

        objectsToHide.SetActive(false);

        DisableDebugControls();
    }

    public void OnMainScreenOpen()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        ReloadTokens();
#endif

        LoadDate();
        versionText.text = "V " + Application.version;

        objectsToHide.SetActive(false);
    }
    

    public void DevLogin()
    {
        CanvasManager.instance.authenticateWindow.UserAuthenticated();

        objectsToHide.SetActive(false);
    }


    public void RefreshMainWindow()
    {
        CanvasManager.instance.mainScreen.StartMainWindow();

        objectsToHide.SetActive(false);
    }


    public void ResetApp()
    {
        PlayerPrefs.DeleteAll();
        Application.Quit();
    }


    #region start date

    private void LoadDate()
    {
        dateInput.inputText.text = PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate).ToString("d");

        //dateInput.UpdateStateInstant();
    }

    public void SetStartDate()
    {
        string d = dateInput.inputText.text;

        PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, Convert.ToDateTime(d));

        RefreshMainWindow();
    }

    #endregion


    #region codes

    public void ReloadTokens()
    {
        authToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, "N/A");
        accessToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, "N/A");
        refreshToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, "N/A");


        //authToken.UpdateStateInstant();
        //accessToken.UpdateStateInstant();
        //refreshToken.UpdateStateInstant();
    }

    public async void RefreshTokens()
    {
        JsonData j = await APIManager.GoogleFit.Authorisation.RefreshAccessToken();

        Debug.Log("[DebugCanvas] " + j.ToJson());

        ReloadTokens();
    }


    public void SetAuthCode()
    {
        //Debug.Log("[DebugCanvas] " + "Setting auth code to: " + authToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, authToken.inputText.text);
    }

    public void SetAccessToken()
    {
        //Debug.Log("[DebugCanvas] " + "Setting access token to: " + accessToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, accessToken.inputText.text);
    }

    public void SetRefreshToken()
    {
        //Debug.Log("[DebugCanvas] " + "Setting refresh token to: " + refreshToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, refreshToken.inputText.text);
    }

    #endregion
}
