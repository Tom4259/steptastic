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

public class MainWindow : MonoBehaviour
{
	public GameObject loadingScreen;



    [Space(25)]
    [Header("Home screen")]
    public GameObject homeScreen;
    public StatisticsWindow statisticsWindow;


    [Header("Today's summary")]
	[Header("Progress bar")]
	public CircleProgressBar progressBar;
	public float animationTime = 1;

    [Space(5)]
	[Header("Smaller UI blocks")]
    [Header("Steps")]
	public TMP_Text stepsTodayText;
	//public TMP_Text stepsGoalText;
	public ImageFillController stepsGoalProgressBar;

    [Space(2)]
    [Header("Distance")]
    public TMP_Text distanceTodayText;
    //public TMP_Text distanceGoalText;
    public ImageFillController distanceGoalProgressBar;

    [Space(5)]
    [Header("Map visualisation")]
    public Image mapImage;



    [Space(25)]
    [Header("Goals screen")]
    public GoalsWindow goalsScreen;





    //putting these item in a different window
    [Space(30)]
    [Header("Challenge details")]
    public TMP_Text startLocation;
    public TMP_Text endLocation;

    [Space]
	[Header("Graphs")]
	public EasyChartSettings activityChart;



	//called when the main window needs to be refreshed or loaded
	public async void StartMainWindow()
	{
		loadingScreen.SetActive(true);

        //shows the user their start and end Location
        //startLocation.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationName).Replace(",", ", ");
        //endLocation.text = PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationName).Replace(",", ", ");

		ResetUIBlockText();
        await CalculateUserProgress();
        await GetMapImage();
        await LoadUIBlocks();

#if UNITY_IOS && !UNITY_EDITOR
        //await LoadActivityGraph();
#endif

        loadingScreen.SetActive(false);

        AnimateScreen();


		DebugCanvas.instance.Start();
    }

	//animates the screen
	private void AnimateScreen()
	{
        float percentage = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted);

        //animating the main challenge progress bar
        LeanTween.value(gameObject, (float f) =>
        {
            progressBar.percent = f;
        }, 0, percentage, animationTime).setEaseInOutCubic();


		//activityChart.AnimateGraph();


		int stepsToday = int.Parse(stepsTodayText.text);
		float distanceToday = float.Parse(distanceTodayText.text);

		//animating the steps UI block
		LeanTween.value(gameObject, (float f) =>
		{
			stepsTodayText.text = f.ToString("#,##0");
		}, 0, stepsToday, animationTime).setEaseInOutCubic();


		//animating the distance UI block
		LeanTween.value(gameObject, (float f) =>
		{
            distanceTodayText.text = Math.Round(f, 2) + " km";
		}, 0, distanceToday, animationTime).setEaseInOutCubic();


        //animating the small UI object goal progress bars
        int stepGoal = PlayerPrefsX.GetInt(PlayerPrefsLocations.User.Goals.dailyStepGoal, 10000);
        float distanceGoal = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Goals.dailyDistanceGoal, 8);

        LeanTween.value(gameObject, (float f) =>
        {
            stepsGoalProgressBar.percent = f / stepGoal;
        }, 0, stepsToday, animationTime).setEaseInOutCubic();

        LeanTween.value(gameObject, (float f) =>
        {
            distanceGoalProgressBar.percent = f / distanceGoal;
        }, 0, distanceToday, animationTime).setEaseInOutCubic();
    }


    public void OpenChallengeInfoScreen()
    {
        Debug.Log("[ChallengeInfo] Opening statistics window");
    }

    public void OpenStepsAndDistanceStatistics()
    {
        Debug.Log("[Statistics] Opening statistics window");

        statisticsWindow.OpenWindow();
    }



    #region GoogleFit


