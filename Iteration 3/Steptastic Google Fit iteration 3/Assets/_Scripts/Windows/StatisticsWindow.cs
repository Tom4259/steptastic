using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using Michsky.MUIP;
using XCharts.Runtime;

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
    public EasyChartSettings dataOverPeriodChart;




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
        LeanTween.move(RT, startPosition, CanvasManager.windowAnimationTime);
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
    }

    private void LoadStepsWeek()
    {
        dataOverPeriodChart.SetChartTitle("Steps over the week");
        SetWeekXAxis();

        //load data here
    }

    private void LoadDistanceDay()
    {
        dataOverPeriodChart.SetChartTitle("Distance over the day");
        SetDayXAxis();

        //load data here
    }

    private void LoadDistanceWeek()
    {
        dataOverPeriodChart.SetChartTitle("Distance over the week");
        SetWeekXAxis();

        //load data here
    }

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

        dataOverPeriodChart.RefreshGraph(true);
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
            "Sun",

        };

        dataOverPeriodChart.SetSingleAxisPoints(xAxisPoints);

        dataOverPeriodChart.RefreshGraph(true);
    }

    #endregion


    #endregion
}