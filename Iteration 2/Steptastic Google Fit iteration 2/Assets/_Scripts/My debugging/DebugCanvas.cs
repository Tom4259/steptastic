using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using UnityEngine.UIElements;
using LitJson;

public class DebugCanvas : MonoBehaviour
{
    public CustomInputField authToken;
    public CustomInputField accessToken;
    public CustomInputField refreshToken;

    [Space]
    public TMP_Text versionText;

    private void Start()
    {
        ReloadTokens();
        versionText.text = "V " + Application.version;
    }


    public void ReloadTokens()
    {
        authToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, "N/A");
        accessToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, "N/A");
        refreshToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, "N/A");
    }

    public async void RefreshTokens()
    {
        JsonData j = await APIManager.GoogleFit.Authorization.RefreshAccessToken();

        Debug.Log(j.ToJson());

        ReloadTokens();
    }


    public void DevLogin()
    {
        CanvasManager.instance.authenticateWindow.UserAuthenticated();
    }


    public void SetAuthCode()
    {
        Debug.Log("[" + GetType().Name + "] " + "Setting auth code to: " + authToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, authToken.inputText.text);
    }

    public void SetAccessToken()
    {
        Debug.Log("[" + GetType().Name + "] " + "Setting access token to: " + accessToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, accessToken.inputText.text);
    }

    public void SetRefreshToken()
    {
        Debug.Log("[" + GetType().Name + "] " + "Setting refresh token to: " + refreshToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, refreshToken.inputText.text);
    }
}
