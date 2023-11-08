using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using Michsky.MUIP;
using System;
using TMPro;

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



    private void Start()
	{
		//CanvasManager.instance.mainWindow.onMainScreenLoaded += UpdateUI;
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

		//Debug.Log("[Statistics]", () => currentView);
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

#if UNITY_ANDROID || UNITY_EDITOR

	public async void GetDataDay(int dataType)
	{
        DateTime startRequest = DateTime.Today;
        DateTime endRequest = DateTime.Now;

        JsonData json1;
        JsonData json2;

        APIManager.GoogleFit.ApiData APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest, 3600000);


		#region graph


        if (dataType == 0)
		{
			json1 = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
			dataOverPeriodChart.SetYAxisNumbericFormatter("###,###,###");
		}
		else
		{
			json1 = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);
			dataOverPeriodChart.SetYAxisNumbericFormatter("0.## km");
		}


        StatisticsGraph(json1, dataType);

		#endregion


		#region this day vs last week on day

		#region getting data and setting json1 to last week and json 2 to today


        startRequest = DateTime.Today.AddDays(-7);
		endRequest = DateTime.Today.AddDays(-6);

        APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);

        if (dataType == 0) json1 = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
        else json1 = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);




        startRequest = DateTime.Today;
		endRequest = DateTime.Now;

        APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);

        if (dataType == 0) json2 = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
        else json2 = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);


		#endregion


		Debug.Log("[Statistics] 1day \n" + json1.ToJson());
		Debug.Log("[Statistics] 2day \n" + json2.ToJson());

		double todayV = double.Parse(json2[0]
			["dataset"][0]["point"][0]
			["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());

		double lastWeekV = double.Parse(json1[0]
            ["dataset"][0]["point"][0]
            ["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());

		Debug.Log("[Statistics]", () => todayV);
		Debug.Log("[Statistics]", () => lastWeekV);


		thisWeekValue.text = todayV.ToString();
		lastWeekValue.text = lastWeekV.ToString();

		#endregion
    }

	public async void GetDataWeek(int dataType)
	{
		DateTime startRequest = UsefulFunctions.StartOfWeek();
		DateTime endRequest = DateTime.Now;

		JsonData json1;
		JsonData json2;

		APIManager.GoogleFit.ApiData APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);


		#region graph

		if (dataType == 0)
		{
			json1 = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
			dataOverPeriodChart.SetYAxisNumbericFormatter("###,###,###");
		}
		else
		{
			json1 = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);
			dataOverPeriodChart.SetYAxisNumbericFormatter("0.## km");
		}

		StatisticsGraph(json1, dataType);

		#endregion


		#region this week vs last week

		#region getting data and setting json1 to last week and json 2 to today


		startRequest = UsefulFunctions.StartOfWeek();
		endRequest = startRequest.AddDays(7);

		APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest, 604800000);

		if (dataType == 0) json1 = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
		else json1 = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);




		startRequest = UsefulFunctions.StartOfWeek();
		endRequest = DateTime.Now;

		APIData = APIManager.GoogleFit.GenerateAPIbody(startRequest, endRequest);

		if (dataType == 0) json2 = await APIManager.GoogleFit.GetStepsBetweenMillis(APIData);
		else json2 = await APIManager.GoogleFit.GetDistanceBetweenMillis(APIData);


		#endregion


		Debug.Log("[Statistics] 1week \n" + json1.ToJson());
		Debug.Log("[Statistics] 2week \n" + json2.ToJson());

		double todayV = double.Parse(json2["bucket"][0]
            ["dataset"][0]["point"][0]
            ["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());

        double lastWeekV = double.Parse(json1["bucket"][0]
            ["dataset"][0]["point"][0]
            ["value"][(dataType == 0 ? "intVal" : "fpVal")].ToString());

        Debug.Log("[Statistics]", () => todayV);
        Debug.Log("[Statistics]", () => lastWeekV);


        thisWeekValue.text = todayV.ToString();
        lastWeekValue.text = lastWeekV.ToString();

        #endregion
    }

#elif UNITY_IOS

	public async void GetDataDay(int dataType)
	{
		DateTime start = DateTime.Today;
		DateTime end = DateTime.Now;


		List<APIManager.HealthKit.QuantityData> data;


		#region graph


		if (dataType == 0)
		{
			data = await APIManager.HealthKit.GetStepsList(start, end);
			dataOverPeriodChart.SetYAxisNumbericFormatter("###,###,###");
		}
		else
		{
			data = await APIManager.HealthKit.GetDistanceList(start, end);
			dataOverPeriodChart.SetYAxisNumbericFormatter("0.## km");
		}


		List<APIManager.HealthKit.OrderedQuantityData> orderedData = APIManager.HealthKit.OrderQuantityListHour(data);


		List<double> dayValues = new List<double>( new double[24]);


		for (int i = 0; i < orderedData.Count; i++)
		{
			//Debug.Log("[Statistics] DAY:   " + orderedData[i].timeOfData.ToString("t") + " value: " + orderedData[i].value);

			dayValues[orderedData[i].timeOfData.Hour - 1] = orderedData[i].value;
		}


		dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);
		dataOverPeriodChart.SetSerieData(dayValues, 0);

		#endregion


		#region this week vs last week

		if (dataType == 0) thisWeekValue.text = (await APIManager.HealthKit.GetSteps(start, end)).ToString();
		else thisWeekValue.text = (await APIManager.HealthKit.GetDistance(start, end)).ToString();

		start = DateTime.Today.AddDays(-7);
		end = start.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(1000);
		//end = DateTime.Today.AddDays(-7).AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(1000);

		//Debug.Log(start.ToString("g"));
		//Debug.Log(end.ToString("g"));

		if (dataType == 0) lastWeekValue.text = (await APIManager.HealthKit.GetSteps(start, end)).ToString();
		else lastWeekValue.text = (await APIManager.HealthKit.GetDistance(start, end)).ToString();

		#endregion
	}

	public async void GetDataWeek(int dataType)
	{
		#region graph

		DateTime start = UsefulFunctions.StartOfWeek();
		DateTime end = DateTime.Now;


		List<APIManager.HealthKit.QuantityData> data;

		if (dataType == 0)
		{
			data = await APIManager.HealthKit.GetStepsList(start, end);
			dataOverPeriodChart.SetYAxisNumbericFormatter("###,###,###");
		}
		else
		{
			data = await APIManager.HealthKit.GetDistanceList(start, end);
			dataOverPeriodChart.SetYAxisNumbericFormatter("0.## km");
		}


		List<APIManager.HealthKit.OrderedQuantityData> orderedData = APIManager.HealthKit.OrderQuantityListDay(data);


		List<double> weekValues = new List<double>(new double[7]);


		for (int i = 0; i < orderedData.Count; i++)
		{
			//Debug.Log("[Statistics] WEEK:   " + orderedData[i].timeOfData.ToString("d") + " value: " + orderedData[i].value);

			weekValues[(int)orderedData[i].timeOfData.DayOfWeek - 1] = orderedData[i].value;
		}



		dataOverPeriodChart.SetYAxisNumbericFormatter("");
		dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);
		dataOverPeriodChart.SetSerieData(weekValues, 0);

		#endregion


		#region this week vs last week



		#endregion
	}

#endif


	#endregion


	#region statistics items


#if UNITY_ANDROID || UNITY_EDITOR

	public void StatisticsGraph(JsonData json, int dataType)
    {
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
        //Debug.Log("[Statistics]", () => weekValues.Count);

        dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);
        dataOverPeriodChart.SetSerieData(weekValues, 0);
    }

#elif UNITY_IOS



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