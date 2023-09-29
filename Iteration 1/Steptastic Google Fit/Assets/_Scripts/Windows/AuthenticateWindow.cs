using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LitJson;

public class AuthenticateWindow : MonoBehaviour
{
    public TMP_InputField authCode;
    public TMP_InputField accessToken;
    public TMP_InputField refreshToken;

    public void ExchangedAuthForToken(string _ = "")
    {
        authCode.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        accessToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);
        refreshToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken);

        UserAuthenticated();
    }

    public void EDITORrefreshToken()
    {
        StartCoroutine(APIManager.GoogleFit.Authorization.RefreshAccessToken(EDITORrefreshToken));
    }

    public void EDITORrefreshToken(JsonData j)
    {
        authCode.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        accessToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);
        refreshToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken);

        ProcessDeepLinkMngr.Instance.editorAuth = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        ProcessDeepLinkMngr.Instance.editorToken = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);
        ProcessDeepLinkMngr.Instance.editorRefresh = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken);
    }

    //called when everything about authentication has been completed
    public void UserAuthenticated()
    {
        CanvasManager.instance.UserAuthenticated();
    }

    //following for developing purposes

    public void loginAsDeveloper()
    {
        setAuthCode();
        setAccessToken();
        setRefreshToken();

        UserAuthenticated();
    }

    public void setAuthCode()
    {
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, authCode.text);
    }

    public void setAccessToken()
    {
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, accessToken.text);
    }

    public void setRefreshToken()
    {
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, refreshToken.text);
    }
}
