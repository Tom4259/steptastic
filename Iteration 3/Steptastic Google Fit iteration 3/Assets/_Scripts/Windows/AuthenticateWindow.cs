using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LitJson;
using Michsky.MUIP;
using System;

public class AuthenticateWindow : MonoBehaviour
{
    public ModalWindowManager scopeError;

    public ButtonManager continueButton;

    /// <summary>
    /// opens the oauth2 screen for the user to login to their google account and authorize my app to access their data
    /// </summary>
    public void LoginToGoogleFit()
    {
        //add an editor token, so don't need to keep logging in to google account on emulator
#if UNITY_EDITOR

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, ProcessDeepLinkMngr.Instance.editorAuth);
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, ProcessDeepLinkMngr.Instance.editorToken);
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, ProcessDeepLinkMngr.Instance.editorRefresh);

        PlayerPrefsX.Save();

        CanvasManager.instance.authenticateWindow.ExchangedAuthForToken();

#else
        APIManager.GoogleFit.Authorization.GetAuthorizationCode();
#endif
    }

    public bool CheckScopes(string url)
    {
        try
        {
            if (url.Split("scope=")[1].Split("%20").Length < 2)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }

    public void ShowScopeError()
    {
        scopeError.Open();
    }



    public void ExchangedAuthForToken(string _ = "")
    {
        UserAuthenticated();
    }

    //called when everything about authentication has been completed
    public void UserAuthenticated()
    {
        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, true);

        continueButton.onClick.Invoke();
    }
}
