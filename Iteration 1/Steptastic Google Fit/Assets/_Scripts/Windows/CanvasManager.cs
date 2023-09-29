using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitJson;
using System;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager instance;

#if UNITY_EDITOR
    public bool testUserAuthenticated = false;
    public bool testUserGetLocation = false;
    public bool testUserCreatedChallenge = false;
#endif

    [Space]
    public RectTransform setupWindows;

    [Space]
    public AuthenticateWindow authenticateWindow;
    public RequestUserLocationWindow requestUserLocationWindow; //code in later
    public ChallengeSetupWindow challengeSetupWindow;

    [Space]
    public MainWindow mainScreen;

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

        if (testUserAuthenticated) PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, false);
        else PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, true);

        if (testUserGetLocation) PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, false);
        else PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, true);

        if (testUserCreatedChallenge) PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, false);
        else PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, true);

#endif

        setupWindows.gameObject.SetActive(true);
        authenticateWindow.gameObject.SetActive(true);
        //requestUserLocationWindow.gameObject.SetActive(true); //code in later
        challengeSetupWindow.gameObject.SetActive(true);
        mainScreen.gameObject.SetActive(false);
    }

    private void Start()
    {
        //if the user has completed all setup windows
        if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.mainScreen, false))
        {
            setupWindows.gameObject.SetActive(false);
            mainScreen.gameObject.SetActive(true);
        }
        else
        {
            //user has logged in
            if (PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Account.authenticated, false))
            {
                authenticateWindow.gameObject.SetActive(false);

                //checking if the access token needs refreshing
                if (DateTime.Compare(PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, DateTime.Now.AddDays(-1)), DateTime.Now) < 0)
                {
                    Debug.Log("refreshing token");

                    StartCoroutine(APIManager.GoogleFit.Authorization.RefreshAccessToken(refreshCallback));
                }
            }
            else { return; }

            //user has allowed/denied testUserGetLocation services //code in later
            //if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, false))
            //{
            //    requestUserLocationWindow.gameObject.SetActive(false);
            //}
            //else { return; }

            //get user testUserGetLocation first, so start testUserGetLocation can either be 

            //user has created a testUserCreatedChallenge
            if (PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, false))
            {
                challengeSetupWindow.gameObject.SetActive(false);
            }
            else { return; }
            

            mainScreen.gameObject.SetActive(true);
        }
    }

    public void refreshCallback(JsonData j)
    {
        Debug.Log(j.ToJson());

        checkAllCompleted();
    }


    public void UserAuthenticated()
    {
        authenticateWindow.gameObject.SetActive(false);

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, true);

        checkAllCompleted();
    }

    public void UserFinishedLocationrequest()
    {
        requestUserLocationWindow.gameObject.SetActive(false);

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, true);

        checkAllCompleted();
    }

    public void UserSetUpChallenge()
    {
        challengeSetupWindow.gameObject.SetActive(false);

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, true);

        checkAllCompleted();
    }

    private void checkAllCompleted()
    {
        bool authenticated = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Account.authenticated, false);
        //bool testUserGetLocation = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, false);
        bool location = true;
        bool challenge = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, false);

        if(authenticated && challenge && location)
        {
            setupWindows.gameObject.SetActive(false);
            mainScreen.gameObject.SetActive(true);

            mainScreen.StartMainWindow();
        }

        
    }    

    /// <summary>
    /// this method changes the screens that my application displays, and sets text boxes to important codes used for 
    /// retrieving data
    /// </summary>
    public void onUserLoggedIn(string _ = "")
    {
        Debug.Log("this is where the main script would be called");
        getTodaySteps();
    }

/*
#if UNITY_EDITOR

    /// <summary>
    /// this method is only for the unity editor, as it i would like it to behave differently to an actual android 
    /// device
    /// </summary>
    public void editorHasCode()
    {
        authenticateWindow.gameObject.SetActive(false);
        mainScreen.gameObject.SetActive(true);

        authCode.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode);
        accessToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken);
        refreshToken.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken);

        getTodaySteps();
    }

#endif
    */




    //put the following in the main screen script


    /// <summary>
    /// this method genereates the api body, and sends a web request to google, fetching the number of steps the user has done so far today.
    /// this is then displayed on the screen
    /// </summary>
    private void getTodaySteps()
    {
        DateTime date = DateTime.Now;
        TimeSpan t = new TimeSpan(0, date.Hour, date.Minute, date.Second);

        APIManager.GoogleFit.apiData body = APIManager.GoogleFit.GenerateAPIbody(date.Subtract(t), DateTime.Now);

        Debug.Log(body.startTimeMillis);
        Debug.Log(body.endTimeMillis);
        Debug.Log(body.durationMillis);

        //StartCoroutine(APIManager.GoogleFit.GetStepsBetweenMillis(body, getTodaySteps)); ;
    }
}