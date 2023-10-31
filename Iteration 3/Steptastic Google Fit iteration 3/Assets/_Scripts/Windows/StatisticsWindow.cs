using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using Michsky.MUIP;
using XCharts.Runtime;
using System;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class StatisticsWindow : MonoBehaviour
{
    private class LoadedData
    {
        public string chartName;


        public int stepsToday;
        public int todayLastWeek;
    }


    public CustomDropdown dataTypeDropdown;
    public CustomDropdown viewPeriodDropdown;


    [Space]
    [Header("Activity over period chart")]
    public EasyChartSettings dataOverPeriodChart;
    public List<float> dayRoundedCorners = new List<float>
    {
        8.36f,
        0.72f,
        0.86f,
        0.81f
    };

    public List<float> weekRoundedCorners = new List<float>
    {
        20.5f,
        22.27f,
        0.86f,
        0.81f
    };




    private RectTransform RT;
    private Vector2 startPosition;

    private LoadedData loadedStepsDay = null;
    private LoadedData loadedStepsWeek = null;

    private LoadedData loadedDistanceDay = null;
    private LoadedData loadedDistanceWeek = null;


    private enum Views
    {
        StepsDay, 
        StepsWeek,
        DistanceDay,
        DistanceWeek,
    }

    private Views currentView;


    private void Start()
    {
        RT = GetComponent<RectTransform>();
        startPosition = new Vector2 (CanvasManager.instance.GetComponent<CanvasScaler>().referenceResolution.x, 0);
    }


    public void OpenWindow(int dataType)
    {
        RT.anchoredPosition = startPosition;

        dataTypeDropdown.SetDropdownIndex(dataType);
        dataTypeDropdown.items[dataType].OnItemSelection.Invoke();

        LeanTween.move(RT, Vector2.zero, CanvasManager.windowAnimationTime).setEaseInOutCubic();

        UpdateUI();
    }

    public void CloseWindow()
    {
        LeanTween.move(RT, startPosition, CanvasManager.windowAnimationTime / 2);
    }

    public void UpdateUI()
    {
        Debug.Log("[Statistics] Updating UI");

        //updating the new view
        if (dataTypeDropdown.selectedItemIndex == 0 && viewPeriodDropdown.selectedItemIndex == 0)
        {
            currentView = Views.StepsDay;
        }
        else if (dataTypeDropdown.selectedItemIndex == 0 && viewPeriodDropdown.selectedItemIndex == 1)
        {
            currentView = Views.StepsWeek;
        }
        else if (dataTypeDropdown.selectedItemIndex == 1 && viewPeriodDropdown.selectedItemIndex == 0)
        {
            currentView = Views.DistanceDay;
        }
        else if (dataTypeDropdown.selectedItemIndex == 1 && viewPeriodDropdown.selectedItemIndex == 1)
        {
            currentView = Views.DistanceWeek;
        }


        switch (currentView)
        {
            case Views.StepsDay:
                LoadStepsDay();
                break;

            case Views.StepsWeek:
                LoadStepsWeek();
                break;

            case Views.DistanceDay:
                LoadDistanceDay();
                break;

            case Views.DistanceWeek:
                LoadDistanceWeek();
                break;
        }

        Debug.Log("[Statistics]", () => currentView);
    }



    #region views

    private void LoadStepsDay()
    {
        dataOverPeriodChart.SetChartTitle("Steps over the day");
        SetDayXAxis();


        //load data here
        if(loadedStepsDay == null)
        {
            //make request here
            GetDataDay(0);
        }
        else
        {

        }
    }

    private void LoadStepsWeek()
    {
        dataOverPeriodChart.SetChartTitle("Steps over the week");
        SetWeekXAxis();


        //load data here
        if (loadedStepsWeek == null)
        {
            //make request here
            GetDataWeek(0);
        }
        else
        {

        }
    }

    private void LoadDistanceDay()
    {
        dataOverPeriodChart.SetChartTitle("Distance over the day");
        SetDayXAxis();


        //load data here
        if (loadedDistanceDay == null)
        {
            //make request here
            GetDataDay(1);
        }
        else
        {

        }
    }

    private void LoadDistanceWeek()
    {
        dataOverPeriodChart.SetChartTitle("Distance over the week");
        SetWeekXAxis();


        //load data here
        if (loadedDistanceWeek == null)
        {
            //make request here
            GetDataWeek(1);
        }
        else
        {

        }
    }

    #region data requests, and saving to objects

#if UNITY_ANDROID //|| UNITY_EDITOR

    public async void GetDataDay(int dataType)
    {
        DateTime start = DateTime.Today;
        DateTime end = DateTime.Now;


        JsonData json;

        APIManager.GoogleFit.ApiData apiData = APIManager.GoogleFit.GenerateAPIbody(start, end, 3600000);

        if (dataType == 0) json = await APIManager.GoogleFit.GetStepsBetweenMillis(apiData);
        else json = await APIManager.GoogleFit.GetDistanceBetweenMillis(apiData);



        //Debug.Log(json.ToJson().ToString());

        

        List<double> dayValues = new List<double>();
        float totalValue = 0;

        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            double item = 0;

            try
            {
                item = double.Parse(stepData[0]["value"][0][(dataType == 0 ? "intVal" : "fpVal")].ToString());

                totalValue += (float)item;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            dayValues.Add(item);

            //Debug.Log("[Statistics]", () => item);
        }

        //Debug.Log("[Statistics]", () => totalValue);
        Debug.Log("[Statistics]", () => dayValues.Count);

        dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);
        dataOverPeriodChart.SetSerieData(dayValues, 0);

        //dataOverPeriodChart.RefreshGraph(true);
    }

    public async void GetDataWeek(int dataType)
    {
        DateTime start = DateTime.Today.AddDays(- ((int)DateTime.Today.DayOfWeek - 1));
        DateTime end = DateTime.Now;


        JsonData json;

        APIManager.GoogleFit.ApiData apiData = APIManager.GoogleFit.GenerateAPIbody(start, end);

        if (dataType == 0) json = await APIManager.GoogleFit.GetStepsBetweenMillis(apiData);
        else json = await APIManager.GoogleFit.GetDistanceBetweenMillis(apiData);



        //Debug.Log(json.ToJson().ToString());



        List<double> weekValues = new List<double>();
        float totalValue = 0;

        for (int i = 0; i < json["bucket"].Count; i++)
        {
            JsonData stepData = json["bucket"][i]["dataset"][0]["point"];

            double item = 0;

            try
            {
                item = double.Parse(stepData[0]["value"][0][(dataType == 0 ? "intVal" : "fpVal")].ToString());
                
                totalValue += (float)item;
            }
            catch (ArgumentOutOfRangeException) { }
            catch (KeyNotFoundException) { }

            weekValues.Add(item);

            //Debug.Log("[Statistics]", () => item);
        }

        //Debug.Log("[Statistics]", () => totalValue);
        Debug.Log("[Statistics]", () => weekValues.Count);

        dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);
        dataOverPeriodChart.SetSerieData(weekValues, 0);

        //dataOverPeriodChart.RefreshGraph(true);
    }

