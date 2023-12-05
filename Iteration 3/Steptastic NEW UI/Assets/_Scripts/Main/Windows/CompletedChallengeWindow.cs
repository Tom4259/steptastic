using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using LitJson;
using TMPro;
using UnityEngine.UI;

public class CompletedChallengeWindow : MonoBehaviour
{
	private RectTransform rect;

	[Space]
	public TMP_Text challengeDescriptionText;
	public Image mapImage;


	private void Awake()
	{
		rect = GetComponent<RectTransform>();
	}



	public void OpenWindow(bool animation = true)
	{
		if (animation)
		{
			LeanTween.value(gameObject, (float f) =>
			{
				rect.anchoredPosition = new Vector2(f, 0);
			}, -800, 0, CanvasManager.fastWindowAnimationTime).setEaseInOutCubic();
		}
		else
		{
			rect.anchoredPosition = new Vector2(0, 0);
		}
	}

	public void CloseWindow(bool animation = true)
	{
		if (animation)
		{
			LeanTween.value(gameObject, (float f) =>
			{
				rect.anchoredPosition = new Vector2(f, 0);
			}, 0, -800, CanvasManager.fastWindowAnimationTime).setEaseInOutCubic();
		}
		else
		{
			rect.anchoredPosition = new Vector2(-800, 0);
		}
	}


	public async void LoadChallengeInfo()
	{
		double totalSteps = await GetTotalSteps();
		float totalDistance = GetTotalDistance();

		Debug.Log(() => totalSteps);
		Debug.Log(() => totalDistance);

		APIManager.MapQuest.MapData mData = new APIManager.MapQuest.MapData
		{
			startLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
			endLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

			location1 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
			location2 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

			addCurrentLocation = false,

            imageHeight = (int)Math.Round(mapImage.rectTransform.rect.height),
            imageWidth = (int)Math.Round(mapImage.rectTransform.rect.width),

            zoom = UsefulFunctions.GetMapZoomApproximation() + 1
        };

        mapImage.sprite = await APIManager.MapQuest.GetMapImage(mData);

		challengeDescriptionText.text = challengeDescriptionText.text.Replace("{{steps}}", totalSteps.ToString())
			.Replace("{{distance}}", totalDistance.ToString());

    }



	private async Task<double> GetTotalSteps()
	{
		DateTime start = PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate);
		DateTime end = DateTime.Now;

#if UNITY_ANDROID || UNITY_EDITOR

		APIManager.GoogleFit.ApiData apiData = APIManager.GoogleFit.GenerateAPIbody(start, end);
		JsonData json = await APIManager.GoogleFit.GetStepsBetweenMillis(apiData);

		double totalSteps = 0;

		for (int i = 0; i < json["bucket"].Count; i++)
		{
			try
			{
				int item = int.Parse(json["bucket"][i]["dataset"][0]["point"][0]["value"][0]["intVal"].ToString());

				totalSteps += item;
			}
			catch (KeyNotFoundException) { }
			catch (ArgumentOutOfRangeException) { }
		}

		return totalSteps;


#elif UNITY_IOS

		double steps = await APIManager.HealthKit.GetSteps(start, end);

		return steps;

#endif

	}

	private float GetTotalDistance()
	{
		return PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget);
	}
}
