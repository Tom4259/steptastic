using LitJson;
using Michsky.MUIP;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static APIManager.GoogleFit;
using static UnityEditor.LightingExplorerTableColumn;

public class StatisticsWindow : MonoBehaviour
{
	private class LoadedData
	{
		public string chartName;

		public List<double> chartData;


		public int stepsToday;
		public int todayLastWeek;
	}	


	public CustomDropdown dataTypeDropdown;
	public CustomDropdown viewPeriodDropdown;


	[Space]
	[Header("Activity over period chart")]
	public EasyChartSettings dataOverPeriodChart;
	public List<float> dayRoundedCorners;

	public List<float> weekRoundedCorners;


	[Space]
	[Header("Goals")]
	public TMP_Text dailyGoalTitle;
	public ProgressBar dailyGoalProgress;
	public TMP_Text dailyGoalMax;


	[Space]
	[Header("This week vs last week")]
	public TMP_Text thisWeekValue;
	public TMP_Text lastWeekValue;



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


#if UNITY_ANDROID || UNITY_EDITOR
	AndroidStatistics statisticsController;
#elif UNITY_IOS
	iOSStatistics statisticsController;
#endif



    private void Start()
	{
#if UNITY_ANDROID || UNITY_EDITOR
        statisticsController = new AndroidStatistics();
#elif UNITY_IOS
        statisticsController = new iOSStatistics();
#endif

        CanvasManager.instance.mainWindow.onMainScreenLoaded += StartWindow;
	}

	public void StartWindow()
	{
		dataTypeDropdown.SetDropdownIndex(0);
		dataTypeDropdown.items[0].OnItemSelection.Invoke();

		UpdateUI();
	}


	public void OpenWindow(int dataType)
	{
		dataTypeDropdown.SetDropdownIndex(dataType);
		dataTypeDropdown.items[dataType].OnItemSelection.Invoke();

		UpdateUI();
	}

	public void UpdateUI()
	{
		//Debug.Log("[Statistics] Updating UI");

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

                dataOverPeriodChart.SetChartTitle("Steps over the day");
                SetDayXAxis();

                
                if (loadedStepsDay == null)
                {
                    //make request here
                    GetDataDay(0);
                }
                else//load data here
                {

                }

                break;

			case Views.StepsWeek:

                dataOverPeriodChart.SetChartTitle("Steps over the week");
                SetWeekXAxis();


                if (loadedStepsWeek == null)
                {
                    //make request here
                    GetDataWeek(0);
                }
                else//load data here
                {

                }

                break;

			case Views.DistanceDay:

                dataOverPeriodChart.SetChartTitle("Distance over the day");
                SetDayXAxis();


                if (loadedDistanceDay == null)
                {
                    //make request here
                    GetDataDay(1);
                }
                else//load data here
                {

                }

                break;

			case Views.DistanceWeek:

                dataOverPeriodChart.SetChartTitle("Distance over the week");
                SetWeekXAxis();

                
                if (loadedDistanceWeek == null)
                {
                    //make request here
                    GetDataWeek(1);
                }
                else//load data here
                {

                }

                break;
		}
	}



	#region data requests, and saving to objects

#if UNITY_ANDROID || UNITY_EDITOR

    public async void GetDataDay(int dataType)
	{
        DateTime startRequest = DateTime.Today;
        DateTime endRequest = DateTime.Now;

        APIManager.GoogleFit.ApiData APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest, 3600000);

        dataOverPeriodChart.SetSerieData(await statisticsController.LoadGraph(dataType, APIData), 0);

        dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == 0 ? "###,###,###" : "0.## km");
        dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);



        Tuple<double, double> todayVsLastWeekToday = await statisticsController.LoadTodayVsLastWeekToday(dataType);

        thisWeekValue.text = todayVsLastWeekToday.Item1.ToString();
        lastWeekValue.text = todayVsLastWeekToday.Item2.ToString();
    }


	public async void GetDataWeek(int dataType)
	{
		DateTime startRequest = UsefulFunctions.StartOfWeek();
		DateTime endRequest = DateTime.Now;

		APIManager.GoogleFit.ApiData APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);

        dataOverPeriodChart.SetSerieData(await statisticsController.LoadGraph(dataType, APIData), 0);

        dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == 0 ? "###,###,###" : "0.## km");
        dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);



        Tuple<double, double> thisWeekVsLastWeek = await statisticsController.LoadThisWeekVsLastWeek(dataType);

        thisWeekValue.text = thisWeekVsLastWeek.Item1.ToString();
        lastWeekValue.text = thisWeekVsLastWeek.Item2.ToString();
    }

