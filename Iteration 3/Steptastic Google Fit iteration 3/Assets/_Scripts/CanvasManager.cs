using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitJson;
using System;
using Michsky.MUIP;
using System.Threading.Tasks;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager instance;

#if UNITY_EDITOR
    public bool testSetupStage = true;
#endif

    public LoadingScreen loadingScreen;

    [Space]
    public RectTransform setupWindows;
    public WindowManager setupWindowmanager;

    [Space]
    public AuthenticateWindow authenticateWindow;
    public RequestUserLocationWindow requestUserLocationWindow; //code in later
    public ChallengeSetupWindow challengeSetupWindow;

    [Space]
    public MainWindow mainScreen;
    public NavigationBar navigationBar;
    private Vector2 mainScreenStartLocation;
    public static float animationTime = 1.75f;
    public static float windowAnimationTime = 0.75f;

    private void Awake()
    {
        //creates an instance so other scripts can access variables and methods
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
#endif

        loadingScreen.gameObject.SetActive(false);

        mainScreenStartLocation = new Vector2(GetComponent<CanvasScaler>().referenceResolution.x, 0);

        setupWindows.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        mainScreen.GetComponent<RectTransform>().anchoredPosition = mainScreenStartLocation;

        setupWindows.gameObject.SetActive(true);
        mainScreen.gameObject.SetActive(false);
    }

    private async void Start()
    {
        //if the user has completed the setup stage
        if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.setup, false))
        {
            //if the expiry time of access token has passed and the user is authenticated, refresh the access token
            if (PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Account.authenticated)) 
            {
#if UNITY_ANDROID || UNITY_EDITOR
                if(DateTime.Compare(PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, DateTime.Now.AddHours(-1)), DateTime.Now) < 0)
                {
                    Debug.Log("[CanvasManager]" + "refreshing token: expiry date reached");

                    //refreshes the access token and debugs it to the console
                    JsonData j = await APIManager.GoogleFit.Authorisation.RefreshAccessToken();
                    Debug.Log("[CanvasManager]" + j.ToJson());
                }
#endif
            }

            CloseSetupWindow();

            OpenMainWindow(false);
        }
        else
        {
            LoadSetup();
        }
    }

    //shows the setup page
    private void LoadSetup()
    {
        setupWindows.gameObject.SetActive(true);
        mainScreen.gameObject.SetActive(false);
        navigationBar.gameObject.SetActive(false);

        setupWindowmanager.OpenWindowByIndex(0);

        challengeSetupWindow.PopulateDropdowns();
    }

    //closes the setup page and opens the main page
    public void SetupCompleted()
    {
        Debug.Log("[Setup] Setup completed");

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.setup, true);

        CloseSetupWindow();
        OpenMainWindow(true);
    }

    //closes the setup page
    private async void CloseSetupWindow()
    {
        LeanTween.move(setupWindows, -mainScreenStartLocation, animationTime).setEaseInOutCubic();

        await Task.Delay((int)(animationTime * 1000) + 2000);

        setupWindows.gameObject.SetActive(false);
    }

    //opens the main window, animation is used for the sliding in effect
    private void OpenMainWindow(bool animation)
    {
        mainScreen.gameObject.SetActive(true);
        navigationBar.gameObject.SetActive(true);

        if (animation)
        {
            LeanTween.move(mainScreen.GetComponent<RectTransform>(), Vector2.zero, animationTime).setEaseInOutCubic();
        }
        else
        {
            mainScreen.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        mainScreen.StartMainWindow();
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}