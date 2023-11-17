using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using LitJson;
using API = APIManager.GoogleFit;
using UnityEngine.UI;
using Michsky.MUIP;
using System.Threading.Tasks;
using UnityEngine.Events;

public class MainWindow : MonoBehaviour
{

    public NavigationBar navigationBar;


	[Space(5)]
	public StatisticsWindow statisticsWindow;



	[Space(10)]
    [Header("Home screen")]
    public TMP_Text usernameText;
    public TMP_Text todayDate;


	[Header("Progress bars")]
	public CircleProgressBar targetProgressBar;
	public CircleProgressBar stepsProgressBar;
	public CircleProgressBar distanceProgressBar;
	private float animationTime = 1;


    [Header("Graph blocks")]
    public TMP_Text stepsTodayValue;
    public EasyChartSettings stepsChart;

    [Space(2)]
    public TMP_Text distanceTodayValue;
    public EasyChartSettings distanceChart;


    [Header("Map visualisation")]
    public Image mapImage;



    public UnityAction onMainScreenLoaded;


    private void Start()
    {
        animationTime = CanvasManager.animationTime;
    }


    //called when the main window needs to be refreshed or loaded
    public async void StartMainWindow()
	{
		CanvasManager.instance.loadingScreen.gameObject.SetActive(true);

        usernameText.text = "Hi " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Details.nickname, "there") + "!";

        DateTime now = DateTime.Now;

        todayDate.text = now.ToString("dddd") + " "
            + UsefulFunctions.AddOrdinal(int.Parse(now.ToString("dd"))) + " "
            + now.ToString("MMMM");

        //shows the user their start and end Location
        //startLocation.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationName).Replace(",", ", ");
        //endLocation.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationName).Replace(",", ", ");
        

        ResetUIBlockText();
        //await GetMapImage();
        GetMapImage();
        await CalculateUserProgress();
        await LoadUIBlocks();

        CanvasManager.instance.loadingScreen.gameObject.SetActive(false);

        AnimateScreen();


        onMainScreenLoaded?.Invoke();


		DebugCanvas.instance.OnMainScreenOpen();
    }


	//animates the screen
	private void AnimateScreen()
	{
        float percentage = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted);

        int stepsToday = int.Parse(stepsTodayValue.text);
        float distanceToday = float.Parse(distanceTodayValue.text);

        int stepGoal = UserGoals.GetDailyStepGoal();
        float distanceGoal = UserGoals.GetDailyDistanceGoal();


        //animating the main challenge progress bar and daily goal progress bars
        LeanTween.value(gameObject, (float f) =>
        {
            targetProgressBar.percent = f;
        }, 0, percentage, animationTime).setEaseInOutCubic();


        LeanTween.value(gameObject, (float f) =>
        {
            stepsProgressBar.percent = (f / stepGoal) * 100;
        }, 0, stepsToday, animationTime).setEaseInOutCubic();

        LeanTween.value(gameObject, (float f) =>
        {
            distanceProgressBar.percent = (f / distanceGoal) * 100;
        }, 0, distanceToday, animationTime).setEaseInOutCubic();



        //animating the steps UI block
        LeanTween.value(gameObject, (float f) =>
		{
			stepsTodayValue.text = f.ToString("#,##0");
		}, 0, stepsToday, animationTime).setEaseInOutCubic();


		//animating the distance UI block
		LeanTween.value(gameObject, (float f) =>
		{
            distanceTodayValue.text = Math.Round(f, 2) + " km";
		}, 0, distanceToday, animationTime).setEaseInOutCubic();        
    }


    public void OpenChallengeInfoScreen()
    {
        Debug.Log("[ChallengeInfo] Opening statistics window, yet to create a window for this");
    }

    public void OpenStatisticsScreen(int dataType)
    {
        navigationBar.OpenWindow(1, () => statisticsWindow.OpenWindow((StatisticsWindow.DataTypes)dataType));
    }



    #region GoogleFit