#elif UNITY_IOS

	public async void GetDataDay(int dataType)
	{
        dataOverPeriodChart.SetSerieData(await statisticsController.LoadGraph(dataType, true), 0);

        dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == 0 ? "###,###,###" : "0.## km");
        dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);



        Tuple<double, double> todayVsLastWeekToday = await statisticsController.LoadTodayVsLastWeekToday(dataType);

        thisWeekValue.text = todayVsLastWeekToday.Item1.ToString();
        lastWeekValue.text = todayVsLastWeekToday.Item2.ToString();

    }

	public async void GetDataWeek(int dataType)
	{
        dataOverPeriodChart.SetSerieData(await statisticsController.LoadGraph(dataType, false), 0);

        dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == 0 ? "###,###,###" : "0.## km");
        dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);



        Tuple<double, double> thisWeekVsLastWeek = await statisticsController.LoadThisWeekVsLastWeek(dataType);

        thisWeekValue.text = thisWeekVsLastWeek.Item1.ToString();
        lastWeekValue.text = thisWeekVsLastWeek.Item2.ToString();
    }

#endif

    #endregion


    #region Android statistics

    public class AndroidStatistics
    {
		public async Task<List<double>> LoadGraph(int dataType, APIManager.GoogleFit.ApiData APIData)
		{
            JsonData json;

            if (dataType == 0)
            {
                json = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
            }
            else
            {
                json = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);
            }

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
            }

            return weekValues;            
        }


        public async Task LoadGoals(JsonData json, int dataType)
		{

		}


		public async Task<Tuple<double, double>> LoadTodayVsLastWeekToday(int dataType)
		{
            DateTime startRequest = DateTime.Today;
            DateTime endRequest = DateTime.Now;
            APIManager.GoogleFit.ApiData APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);


            JsonData today;


            if (dataType == 0) today = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
            else today = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);



            startRequest = DateTime.Today.AddDays(-7);
            endRequest = DateTime.Today.AddDays(-6);
            APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);


            JsonData lastWeekToday;


            if (dataType == 0) lastWeekToday = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
            else lastWeekToday = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);


            Debug.Log("[Statistics] today \n" + today.ToJson());
            Debug.Log("[Statistics] last wk today \n" + lastWeekToday.ToJson());

            double todayV = double.Parse(today[0]
                ["dataset"][0]["point"][0]
                ["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());

            double lastWeekV = double.Parse(lastWeekToday[0]
                ["dataset"][0]["point"][0]
                ["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());

            Debug.Log("[Statistics]", () => todayV);
            Debug.Log("[Statistics]", () => lastWeekV);


            return new Tuple<double, double>(todayV, lastWeekV);
        }

		public async Task<Tuple<double, double>> LoadThisWeekVsLastWeek(int dataType)
		{
            DateTime startRequest = UsefulFunctions.StartOfWeek();
            DateTime endRequest = DateTime.Now;
            APIManager.GoogleFit.ApiData APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);


            JsonData thisWeek;


            if (dataType == 0) thisWeek = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
            else thisWeek = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);


            startRequest = UsefulFunctions.StartOfWeek().AddDays(-7);
            endRequest = startRequest.AddDays(7);
            APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest, 604800000);


            JsonData lastWeek;


            if (dataType == 0) lastWeek = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
            else lastWeek = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);


            Debug.Log("[Statistics] thisWeek \n" + thisWeek.ToJson());
            Debug.Log("[Statistics] lastWeek \n" + lastWeek.ToJson());

            double todayV = double.Parse(thisWeek["bucket"][0]
            ["dataset"][0]["point"][0]
                ["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());

            double lastWeekV = double.Parse(lastWeek["bucket"][0]
            ["dataset"][0]["point"][0]
                ["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());


            return new Tuple<double, double>(todayV, lastWeekV);
        }
    }

	#endregion


	#region iOS statistics

    public class iOSStatistics
    {
        public async Task<List<double>> LoadGraph(int dataType, bool isDayData)
        {
            if (isDayData)
			{
                DateTime start = DateTime.Today;
                DateTime end = DateTime.Now;

                List<APIManager.HealthKit.QuantityData> data;

                if (dataType == 0)
                {
                    data = await APIManager.HealthKit.GetStepsList(start, end);
                }
                else
                {
                    data = await APIManager.HealthKit.GetDistanceList(start, end);
                }

                List<APIManager.HealthKit.OrderedQuantityData> orderedData = APIManager.HealthKit.OrderQuantityListHour(data);

                List<double> dayValues = new List<double>(new double[24]);


                for (int i = 0; i < orderedData.Count; i++)
                {
                    //Debug.Log("[Statistics] DAY:   " + orderedData[i].timeOfData.ToString("t") + " value: " + orderedData[i].value);

                    dayValues[orderedData[i].timeOfData.Hour] = orderedData[i].value;
                }


                return dayValues;
            }


			else//dropdown is selected to week, so show the data over the week
			{
                DateTime start = UsefulFunctions.StartOfWeek();
                DateTime end = DateTime.Now;

                List<APIManager.HealthKit.QuantityData> data;

                if (dataType == 0)
                {
                    data = await APIManager.HealthKit.GetStepsList(start, end);
                }
                else
                {
                    data = await APIManager.HealthKit.GetDistanceList(start, end);
                }

                List<APIManager.HealthKit.OrderedQuantityData> orderedData = APIManager.HealthKit.OrderQuantityListDay(data);

				List<double> weekValues = new List<double>(new double[7]);


                for (int i = 0; i < orderedData.Count; i++)
                {
                    //Debug.Log("[Statistics] WEEK:   " + orderedData[i].timeOfData.ToString("d") + " value: " + orderedData[i].value);

                    weekValues[(int)orderedData[i].timeOfData.DayOfWeek - 1] = orderedData[i].value;
                }


                return weekValues;
            }
        }


        public async Task LoadGoals(int dataType)
        {

        }


        public async Task<Tuple<double, double>> LoadTodayVsLastWeekToday(int dataType)
        {
            DateTime start = DateTime.Today;
            DateTime end = DateTime.Now;

            double today;

            if (dataType == 0)
            {
                today = await APIManager.HealthKit.GetSteps(start, end);
            }
            else
            {
                today = await APIManager.HealthKit.GetDistance(start, end);
            }


            start = DateTime.Today.AddDays(-7);
            end = start.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(1000);

            double lastWeekToday;

            if (dataType == 0)
            {
                lastWeekToday = await APIManager.HealthKit.GetSteps(start, end);
            }
            else
            {
                lastWeekToday = await APIManager.HealthKit.GetDistance(start, end);
            }

            return new Tuple<double, double>(today, lastWeekToday);
        }

        public async Task<Tuple<double, double>> LoadThisWeekVsLastWeek(int dataType)
        {
            DateTime start = UsefulFunctions.StartOfWeek();
            DateTime end = DateTime.Now;

            double thisWeek;

            if (dataType == 0)
            {
                thisWeek = await APIManager.HealthKit.GetSteps(start, end);
            }
            else
            {
                thisWeek = await APIManager.HealthKit.GetDistance(start, end);
            }


            start = UsefulFunctions.StartOfWeek().AddDays(-7);
            end = start.AddDays(7);


            double lastWeek;

            if (dataType == 0)
            {
                lastWeek = await APIManager.HealthKit.GetSteps(start, end);
            }
            else
            {
                lastWeek = await APIManager.HealthKit.GetDistance(start, end);
            }

            return new Tuple<double, double>(thisWeek, lastWeek);
        }        
    }


	#endregion


	#region helpers

    private void SetDayXAxis()
    {
        List<string> xAxisPoints = new List<string>
        {
            "12 am",
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
            "11 pm",
            "",//24

		};


        dataOverPeriodChart.SetXAxisSplitNumber(14);
        dataOverPeriodChart.SetXAxisPoints(xAxisPoints);
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


        dataOverPeriodChart.SetXAxisPoints(xAxisPoints);
    }

	#endregion
}