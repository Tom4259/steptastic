using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using UnityEngine.UIElements;

public class DebugCanvas : MonoBehaviour
{
    public CustomInputField authToken;
    public CustomInputField accessToken;
    public CustomInputField refreshToken;

    private void Start()
    {
        refreshTokens();
    }


    public void refreshTokens()
    {
        authToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, "N/A");
        accessToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, "N/A");
        refreshToken.inputText.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, "N/A");
    }


    public void loginAsDeveloper()
    {
        setAuthCode();
        setAccessToken();
        setRefreshToken();
    }

    public void setAuthCode()
    {
        Debug.Log("[" + GetType().Name + "]" + "setting auth code to: " + authToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, authToken.inputText.text);
    }

    public void setAccessToken()
    {
        Debug.Log("[" + GetType().Name + "]" + "setting access token to: " + accessToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, accessToken.inputText.text);
    }

    public void setRefreshToken()
    {
        Debug.Log("[" + GetType().Name + "]" + "setting refresh token to: " + refreshToken.inputText.text);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, refreshToken.inputText.text);
    }
}
