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

#endif
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
                if(DateTime.Compare(PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, DateTime.Now.AddHours(-1)), DateTime.Now) < 0)
                {
                    Debug.Log("[" + GetType().Name + "]" + "refreshing token: expiry date reached");

                    //refreshes the access token and debugs it to the console
                    JsonData j = await APIManager.GoogleFit.Authorization.RefreshAccessToken();
                    Debug.Log("[" + GetType().Name + "]" + j.ToJson());
                }
            }

            closeSetupWindow();

            openMainWindow(false);
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

        closeSetupWindow();

        openMainWindow(true);
    }

    private async void closeSetupWindow()
    {
        LeanTween.move(setupWindows, -mainScreenStartLocation, animationTime).setEaseInOutCubic();

        await Task.Delay((int)(animationTime * 1000) + 2000);

        setupWindows.gameObject.SetActive(false);
    }

    private void openMainWindow(bool animation)
    {
        mainScreen.gameObject.SetActive(true);

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
}