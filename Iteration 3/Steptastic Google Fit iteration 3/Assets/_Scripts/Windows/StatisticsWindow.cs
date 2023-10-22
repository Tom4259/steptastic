using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using Michsky.MUIP;
using static UnityEditor.LightingExplorerTableColumn;
using XCharts.Runtime;

public class StatisticsWindow : MonoBehaviour
{
    private class LoadedData
    {
        public string chartName;


        public int stepsToday;
        public int todayLastWeek;
    }


    public CustomDropdown viewPeriodDropdown;
    public CustomDropdown dataTypeDropdown;


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
        //updating the new view
        if (viewPeriodDropdown.selectedItemIndex == 0 && dataTypeDropdown.selectedItemIndex == 0)
        {
            currentView = Views.StepsDay;
        }
        else if (viewPeriodDropdown.selectedItemIndex == 0 && dataTypeDropdown.selectedItemIndex == 1)
        {
            currentView = Views.DistanceDay;
        }
        else if (viewPeriodDropdown.selectedItemIndex == 1 && dataTypeDropdown.selectedItemIndex == 0)
        {
            currentView = Views.DistanceDay;
        }
        else if (viewPeriodDropdown.selectedItemIndex == 1 && dataTypeDropdown.selectedItemIndex == 1)
        {
            currentView = Views.DistanceWeek;
        }


        switch (currentView)
        {
            case Views.StepsDay:
                LoadStepsday();
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
    }

    #region views


    private void LoadStepsday()
    {
        List<string> xAxisPoints = new List<string>
        {

        };

        dataOverPeriodChart.SetXAxisPoints(xAxisPoints);
    }

    private void LoadStepsWeek()
    {

    }

    private void LoadDistanceDay()
    {

    }

    private void LoadDistanceWeek()
    {

    }


    #endregion
}
