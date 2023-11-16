using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.MUIP;
using System;
using System.Threading.Tasks;

public class AuthenticatePanel : MonoBehaviour
{
    private SetupManager setupManager;


    public GameObject healthKitItems;
    public GameObject googleFitItems;


    [Space]
    public ModalWindowManager androidScopeError;
    public ModalWindowManager IOSScopeManager;

    [Space]
    public int locationWindowIndex = 1;


    [Space(30)]
    public ButtonManager continueButton;


    private void Start()
    {
        setupManager = GetComponentInParent<SetupManager>();

#if !UNITY_EDITOR
        healthKitItems.SetActive(Application.platform == RuntimePlatform.IPhonePlayer);
        googleFitItems.SetActive(Application.platform == RuntimePlatform.Android);
#else
        healthKitItems.SetActive(false);
        googleFitItems.SetActive(true);
#endif
    }


    /// <summary>
    /// opens the oauth2 screen for the user to login to their google account and authorize my app to access their data
    /// </summary>
    public async void AuthoriseService()
    {
        //add an editor token, so don't need to keep logging in to google account on emulator
#if UNITY_EDITOR

        await Task.Delay(1);

        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode, GoogleFitService.Instance.editorAuth);
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, GoogleFitService.Instance.editorToken);
        PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, GoogleFitService.Instance.editorRefresh);

        PlayerPrefsX.Save();

        CanvasManager.instance.authenticatePanel.ExchangedAuthForToken();
    }

#elif UNITY_ANDROID
        APIManager.GoogleFit.Authorisation.GetAuthorisationCode();
    }

#elif UNITY_IOS
        bool isAuthorised = await APIManager.HealthKit.Authorisation.Authorise();

        CheckScopes();
    }

#endif

#if UNITY_ANDROID || UNITY_EDITOR

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

#elif UNITY_IOS

    public async void CheckScopes()
    {
        if (!await APIManager.HealthKit.Authorisation.CheckPermissions())
        {
            Debug.LogWarning("[CanvasManager] All or some permissions have not been enabled...");

            ShowScopeError();
        }
        else
        {
            UserAuthenticated();
        }
    }

#endif

    public void ShowScopeError()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        androidScopeError.Open();
#elif UNITY_IOS
        IOSScopeManager.Open();
#endif
    }

#if UNITY_ANDROID || UNITY_EDITOR

    public void ExchangedAuthForToken(string _ = "")
    {
        UserAuthenticated();
    }

#endif


    //called when everything about authentication has been completed
    public void UserAuthenticated()
    {
        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, true);

        continueButton.onClick.Invoke();
    }

    public void NextPanel()
    {
        setupManager.NextPanel();
    }
}