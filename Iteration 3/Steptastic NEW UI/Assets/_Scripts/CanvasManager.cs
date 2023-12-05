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
    public RectTransform setupWindow;
    public MyPanelManager setupPanelManager;

    [Space]
    [Header("Setup panels")]
    public ProfilePanel profilePanel;
    public AuthenticatePanel authenticatePanel;
    public LocationRequestPanel locationRequestPanel;
    public ChallengeSetupPanel challengeSetupPanel;

    [Space]
    [Header("Main windows")]
    public MainWindow mainWindow;
    public CompletedChallengeWindow completedChallengeWindow;
    public NavigationBar navigationBar;

    private Vector2 mainScreenStartLocation;
    public static float animationTime = 1.75f;
    public static float fastWindowAnimationTime = 0.75f;
    public static float windowAnimationTime = 1.4f;



    public bool isSetupWindowOpen = true;
    public bool isMainWindowOpen = false;




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

        loadingScreen.gameObject.SetActive(true);

        mainScreenStartLocation = new Vector2(GetComponent<CanvasScaler>().referenceResolution.x, 0);

        setupWindow.gameObject.SetActive(true);
        mainWindow.gameObject.SetActive(false);
        completedChallengeWindow.CloseWindow(false);
        completedChallengeWindow.gameObject.SetActive(true);
    }

    private async void Start()
    {
        Application.targetFrameRate = 60;

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

            CloseSetupWindow(false);

            OpenMainWindow(false);
        }
        else
        {
            OpenSetupWindow();
        }
    }

    //shows the setup page
    private void OpenSetupWindow()
    {
        isSetupWindowOpen = true;

        //setupWindow.gameObject.SetActive(true);

        LeanTween.value(gameObject, (float f) =>
        {
            setupWindow.anchoredPosition = new Vector2(0, f);
        }, -Screen.currentResolution.height, 0, fastWindowAnimationTime).setEaseOutCubic();

        mainWindow.gameObject.SetActive(false);
        navigationBar.gameObject.SetActive(false);

        challengeSetupPanel.PopulateDropdowns();

        loadingScreen.gameObject.SetActive(false);
    }

    //closes the setup page and opens the main page
    public void SetupCompleted()
    {
        Debug.Log("[Setup] Setup completed");

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.setup, true);

        CloseSetupWindow(true);
        OpenMainWindow(true);
    }

    //closes the setup page
    private async void CloseSetupWindow(bool animation)
    {
        isSetupWindowOpen = false;

        if (animation)
        {
            LeanTween.value(gameObject, (float f) =>
            {
                setupWindow.anchoredPosition = new Vector2(0, f);
            }, -Screen.currentResolution.height, 0, fastWindowAnimationTime).setEaseOutCubic();
        }
        else
        {
            setupWindow.anchoredPosition = -mainScreenStartLocation;
        }

        await Task.Delay((int)(animationTime * 1000) + 2000);

        setupWindow.gameObject.SetActive(false);
    }

    //opens the main window, animation is used for the sliding in effect
    private void OpenMainWindow(bool animation)
    {
        isMainWindowOpen = true;

        mainWindow.gameObject.SetActive(true);
        navigationBar.gameObject.SetActive(true);

        if (animation)
        {
            LeanTween.move(mainWindow.GetComponent<RectTransform>(), Vector2.zero, animationTime).setEaseInOutCubic();
        }
        else
        {
            mainWindow.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        mainWindow.StartMainWindow();
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}