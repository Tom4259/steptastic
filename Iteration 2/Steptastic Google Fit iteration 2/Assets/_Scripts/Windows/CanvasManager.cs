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
    public bool testSetupStage = true;
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

        setupWindows.gameObject.SetActive(true);
        mainScreen.gameObject.SetActive(false);
    }

    private void Start()
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
                    StartCoroutine(APIManager.GoogleFit.Authorization.RefreshAccessToken((JsonData j) =>
                    {
                        Debug.Log("[" + GetType().Name + "]" + j.ToJson());
                    }));
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

        challengeSetupWindow.PopulateDropdowns();
    }

    public void SetupCompleted()
    {
        Debug.Log("[" + GetType().Name + "]" + "setup completed");

        CanvasGroup c = setupWindows.gameObject.GetComponent<CanvasGroup>();

        //LeanTween.value(setupWindows.gameObject, (float v) =>
        //{
        //    c.alpha = v;
        //}, 1, 0, 3);
    }

    /*
    //checks wether all windows have been completed by the user, if they have then the user can continue to the main screen
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
    */

    //put the following in the main screen script, KEEP AS AN EXAMPLE OF THE API CALL


    /// <summary>
    /// this method genereates the api body, and sends a web request to google, fetching the number of steps the user has done so far today.
    /// this is then displayed on the screen
    /// </summary>
    private void getTodaySteps()
    {
        DateTime date = DateTime.Now;
        TimeSpan t = new TimeSpan(0, date.Hour, date.Minute, date.Second);

        APIManager.GoogleFit.apiData body = APIManager.GoogleFit.GenerateAPIbody(date.Subtract(t), DateTime.Now);

        Debug.Log("[" + GetType().Name + "]", () => body.startTimeMillis);
        Debug.Log("[" + GetType().Name + "]", () => body.endTimeMillis);
        Debug.Log("[" + GetType().Name + "]", () => body.durationMillis);

        //StartCoroutine(APIManager.GoogleFit.GetStepsBetweenMillis(body, getTodaySteps)); ;
    }
}