#if UNITY_ANDROID || UNITY_EDITOR

    #region progress to target

    /// <summary>
    /// calculates the user progress based on the amount of distance the user has covered since the start start
    /// </summary>
    private async Task CalculateUserProgress()
    {
        //if start start is now, then make it beggining of the day
        DateTime startDate = PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, DateTime.Today);
        DateTime now = DateTime.Now;

        double dif = (now - startDate).TotalMinutes;

        Debug.Log("[CalculateUserProgressAndroid]", () => startDate);
        Debug.Log("[CalculateUserProgressAndroid]", () => now);
        Debug.Log("[CalculateUserProgressAndroid]", () => dif);

        API.ApiData data = new API.ApiData();

        //if less than a minute, get data in 30 minute intervals
        if (dif < 60) data = API.GenerateAPIbody(startDate, now, 1800000);

        //if less than 1 day, get data in hours
        else if (dif < 1440) data = API.GenerateAPIbody(startDate, now, 3600000);

        //if greater than 1 day (in minutes) get data with interval of 1 day
        else data = API.GenerateAPIbody(startDate, now);



        JsonData json = await API.GetDistanceBetweenMillis(data);
        //Debug.Log("[CalculateUserProgressAndroid]" + json.ToJson());



        float totalMeters = 0;

        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            try
            {
                totalMeters += float.Parse(stepData[0]["value"][0]["fpVal"].ToString());
            }
            catch (ArgumentOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            //Debug.Log("[CalculateUserProgressAndroid]", () => totalMeters);
        }

        Debug.Log("[CalculateUserProgressAndroid]", () => totalMeters);

        float distanceToTarget = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget, -1);
        float userKM = totalMeters / 1000;

        float percentage = (userKM / distanceToTarget) * 100;

        Debug.Log("[CalculateUserProgressAndroid]", () => percentage);


        PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted, percentage);
        PlayerPrefs.Save();
    }

    #endregion

    #region map image

    /// <summary>
    /// requests an image from MapQuest displaying the user's challenge data
    /// </summary>
    private async void GetMapImage()
    {
        #region variables

        float userLat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[0]);
        float userLong = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[1]);
        float targetLat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[0]);
        float targetLong = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[1]);

        float currentPointLat = UsefulFunctions.LatLongBetweenTwoLatLongs(userLat, userLong, targetLat, targetLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item1;
        float currentPointLong = UsefulFunctions.LatLongBetweenTwoLatLongs(userLat, userLong, targetLat, targetLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item2;

        #endregion

        #region debugging variables

        Debug.Log("[MapImageAndroid]", () => userLat);
        Debug.Log("[MapImageAndroid]", () => userLong);
        Debug.Log("[MapImageAndroid]", () => targetLat);
        Debug.Log("[MapImageAndroid]", () => targetLong);

        Debug.Log("[MapImageAndroid]", () => currentPointLat);
        Debug.Log("[MapImageAndroid]", () => currentPointLong);

        #endregion

        //can possible optimise more
        APIManager.MapQuest.MapData mData = new APIManager.MapQuest.MapData
        {
            startLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
            endLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

            location1 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
            location2 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

            currentLattitude = currentPointLat,
            currentLongitude = currentPointLong,

            imageHeight = (int)Math.Round(mapImage.rectTransform.rect.height),
            imageWidth = (int)Math.Round(mapImage.rectTransform.rect.width),

            zoom = UsefulFunctions.GetMapZoomApproximation()
        };

        mapImage.sprite = await APIManager.MapQuest.GetMapImage(mData);

        //return Task.CompletedTask;
    }

    #endregion

    #region UI blocks

    //shows a more professional placeholder
    private void ResetUIBlockText()
    {
        stepsTodayValue.text = "------";
        distanceTodayValue.text = "--- km";
    }

    //loads data for the UI blocks
    private async Task LoadUIBlocks()
    {
        #region request data

        DateTime start = DateTime.Today;
        DateTime end = DateTime.Now;

        API.ApiData body = API.GenerateAPIbody(start, DateTime.Now, 3600000); //1 hour time gap
        //API.ApiData body = API.GenerateAPIbody(start, DateTime.Now, (3600000 / 2)); //30 min time gap

        //Debug.Log("[UIBlocksAndroid]", () => body.startTimeMillis);
        //Debug.Log("[UIBlocksAndroid]", () => body.endTimeMillis);
        //Debug.Log("[UIBlocksAndroid]", () => body.durationMillis);

        #endregion


        JsonData stepsJson = await API.GetStepsBetweenMillis(body);
        JsonData distanceJson = await API.GetDistanceBetweenMillis(body);


        //Debug.Log("[UIBlocksAndroid] " + stepsJson.ToJson());
        //Debug.Log("[UIBlocksAndroid] " + distanceJson.ToJson());


        #region counting up

        int totalSteps = 0;
        float totalMeters = 0;

        for (int i = 0; i < stepsJson["bucket"].Count; i++)
        {
            try
            {
                int item = int.Parse(stepsJson["bucket"][i]["dataset"][0]["point"][0]["value"][0]["intVal"].ToString());

                totalSteps += item;
            }
            catch (KeyNotFoundException) { }
            catch (ArgumentOutOfRangeException) { }
        }

        Debug.Log("[UIBlocksAndroid]", () => totalSteps);


        for (int i = 0; i < distanceJson["bucket"].Count; i++)
        {
            try
            {
                float item = float.Parse(distanceJson["bucket"][i]["dataset"][0]["point"][0]["value"][0]["fpVal"].ToString());

                totalMeters += item;
            }
            catch (KeyNotFoundException) { }
            catch (ArgumentOutOfRangeException) { }
        }

        Debug.Log("[UIBlocksAndroid]", () => totalMeters);

        #endregion

        //visualise the data in a graph
        ProcessGraph(stepsJson, distanceJson);
        

        double distanceKM = Math.Round((totalMeters / 1000), 2);

        distanceTodayValue.text = distanceKM.ToString();
        stepsTodayValue.text = totalSteps.ToString();
    }

    #endregion

    #region over the day graph

    private void ProcessGraph(JsonData steps, JsonData distance)
    {
        List<double> stepsOverDay = new List<double>();
        List<double> distanceOverDay = new List<double>();


        for (int i = 0; i < steps["bucket"].Count; i++)
        {
            double value = 0;

            try
            {
                value = double.Parse(steps["bucket"][i]["dataset"][0]["point"][0]["value"][0]["intVal"].ToString());
            }
            catch (KeyNotFoundException) { }
            catch (ArgumentOutOfRangeException) { }

            stepsOverDay.Add(value);
        }


        for (int i = 0; i < distance["bucket"].Count; i++)
        {
            double value = 0;

            try
            {
                value = double.Parse(distance["bucket"][i]["dataset"][0]["point"][0]["value"][0]["fpVal"].ToString());
            }
            catch (KeyNotFoundException) { }
            catch (ArgumentOutOfRangeException) { }

            distanceOverDay.Add(value);
        }

        stepsChart.SetSerieData(stepsOverDay, 0, true);
        distanceChart.SetSerieData(distanceOverDay, 0, true);
    }

    #endregion

#endif

    #endregion



    #region Health Kit


#if UNITY_IOS && !UNITY_EDITOR

    #region progress to target

    /// <summary>
    /// calculates the user progress based on the amount of distance the user has covered since the start date
    /// </summary>
    private async Task CalculateUserProgress()
    {
        //if start date is now, then make it beggining of the day
        DateTime startDate = PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, DateTime.Today);
        DateTime now = DateTime.UtcNow;

        double userDistance = await APIManager.HealthKit.GetDistance(startDate, now);
        float distanceToTarget = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget, -1);

        float percentage = (float)(userDistance / distanceToTarget) * 100;


        Debug.Log("[CalculateUserProgressIOS]", () => userDistance);
        Debug.Log("[CalculateUserProgressIOS]", () => distanceToTarget);
        Debug.Log("[CalculateUserProgressIOS]", () => percentage);

        PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted, percentage);
        PlayerPrefs.Save();

        return;
    }

    #endregion

    #region map image


    /// <summary>
    /// requests an image from MapQuest displaying the user's challenge data
    /// </summary>
    private async void GetMapImage()
    {
        #region variables

        float startLat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[0]);
        float startLong = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[1]);
        float endLat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[0]);
        float endLong = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[1]);

        float currentPointLat = UsefulFunctions.LatLongBetweenTwoLatLongs(startLat, startLong, endLat, endLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item1;
        float currentPointLong = UsefulFunctions.LatLongBetweenTwoLatLongs(startLat, startLong, endLat, endLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item2;

        #endregion

        #region debugging variables

        Debug.Log("[MapImageIOS]", () => startLat);
        Debug.Log("[MapImageIOS]", () => startLong);
        Debug.Log("[MapImageIOS]", () => endLat);
        Debug.Log("[MapImageIOS]", () => endLong);

        Debug.Log("[MapImageIOS]", () => currentPointLat);
        Debug.Log("[MapImageIOS]", () => currentPointLong);

        #endregion

        //can possible optimise more
        APIManager.MapQuest.MapData mData = new APIManager.MapQuest.MapData
        {
            startLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
            endLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

            location1 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
            location2 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

            currentLattitude = currentPointLat,
            currentLongitude = currentPointLong,

            imageHeight = (int)Math.Round(mapImage.rectTransform.rect.height),
            imageWidth = (int)Math.Round(mapImage.rectTransform.rect.width),

            zoom = UsefulFunctions.GetMapZoomApproximation()
        };

        mapImage.sprite = await APIManager.MapQuest.GetMapImage(mData);

        //return Task.CompletedTask;
    }


    #endregion

    #region UI blocks

    //shows a more professional placeholder
    private void ResetUIBlockText()
    {
        stepsTodayValue.text = "------";
        distanceTodayValue.text = "--- km";
    }

    //loads data for the UI blocks
    private async Task LoadUIBlocks()
    {
        DateTime now = DateTime.Now;
        DateTime startOfDay = DateTime.Today;

        double stepsToday = await APIManager.HealthKit.GetSteps(startOfDay, now);
        double distanceToday = await APIManager.HealthKit.GetDistance(startOfDay, now);

        distanceToday = Math.Round(distanceToday, 2);

        Debug.Log("[UIBlocksIOS]", () => stepsToday);
        Debug.Log("[UIBlocksIOS]", () => distanceToday);

        stepsTodayValue.text = stepsToday.ToString();
        distanceTodayValue.text = distanceToday.ToString();
    }

    #endregion

#endif


    #endregion
}