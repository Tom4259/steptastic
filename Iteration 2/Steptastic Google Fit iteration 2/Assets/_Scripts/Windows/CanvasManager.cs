using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitJson;
using System;
using Michsky.MUIP;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager instance;

#if UNITY_EDITOR
    public bool testSetupStage = true;
#endif

    [Space]
    public RectTransform setupWindows;
    public WindowManager setupWindowmanager;

    [Space]
    public AuthenticateWindow authenticateWindow;
    public RequestUserLocationWindow requestUserLocationWindow; //code in later
    public ChallengeSetupWindow challengeSetupWindow;

    [Space]
    public MainWindow mainScreen;
    private Vector2 mainScreenStartLocation;
    public float animationTime = 1.2f;

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

#if UNITY_EDITOR

        if (testSetupStage)
        {
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, false);
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.setup, false);
        }
        else
        {
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, true);
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.setup, true);
        }

#endif
        mainScreenStartLocation = new Vector2(GetComponent<CanvasScaler>().referenceResolution.x, 0);

        mainScreen.GetComponent<RectTransform>().anchoredPosition = mainScreenStartLocation;

        setupWindows.gameObject.SetActive(true);
        mainScreen.gameObject.SetActive(false);
    }

    private async void Start()
    {
        //if the user has completed the setup stage
        if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.setup, false))
        {
            setupWindows.gameObject.SetActive(false);
            mainScreen.gameObject.SetActive(true);

            //if the expiry time of access token has passed and the user is authenticated, refresh the access token
            if (PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Account.authenticated)) 
            {
                if(DateTime.Compare(PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, DateTime.Now.AddHours(-1)), DateTime.Now) < 0)
                {
                    Debug.Log("[" + GetType().Name + "]" + "refreshing token: expiry date reached");

                    //refreshes the access token and debugs it to the console
                    JsonData j = await APIManager.GoogleFit.Authorization.RefreshAccessToken();
                    Debug.Log("[" + GetType().Name + "]" + j.ToJson());
                }
            }

            mainScreen.StartMainWindow();
        }
        else
        {
            LoadSetup();
        }
    }

    private void LoadSetup()
    {
        setupWindows.gameObject.SetActive(true);
        mainScreen.gameObject.SetActive(false);

        setupWindowmanager.OpenWindowByIndex(0);

        challengeSetupWindow.PopulateDropdowns();
    }

    public void SetupCompleted()
    {
        Debug.Log("[" + GetType().Name + "]" + "setup completed");

        CanvasGroup c = setupWindows.gameObject.GetComponent<CanvasGroup>();

        LeanTween.move(setupWindows, -mainScreenStartLocation, animationTime).setEaseInOutCubic();

        mainScreen.gameObject.SetActive(true);

        LeanTween.move(mainScreen.GetComponent<RectTransform>(), Vector2.zero, animationTime).setEaseInOutCubic();

        mainScreen.StartMainWindow();
    }

    /*
    //checks wether all windows have been completed by the user, if they have then the user can continue to the main screen
    private void checkAllCompleted()
    {
        bool authenticated = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Account.authenticated, false);
        //bool testUserGetLocation = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, false);
        bool Location = true;
        bool challenge = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, false);

        if(authenticated && challenge && Location)
        {
            setupWindows.gameObject.SetActive(false);
            mainScreen.gameObject.SetActive(true);

            mainScreen.StartMainWindow();
        }        
    }
    */
}