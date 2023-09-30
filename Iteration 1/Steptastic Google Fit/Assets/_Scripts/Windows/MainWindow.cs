using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XCharts;
using System;
using LitJson;
using API = APIManager.GoogleFit;
using UnityEngine.UI;
using Unity.Mathematics;

public class MainWindow : MonoBehaviour
{
    [Header("Progress bar")]
    public Image progressBar;

    [Space(20)]
    [Header("Map visualisation")]
    public Image mapImage;


    public void StartMainWindow()
    {
        calculateUserProgress();
        getMapImage();
    }


    #region progress to target

    private void calculateUserProgress()
    {
        //if start date is now, then make it beggining of the day
        DateTime startDate = PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, DateTime.Today);
        DateTime now = DateTime.Now;

        double dif = (now - startDate).TotalMinutes;

        //Debug.Log("saved date: " + startDate.ToString());
        //Debug.Log("now: " + now.ToString());
        //Debug.Log( "dif: " + dif.ToString());

        API.apiData data = new API.apiData();

        //if less than a minute, get data in 30 minute intervals
        if (dif < 60) data = API.GenerateAPIbody(startDate, now, 1800000);

        //if less than 1 day, get data in hours
        else if (dif < 1440) data = API.GenerateAPIbody(startDate, now, 3600000);

        //if greater than 1 day (in minutes) get data with interval of 1 day
        else data = API.GenerateAPIbody(startDate, now);


        StartCoroutine(API.GetDistanceBetweenMillis(data, calculateUserProgress));
    }

    private void calculateUserProgress(JsonData json)
    {
        Debug.Log(json.ToJson());

        float totalMeters = 0;

        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            try
            {
                totalMeters += float.Parse(stepData[0]["value"][0]["fpVal"].ToString());
            }
            catch (ArgumentOutOfRangeException) { }

            Debug.Log(totalMeters);
        }

        Debug.Log(totalMeters);

        float distanceToTarget = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget, -1);
        float userKM = totalMeters / 1000;

        float percentage = (userKM / distanceToTarget) * 100;

        Debug.Log(percentage);

        progressBar.fillAmount = percentage / 100;

        PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.percentCompleted, percentage);
    }

    #endregion

    #region map image

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

        //can possible optimise more
        APIManager.MapQuest.MapData mData = new APIManager.MapQuest.MapData
        {
            startLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
            endLocation = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

            location1 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong),
            location2 = PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong),

            currentLattitude = currentPointLat,
            currentLongitude = currentPointLong,

            imageHeight = (int)mapImage.rectTransform.rect.height,
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

        Debug.Log(dist);

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

        Debug.Log("[" + GetType().Name + "] " + lat + "," + lng);

        return Tuple.Create(lat, lng);
    }

    #endregion

    #endregion
}
