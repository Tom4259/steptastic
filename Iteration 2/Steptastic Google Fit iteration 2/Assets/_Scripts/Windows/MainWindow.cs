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
	public EasyChartSettings dayStepsChart;


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

        loadingScreen.SetActive(false);

        animateScreen();
    }

	private void animateScreen()
	{
        float percentage = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted);

        LeanTween.value(gameObject, (float f) =>
        {
            progressBar.currentPercent = f;
        }, 0, percentage, animationTime);

		dayStepsChart.AnimateGraph();


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

		Debug.Log("[" + GetType().Name + "]", () => startDate);
		Debug.Log("[" + GetType().Name + "]", () => now);
		Debug.Log("[" + GetType().Name + "]", () => dif);

		API.ApiData data = new API.ApiData();

		//if less than a minute, get data in 30 minute intervals
		if (dif < 60) data = API.GenerateAPIbody(startDate, now, 1800000);

		//if less than 1 day, get data in hours
		else if (dif < 1440) data = API.GenerateAPIbody(startDate, now, 3600000);

		//if greater than 1 day (in minutes) get data with interval of 1 day
		else data = API.GenerateAPIbody(startDate, now);



		JsonData json = await API.GetDistanceBetweenMillis(data);
        //Debug.Log("[" + GetType().Name + "]" + json.ToJson());



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

            //Debug.Log("[" + GetType().Name + "]", () => totalMeters);
        }

        Debug.Log("[" + GetType().Name + "]", () => totalMeters);

        float distanceToTarget = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget, -1);
        float userKM = totalMeters / 1000;

        float percentage = (userKM / distanceToTarget) * 100;

        Debug.Log("[" + GetType().Name + "]", () => percentage);


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

		float currentPointLat = latLongBetweenTwoLatLongs(userLat, userLong, targetLat, targetLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item1;
		float currentPointLong = latLongBetweenTwoLatLongs(userLat, userLong, targetLat, targetLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item2;

        #endregion

        #region debugging variables

        Debug.Log("[" + GetType().Name + "]", () => userLat);
		Debug.Log("[" + GetType().Name + "]", () => userLong);
		Debug.Log("[" + GetType().Name + "]", () => targetLat);
		Debug.Log("[" + GetType().Name + "]", () => targetLong);

		Debug.Log("[" + GetType().Name + "]", () => currentPointLat);
		Debug.Log("[" + GetType().Name + "]", () => currentPointLong);

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

			zoom = getMapZoomApproximation(),

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

    #region helpers

    //may need to update these values. test more
    private int getMapZoomApproximation()
	{
		int dist = (int)PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget);

		//Debug.Log("[" + GetType().Name + "]", () => dist);

		if (dist <= 75)
		{
			return 6;
		}
		else if (dist <= 800)
		{
			return 5;
		}
		if (dist <= 2000)
		{
			return 4;
		}
		else if (dist <= 4500)
		{
			return 3;
		}
		else if (dist <= 8000)
		{
			return 2;
		}
		else
		{
			return 1;//maybe
		}
	}

	private Tuple<float, float> latLongBetweenTwoLatLongs(float lat1, float long1, float lat2, float long2, float per)
	{
		per /= 100;

		float lat = lat1 + (lat2 - lat1) * per;
		float lng = long1 + (long2 - long1) * per;

		Debug.Log("[MainWindow] lat long between lat longs: " + lat + "," + lng);

		return Tuple.Create(lat, lng);
	}

    #endregion

    #endregion

    #region UI blocks

	private void ResetUIBlockText()
	{
		stepsBlockValue.text = "------";
		distanceBlockValue.text = "--- km";
	}

	private async Task LoadUIBlocks()
	{
        #region request data

        DateTime date = DateTime.Now;
        TimeSpan t = new TimeSpan(0, date.Hour, date.Minute, date.Second);

        API.ApiData body = API.GenerateAPIbody(date.Subtract(t), DateTime.Now, (3600000 / 2)); //30 minute time gap

        //Debug.Log("[" + GetType().Name + "]", () => body.startTimeMillis);
        //Debug.Log("[" + GetType().Name + "]", () => body.endTimeMillis);
        //Debug.Log("[" + GetType().Name + "]", () => body.durationMillis);

        #endregion


        JsonData stepsJson = await API.GetStepsBetweenMillis(body);
        JsonData distanceJson = await API.GetDistanceBetweenMillis(body);


        //Debug.Log("[" + GetType().Name + "] " + stepsJson.ToJson());
        //Debug.Log("[" + GetType().Name + "] " + distanceJson.ToJson());

        //creating the graph with the steps
        LoadGraphs(stepsJson);


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

		Debug.Log("[" + GetType().Name + "]", () => totalSteps);


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

        Debug.Log("[" + GetType().Name + "]", () => totalMeters);

		#endregion

		double distanceKM = Math.Round((totalMeters / 1000), 2);

		distanceBlockValue.text = distanceKM.ToString();
		stepsBlockValue.text = totalSteps.ToString();
    }

    #endregion

    #region graphs

	private void LoadGraphs(JsonData json)
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

		Debug.Log(() => steps.Count);

		dayStepsChart.SetSerieData(steps);
	}

    #endregion
}