#if UNITY_ANDROID || UNITY_EDITOR


    #region progress to target

    /// <summary>
    /// calculates the user progress based on the amount of distance the user has covered since the start date
    /// </summary>
    private async Task CalculateUserProgress()
    {
        //if start date is now, then make it beggining of the day
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
    private Task GetMapImage()
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

            zoom = UsefulFunctions.GetMapZoomApproximation(),

            imageToSet = mapImage,

            // runs this section of code when the map image has been loaded
            callback = () =>
            {
                //loadingScreen.SetActive(false);
            }
        };

        APIManager.MapQuest.GetMapImage(mData);
        return Task.CompletedTask;
    }

    #endregion

    #region UI blocks

    //shows a more professional placeholder
    private void ResetUIBlockText()
    {
        stepsTodayText.text = "------";
        distanceTodayText.text = "--- km";
    }

    //loads data for the UI blocks
    private async Task LoadUIBlocks()
    {
        #region request data

        DateTime date = DateTime.Now;
        TimeSpan t = new TimeSpan(0, date.Hour, date.Minute, date.Second);

        API.ApiData body = API.GenerateAPIbody(date.Subtract(t), DateTime.Now, 3600000); //1 hour time gap

        //Debug.Log("[UIBlocksAndroid]", () => body.startTimeMillis);
        //Debug.Log("[UIBlocksAndroid]", () => body.endTimeMillis);
        //Debug.Log("[UIBlocksAndroid]", () => body.durationMillis);

        #endregion


        JsonData stepsJson = await API.GetStepsBetweenMillis(body);
        JsonData distanceJson = await API.GetDistanceBetweenMillis(body);


        //Debug.Log("[UIBlocksAndroid] " + stepsJson.ToJson());
        //Debug.Log("[UIBlocksAndroid] " + distanceJson.ToJson());

        //creating the graph with the steps
        //LoadActivityGraph(stepsJson, distanceJson);


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

        double distanceKM = Math.Round((totalMeters / 1000), 2);

        distanceTodayText.text = distanceKM.ToString();
        stepsTodayText.text = totalSteps.ToString();
    }

    #endregion

    #region graphs

    //loads and inputs data into the steps over the day graph
    private void LoadActivityGraph(JsonData stepsJson, JsonData distanceJson)
    {
        StepsData(stepsJson);
        DistanceData(distanceJson);

        //creating the x axis points
        List<string> xPoints = new List<string>();
        for (int i = 0; i < 24; i++)
        {
            //xPoints.Add(i.ToString() + ((i >= 12) ? " pm" : " am"));

            if(i == 0)
            {
                xPoints.Add("12 am");
            }
            else if(i >= 12)
            {
                xPoints.Add((i - 12).ToString() + " pm");
            }
            else
            {
                xPoints.Add(i.ToString() + " am");
            }
        }

        activityChart.SetXAxisPoints(xPoints);
    }

    //plotting the steps data on the activity graph
    private void StepsData(JsonData json)
    {
        List<double> steps = new List<double>();

        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            double item = 0;

            try
            {
                item = double.Parse(stepData[0]["value"][0]["intVal"].ToString());
            }
            catch (ArgumentOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            steps.Add(item);
        }

        int remainder = 24 - steps.Count;

        for (int i = 0; i < remainder; i++)
        {
            steps.Add(0);
        }

        //Debug.Log("[ActivityGraphAndroid]", () => steps.Count);

        activityChart.SetSerieData(steps, 0);
    }

    //plotting the distance data on the activity graph
    private void DistanceData(JsonData json)
    {
        List<double> distance = new List<double>();

        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            double item = 0;

            try
            {
                item = double.Parse(stepData[0]["value"][0]["fpVal"].ToString());
            }
            catch (ArgumentOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            double distanceKM = Math.Round((item / 1000), 2);

            distance.Add(distanceKM);
        }

        int remainder = 24 - distance.Count;

        for (int i = 0; i < remainder; i++)
        {
            distance.Add(0);
        }

        Debug.Log("[ActivityGraphAndroid]", () => distance.Count);

        activityChart.SetSerieData(distance, 1);
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
    private Task GetMapImage()
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

            zoom = UsefulFunctions.GetMapZoomApproximation(),

            imageToSet = mapImage,

            // runs this section of code when the map image has been loaded
            callback = () =>
            {
                //loadingScreen.SetActive(false);
            }
        };

        APIManager.MapQuest.GetMapImage(mData);
        return Task.CompletedTask;
    }


    #endregion

    #region UI blocks

    //shows a more professional placeholder
    private void ResetUIBlockText()
    {
        stepsTodayText.text = "------";
        distanceTodayText.text = "--- km";
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

        stepsTodayText.text = stepsToday.ToString();
        distanceTodayText.text = distanceToday.ToString();
    }

    #endregion

    #region graphs

    //loads and inputs data into the steps over the day graph
    private async Task LoadActivityGraph()
    {
        DateTime endOfDay = DateTime.Today.AddDays(1);
        DateTime startOfDay = DateTime.Today;

        //getting the data
        List<APIManager.HealthKit.QuantityData> stepsQuantityData = await APIManager.HealthKit.GetStepsList(startOfDay, endOfDay);
        List<APIManager.HealthKit.QuantityData> distanceQuantityData = await APIManager.HealthKit.GetDistanceList(startOfDay, endOfDay);


        //Debug.Log("[ActivityGraphIOS]", () => stepsQuantityData.Count);

        //for (int i = 0; i < stepsQuantityData.Count; i++)
        //{
        //    Debug.LogFormat("[ActivityGraphIOS] Item {0} is value {1} for start date of {2} and end date of {3}",
        //        i, stepsQuantityData[i].value, stepsQuantityData[i].startDate, stepsQuantityData[i].endDate);
        //}


        #region steps count up

        List<double> stepsCountPerHour = new List<double>();

        for (int i = 0; i < 24; i++)
        {
            try
            {
                double hourTotal = 0;

                for (int z = 0; z < stepsQuantityData.Count; z++)
                {
                    DateTime averageDate = UsefulFunctions.AverageDateBetweenDateTimes(new List<DateTime>()
                    {
                        stepsQuantityData[z].startDate,
                        stepsQuantityData[z].endDate

                    });


                    if (averageDate.Hour == i)
                    {
                        hourTotal += stepsQuantityData[z].value;
                    }
                }

                stepsCountPerHour.Add(hourTotal);


                //Debug.Log("[ActivityGraphIOS] For hour " + i + ", user has done " + hourTotal);
            }
            catch (ArgumentOutOfRangeException)
            {
                //Debug.Log("[ActivityGraphIOS] For hour " + i + ", user has done 0 steps, (argumant out of range)");

                stepsCountPerHour.Add(0);
            }
        }

        #endregion


        #region distance count up

        List<double> distanceCountPerHour = new List<double>();

        for (int i = 0; i < 24; i++)
        {
            try
            {
                double hourTotal = 0;

                for (int z = 0; z < distanceQuantityData.Count; z++)
                {
                    DateTime averageDate = UsefulFunctions.AverageDateBetweenDateTimes(new List<DateTime>()
                    {
                        distanceQuantityData[z].startDate,
                        distanceQuantityData[z].endDate

                    });

                    double value = Math.Round(distanceQuantityData[z].value, 2);


                    if (averageDate.Hour == i)
                    {
                        hourTotal += value;
                    }
                }

                distanceCountPerHour.Add(hourTotal);


                //Debug.Log("[ActivityGraphIOS] For hour " + i + ", user has done " + hourTotal);
            }
            catch (ArgumentOutOfRangeException)
            {
                //Debug.Log("[ActivityGraphIOS] For hour " + i + ", user has made 0 distance, (argumant out of range)");

                distanceCountPerHour.Add(0);
            }
        }

        #endregion



        //creating the x axis points
        List<string> xPoints = new List<string>();
        for (int i = 0; i < 24; i++)
        {
            //xPoints.Add(i.ToString() + ((i >= 12) ? " pm" : " am"));

            if(i == 0)
            {
                xPoints.Add("12 am");
            }
            else if(i >= 12)
            {
                xPoints.Add((i - 12).ToString() + " pm");
            }
            else
            {
                xPoints.Add(i.ToString() + " am");
            }
        }

        activityChart.SetXAxisPoints(xPoints);


        //inputting the data into the chart
        activityChart.SetSerieData(stepsCountPerHour, 0);
        activityChart.SetSerieData(distanceCountPerHour, 1);
    }

    #endregion


#endif


    #endregion
}