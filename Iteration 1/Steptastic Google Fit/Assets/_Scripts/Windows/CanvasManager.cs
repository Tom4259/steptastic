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

    public RectTransform setupWindows;

    [Space]
    public AuthenticateWindow authenticateWindow;
    public RequestUserLocationWindow requestUserLocationWindow; //code in later
    public ChallengeSetupWindow challengeSetupWindow;

    [Space]
    public RectTransform mainScreen;


    public TMP_Text todaysStepCount;

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

            //user has allowed/denied location services //code in later
            //if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, false))
            //{
            //    requestUserLocationWindow.gameObject.SetActive(false);
            //}
            //else { return; }

            //get user location first, so start location can either be 

            //user has created a challenge
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
    }

    public void UserFinishedLocationrequest()
    {
        requestUserLocationWindow.gameObject.SetActive(false);

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, true);
    }

    public void UserSetUpChallenge()
    {
        challengeSetupWindow.gameObject.SetActive(false);

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, true);
    }

    private void checkAllCompleted()
    {
        bool authenticated = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Account.authenticated, false);
        //bool location = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.requestedUserLocation, false);
        bool location = true;
        bool challenge = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.createdChallenge, false);

        if(authenticated && challenge && location)
        {
            setupWindows.gameObject.SetActive(false);
            mainScreen.gameObject.SetActive(true);
        }
    }    

    /// <summary>
    /// this method changes the screens that my application displays, and sets text boxes to important codes used for 
    /// retrieving data
    /// </summary>
    public void onUserLoggedIn(string _ = "")
    {
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

        APIManager.apiData body = APIManager.GenerateAPIbody(date.Subtract(t), DateTime.Now);

        Debug.Log(body.startTimeMillis);
        Debug.Log(body.endTimeMillis);
        Debug.Log(body.durationMillis);

        StartCoroutine(APIManager.GoogleFit.GetStepsBetweenMillis(body, getTodaySteps)); ;
    }

    private void getTodaySteps(JsonData json)
    {
        todaysStepCount.text = "today's step coount: " + json["bucket"][0]["dataset"][0]["point"][0]["value"][0]["intVal"].ToString();
    }
}