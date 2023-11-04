using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.MUIP;
using System.Threading.Tasks;
using LitJson;
using System;

public class GoalsWindow : MonoBehaviour
{
    public GameObject setGoalsWindow;


    [Space]
    [Header("Step goals")]
    public VerticalProgressBar[] daysProgressBars;



    private void Start()
    {
        CanvasManager.instance.mainWindow.onMainScreenLoaded += LoadGoalsWindow;

        setGoalsWindow.SetActive(false);

        if(!PlayerPrefsX.GetBool(PlayerPrefsLocations.User.CompletedWindows.setGoals, false))
        {
            //show window to set goals
            setGoalsWindow.SetActive(true);
        }
    }

    public void LoadGoalsWindow()
    {
        LoadWeekProgressBars();
    }

    private async Task LoadWeekProgressBars()
    {
        DateTime start = DateTime.Today.AddDays(- ((int)DateTime.Today.DayOfWeek + 1));
        DateTime end = DateTime.Now;

        List<int> stepsOverDays = new List<int>();


        int stepGoal = PlayerPrefsX.GetInt(PlayerPrefsLocations.User.Goals.dailyStepGoal, 10000);



#if UNITY_ANDROID || UNITY_EDITOR

        APIManager.GoogleFit.ApiData data = APIManager.GoogleFit.GenerateAPIbody(start, end);
        JsonData json = await APIManager.GoogleFit.GetStepsBetweenMillis(data);

        //dissect data here


        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            int item = 0;

            try
            {
                item = int.Parse(stepData[0]["value"][0]["intVal"].ToString());
            }
            catch (ArgumentOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            stepsOverDays.Add(item);
        }


        Debug.Log(() => stepsOverDays.Count);


        for (int i = 0; i < daysProgressBars.Length; i++)
        {
            //daysProgressBars[i].percent = (stepsOverDays[i] / stepGoal) * 100;

            LeanTween.value(gameObject, (float f) =>
            {

            }, 0, (stepsOverDays[i] / stepGoal) * 100, 0.125f);
        }

#elif UNITY_IOS

        List<APIManager.HealthKit.QuantityData> data = await APIManager.HealthKit.GetStepsList(start, end);

        

#endif
    }
}