#elif UNITY_IOS  

    public async void GetDataDay(int dataType)
    {
        DateTime start = DateTime.Today;
        DateTime end = DateTime.Now;

        List<APIManager.HealthKit.QuantityData> data;


        if(dataType == 0) data = await APIManager.HealthKit.GetStepsList(start, end);
        else data = await APIManager.HealthKit.GetDistanceList(start, end);


        List<APIManager.HealthKit.OrderedQuantityData> orderedData = APIManager.HealthKit.OrderQuantityListHour(data);


        for (int i = 0; i < orderedData.Count; i++)
        {
            Debug.Log("[Statistics] DAY:   " + orderedData[i].timeOfData.ToString("g") + " value: " + orderedData[i].value);
        }
    }

    public async void GetDataWeek(int dataType)
    {
        DateTime start = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        DateTime end = DateTime.Now;

        List<APIManager.HealthKit.QuantityData> data;


        if (dataType == 0) data = await APIManager.HealthKit.GetStepsList(start, end);
        else data = await APIManager.HealthKit.GetDistanceList(start, end);


        List<APIManager.HealthKit.OrderedQuantityData> orderedData = APIManager.HealthKit.OrderQuantityListDay(data);


        for (int i = 0; i < orderedData.Count; i++)
        {
            Debug.Log("[Statistics] WEEK:   " + orderedData[i].timeOfData.ToString("g") + " value: " + orderedData[i].value);
        }
    }

#endif


    #endregion

    #region helpers

    private void SetDayXAxis()
    {
        List<string> xAxisPoints = new List<string>
        {
            "",
            "",
            "",
            "",
            "",
            "6 am",//6
            "",
            "",
            "",
            "",
            "",
            "12 pm",//12
            "",
            "",
            "",
            "",
            "",
            "6 pm",//18
            "",
            "",
            "",
            "",
            "",
            "",//24

        };

        dataOverPeriodChart.SetSingleAxisSplitNumber(14);
        dataOverPeriodChart.SetSingleAxisPoints(xAxisPoints);
        dataOverPeriodChart.SetXAxisPoints(new List<string>(new string[24]));        
    }

    private void SetWeekXAxis()
    {
        List<string> xAxisPoints = new List<string>
        {
            "Mon",
            "Tue",
            "Wed",
            "Thu",
            "Fri",
            "Sat",
            "Sun"
        };

        dataOverPeriodChart.SetSingleAxisPoints(xAxisPoints);
        dataOverPeriodChart.SetXAxisPoints(new List<string>(new string[7]));
    }

    #endregion


#endregion
}