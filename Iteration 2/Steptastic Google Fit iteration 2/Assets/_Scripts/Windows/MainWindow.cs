using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XCharts;
using System;
using LitJson;
using API = APIManager.GoogleFit;
using UnityEngine.UI;
#if !UNITY_EDITOR
//using Debug = Sisus.Debugging.Debug;
#endif

public class MainWindow : MonoBehaviour
{
	[Header("Progress bar")]
	public Image progressBar;

	[Space(10)]
	public TMP_Text userPercentText;
	[TextArea]
	public string challengeDescriptionText;
	public TMP_Text challengeDescriptionLabel;

	[Space(20)]
	[Header("Map visualisation")]
	public Image mapImage;


	public void StartMainWindow()
	{
		//shows the user their start and end location
		challengeDescriptionLabel.text = challengeDescriptionText.Replace("{{startLocation}}",
			PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationCapital) + ", " +
			PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationName)).Replace("{{endLocation}}",
			PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationCapital) + ", " +
			PlayerPrefsX.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationName));


		calculateUserProgress();
	}

	private void continueMainWindow()
	{
		getMapImage();
	}


	#region progress to target

	/// <summary>
	/// calculates the user progress based on the amount of distance the user has covered since the start date
	/// </summary>
	private void calculateUserProgress()
	{
		//if start date is now, then make it beggining of the day
		DateTime startDate = PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, DateTime.Today);
		DateTime now = DateTime.Now;

		double dif = (now - startDate).TotalMinutes;

		Debug.Log("[" + GetType().Name + "]", () => startDate);
		Debug.Log("[" + GetType().Name + "]", () => now);
		Debug.Log("[" + GetType().Name + "]", () => dif);

		API.apiData data = new API.apiData();

		//if less than a minute, get data in 30 minute intervals
		if (dif < 60) data = API.GenerateAPIbody(startDate, now, 1800000);

		//if less than 1 day, get data in hours
		else if (dif < 1440) data = API.GenerateAPIbody(startDate, now, 3600000);

		//if greater than 1 day (in minutes) get data with interval of 1 day
		else data = API.GenerateAPIbody(startDate, now);
		

		StartCoroutine(API.GetDistanceBetweenMillis(data, calculateUserProgress));
	}


	/// <summary>
	/// dissects the returned data from Google and calculates the percentage completion
	/// </summary>
	/// <param name="json"></param>
	private void calculateUserProgress(JsonData json)
	{
		Debug.Log("[" + GetType().Name + "]" + json.ToJson());

		float totalMeters = 0;

		for (int i = 0; i < json["bucket"].Count; i++)
		{
			JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

			try
			{
				totalMeters += float.Parse(stepData[0]["value"][0]["fpVal"].ToString());
			}
			catch (ArgumentOutOfRangeException) { }

			Debug.Log("[" + GetType().Name + "]", () => totalMeters);
		}

		Debug.Log("[" + GetType().Name + "]", () => totalMeters);

		float distanceToTarget = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget, -1);
		float userKM = totalMeters / 1000;

		float percentage = (userKM / distanceToTarget) * 100;

		Debug.Log("[" + GetType().Name + "]", () => percentage);

		progressBar.fillAmount = percentage / 100;

		PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted, percentage);
		PlayerPrefs.Save();


		userPercentText.text = Math.Round(percentage, 2).ToString() + "%";


		continueMainWindow();
    }

	#endregion

	#region map image

	/// <summary>
	/// requests an image from MapQuest displaying the user's challenge data
	/// </summary>
	private void getMapImage()
	{
		#region variables

		float userLat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[0]);
		float userLong = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[1]);
		float targetLat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[0]);
		float targetLong = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[1]);

		float currentPointLat = latLongBetweenTwoLatLongs(userLat, userLong, targetLat, targetLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item1;
		float currentPointLong = latLongBetweenTwoLatLongs(userLat, userLong, targetLat, targetLong, PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted)).Item2;

		#endregion

		Debug.Log("[" + GetType().Name + "]", () => userLat);
		Debug.Log("[" + GetType().Name + "]", () => userLong);
		Debug.Log("[" + GetType().Name + "]", () => targetLat);
		Debug.Log("[" + GetType().Name + "]", () => targetLong);

		Debug.Log("[" + GetType().Name + "]", () => currentPointLat);
		Debug.Log("[" + GetType().Name + "]", () => currentPointLong);

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

			imageToSet = mapImage
		};


		StartCoroutine(APIManager.MapQuest.getMapImage(mData));
	}

	#region helpers

	//may need to update these values. test more
	private int getMapZoomApproximation()
	{
		int dist = (int)PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget);

		Debug.Log("[" + GetType().Name + "]", () => dist);

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
}