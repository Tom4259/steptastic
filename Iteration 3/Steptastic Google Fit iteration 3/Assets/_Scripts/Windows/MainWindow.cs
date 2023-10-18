using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XCharts;
using System;
using LitJson;
using API = APIManager.GoogleFit;
using UnityEngine.UI;
using Michsky.MUIP;
using XCharts.Runtime;
using System.Linq;
using System.Threading.Tasks;

public class MainWindow : MonoBehaviour
{
	public GameObject loadingScreen;

	[Space(20)]
	[Header("Progress bar")]
	public ProgressBar progressBar;
	public float animationTime = 1;

	//[Space(10)]
	//[TextArea]
	//public string challengeDescriptionText;
	//public TMP_Text challengeDescriptionLabel;

	[Space(20)]
	[Header("Map visualisation")]
	public Image mapImage;


	[Space(20)]
	[Header("Smaller UI blocks")]
	public TMP_Text stepsBlockValue;
	public TMP_Text distanceBlockValue;

	[Space]
	[Header("Graphs")]
	public EasyChartSettings activityChart;


	//called when the main window needs to be refreshed or loaded
	public async void StartMainWindow()
	{
		loadingScreen.SetActive(true);

		//shows the user their start and end Location
		//challengeDescriptionLabel.text = challengeDescriptionText.Replace("{{startLocation}}",
		//	PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationCapital) + ", " +
		//	PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationName)).Replace("{{endLocation}}",
		//	PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationCapital) + ", " +
		//	PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationName));

		ResetUIBlockText();
        await CalculateUserProgress();
        await GetMapImage();
        await LoadUIBlocks();

#if UNITY_IOS && !UNITY_EDITOR
        await LoadStepsDayGraph();
#endif

        loadingScreen.SetActive(false);

        AnimateScreen();


		DebugCanvas.instance.Start();
    }

	//animates to screen
	private void AnimateScreen()
	{
        float percentage = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted);

        LeanTween.value(gameObject, (float f) =>
        {
            progressBar.currentPercent = f;
        }, 0, percentage, animationTime);

		activityChart.AnimateGraph();


		int animationSteps = int.Parse(stepsBlockValue.text);
		float animationDistance = float.Parse(distanceBlockValue.text);

		//animating the steps UI block
		LeanTween.value(gameObject, (float f) =>
		{
			stepsBlockValue.text = f.ToString("#,##0");
		}, 0, animationSteps, animationTime);


		//animating the distance UI block
		LeanTween.value(gameObject, (float f) =>
		{
			distanceBlockValue.text = Math.Round(f, 2) + " km";
		}, 0, animationDistance, animationTime);
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
        stepsBlockValue.text = "------";
        distanceBlockValue.text = "--- km";
    }

    //loads data for the UI blocks
    private async Task LoadUIBlocks()
    {
        #region request data

        DateTime date = DateTime.Now;
        TimeSpan t = new TimeSpan(0, date.Hour, date.Minute, date.Second);

        API.ApiData body = API.GenerateAPIbody(date.Subtract(t), DateTime.Now, (3600000 / 2)); //30 minute time gap

        //Debug.Log("[UIBlocksAndroid]", () => body.startTimeMillis);
        //Debug.Log("[UIBlocksAndroid]", () => body.endTimeMillis);
        //Debug.Log("[UIBlocksAndroid]", () => body.durationMillis);

        #endregion


        JsonData stepsJson = await API.GetStepsBetweenMillis(body);
        JsonData distanceJson = await API.GetDistanceBetweenMillis(body);


        //Debug.Log("[UIBlocksAndroid] " + stepsJson.ToJson());
        //Debug.Log("[UIBlocksAndroid] " + distanceJson.ToJson());

        //creating the graph with the steps
        LoadStepsDayGraph(stepsJson);


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

        distanceBlockValue.text = distanceKM.ToString();
        stepsBlockValue.text = totalSteps.ToString();
    }

    #endregion

    #region graphs

    //loads and inputs data into the steps over the day graph
    private void LoadStepsDayGraph(JsonData json)
    {
        List<double> steps = new List<double>();
        List<bool> ignorePoints = new List<bool>();

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
            ignorePoints.Add(false);
        }

        int remainder = 48 - steps.Count;

        for (int i = 0; i < remainder; i++)
        {
            steps.Add(0);
            ignorePoints.Add(true);
        }

        //Debug.Log("[DailyStepsGraphAndroid]", () => steps.Count);
        //Debug.Log("[DailyStepsGraphAndroid]", () => ignorePoints.Count);

        activityChart.SetSerieData(steps, ignorePoints);
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
        stepsBlockValue.text = "------";
        distanceBlockValue.text = "--- km";
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

        distanceBlockValue.text = distanceToday.ToString();
        stepsBlockValue.text = stepsToday.ToString();
    }

    #endregion

    #region graphs

    //loads and inputs data into the steps over the day graph
    private async Task LoadStepsDayGraph()
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


                //Debug.LogWarning("[ActivityGraphIOS] For hour " + i + ", user has done " + hourTotal);
            }
            catch (ArgumentOutOfRangeException)
            {
                //Debug.LogWarning("[ActivityGraphIOS] For hour " + i + ", user has done 0 steps, (argumant out of range)");

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

                for (int z = 0; z < stepsQuantityData.Count; z++)
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


                Debug.LogWarning("[ActivityGraphIOS] For hour " + i + ", user has done " + hourTotal);
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.LogWarning("[ActivityGraphIOS] For hour " + i + ", user has made 0 distance, (argumant out of range)");

                distanceCountPerHour.Add(0);
            }
        }

        #endregion



        //creating the x axis points
        List<string> xPoints = new List<string>();
        for (int i = 0; i < 24; i++)
        {
            xPoints.Add(i.ToString());
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