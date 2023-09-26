using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XCharts;
using System;
using LitJson;
using API = APIManager;

public class MainWindow : MonoBehaviour
{

    public void StartMainWindow()
    {
        calculateUserProgress();
    }


    private void calculateUserProgress()
    {
        //if start date is now, then make it beggining of the day
        DateTime startDate = PlayerPrefsX.GetDateTime(PlayerPrefsLocations.User.Challenge.startDate, DateTime.Today);
        DateTime now = DateTime.Now;

        double dif = (now - startDate).TotalMinutes;

        //Debug.Log("saved date: " + startDate.ToString());
        //Debug.Log("now: " + now.ToString());
        //Debug.Log("dif: " + dif.ToString());

        API.apiData data = new API.apiData();

        //if less than a minute, get data in 30 minute intervals
        if (dif < 60) data = API.GenerateAPIbody(startDate, now, 1800000);

        //if less than 1 day, get data in hours
        else if (dif < 1440) data = API.GenerateAPIbody(startDate, now, 3600000);

        //if greater than 1 day (in minutes) get data with interval of 1 day
        else data = API.GenerateAPIbody(startDate, now);


        StartCoroutine(API.GoogleFit.GetStepsBetweenMillis(data, calculateUserProgress));
    }

    private void calculateUserProgress(JsonData json)
    {
        //Debug.Log(json.ToJson());

        int totalStepCount = 0;

        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            try
            {
                totalStepCount += int.Parse(stepData[0]["value"][0]["intVal"].ToString());
            }
            catch(ArgumentOutOfRangeException e) { }

            //Debug.Log(totalStepCount + " for index " + i);
        }

        Debug.Log("total: " +  totalStepCount);
    }
}
