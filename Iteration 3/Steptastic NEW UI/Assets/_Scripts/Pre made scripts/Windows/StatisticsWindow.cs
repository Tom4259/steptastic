using LitJson;
using Michsky.MUIP;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static APIManager;

public class StatisticsWindow : MonoBehaviour
{
	private class LoadedData
	{
		public List<double> chartData;

		public double valueThisWeek;
		public double valueLastWeek;

		public double value;
		public Sprite icon;
		public Color parentColour;

		public string goalTitle;
		public string percentText;
		public string dailyGoalMaxText;

		public float progressBarMaxValue;
		public int progressBarDecimals;



		public UnityAction additionalActions;
	}


	[Serializable]
	public class GoalItem
	{
		public Image icon;
		public TMP_Text goalTitle;
		public ProgressBar goalProgressbar;
		public Image progressBarBackground;
		public TMP_Text percentText;
		public TMP_Text goalMaxText;
	}

	private enum Views
	{
		StepsDay,
		StepsWeek,
		DistanceDay,
		DistanceWeek,
	}


	public enum DataTypes
	{
		Steps = 0,
		Distance = 1
	}

	public enum DataPeriods
	{
		Day = 0,
		Week = 1
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
	public GoalItem goalObject;
	public Sprite stepsIcon;
	public Color stepsColour;
	public Sprite distanceIcon;
	public Color distanceColour;


	[Space]
	[Header("This week vs last week")]
	public TMP_Text thisWeekValue;
	public TMP_Text lastWeekValue;



	private LoadedData loadedStepsDay = null;
	private LoadedData loadedStepsWeek = null;

	private LoadedData loadedDistanceDay = null;
	private LoadedData loadedDistanceWeek = null;
	

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


	public void OpenWindow(DataTypes dataType)
	{
		dataTypeDropdown.SetDropdownIndex((int)dataType);
		dataTypeDropdown.items[(int)dataType].OnItemSelection.Invoke();

		UpdateUI();
	}

	public async void UpdateUI()
	{
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
				
				if (loadedStepsDay == null)
				{
					loadedStepsDay = new LoadedData();

					//make request here
					await GetDataDay(DataTypes.Steps, loadedStepsDay);
				}
				else//load data here
				{
					Debug.Log("[Statistics] Loading saved data");

					LoadSavedData(loadedStepsDay);
				}                

                break;

			case Views.StepsWeek:

				if (loadedStepsWeek == null)
				{
					loadedStepsWeek = new LoadedData();

					//make request here
					await GetDataWeek(DataTypes.Steps, loadedStepsWeek);
				}
				else//load data here
				{
					Debug.Log("[Statistics] Loading saved data");

					LoadSavedData(loadedStepsWeek);
				}

                break;

			case Views.DistanceDay:

				if (loadedDistanceDay == null)
				{
					loadedDistanceDay = new LoadedData();

					//make request here
					await GetDataDay(DataTypes.Distance, loadedDistanceDay);
				}
				else//load data here
				{
					Debug.Log("[Statistics] Loading saved data");

					LoadSavedData(loadedDistanceDay);
				}

                break;

			case Views.DistanceWeek:
				
				if (loadedDistanceWeek == null)
				{
					loadedDistanceWeek = new LoadedData();

					//make request here
					await GetDataWeek(DataTypes.Distance, loadedDistanceWeek);
				}
				else//load data here
				{
					Debug.Log("[Statistics] Loading saved data");

					LoadSavedData(loadedDistanceWeek);
				}

                break;
		}

        FormatData(currentView);
    }



	#region getting/loading data

#if UNITY_ANDROID || UNITY_EDITOR


	private async Task GetDataDay(DataTypes dataType, LoadedData saveInto)
	{
		DateTime startRequest = DateTime.Today;
		DateTime endRequest = DateTime.Now;
		GoogleFit.ApiData APIData = GoogleFit.GenerateAPIbody(startRequest, endRequest, 3600000);


		List<double> chartData = await statisticsController.LoadGraph(dataType, APIData);

        dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the day" : "Distance over the day");
        SetDayXAxis();

        dataOverPeriodChart.SetSerieData(chartData, 0);
		dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
		dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);


		GetAndSaveGoals(dataType, DataPeriods.Day, saveInto);


		Tuple<double, double> todayVsLastWeekToday = await statisticsController.LoadTodayVsLastWeekToday(dataType);

		thisWeekValue.text = todayVsLastWeekToday.Item1.ToString();
		lastWeekValue.text = todayVsLastWeekToday.Item2.ToString();


		saveInto.chartData = chartData;
		saveInto.additionalActions += () =>
		{
            dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the day" : "Distance over the day");
            SetDayXAxis();

            dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
			dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);
		};
		saveInto.valueThisWeek = todayVsLastWeekToday.Item1;
		saveInto.valueLastWeek = todayVsLastWeekToday.Item2;
	}

	private async Task GetDataWeek(DataTypes dataType, LoadedData saveInto)
	{
		DateTime startRequest = UsefulFunctions.StartOfWeek();
		DateTime endRequest = DateTime.Now;
		GoogleFit.ApiData APIData = GoogleFit.GenerateAPIbody(startRequest, endRequest);


		List<double> chartData = await statisticsController.LoadGraph(dataType, APIData);

        dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the week" : "Distance over the week");
		SetWeekXAxis();

        dataOverPeriodChart.SetSerieData(chartData, 0);
		dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
		dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);


		GetAndSaveGoals(dataType, DataPeriods.Week, saveInto);


		Tuple<double, double> thisWeekVsLastWeek = await statisticsController.LoadThisWeekVsLastWeek(dataType);

		thisWeekValue.text = thisWeekVsLastWeek.Item1.ToString();
		lastWeekValue.text = thisWeekVsLastWeek.Item2.ToString();


		saveInto.chartData = chartData;
		saveInto.additionalActions += () =>
		{
            dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the week" : "Distance over the week");
            SetWeekXAxis();

            dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
			dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);
		};
		saveInto.valueThisWeek = thisWeekVsLastWeek.Item1;
		saveInto.valueLastWeek = thisWeekVsLastWeek.Item2;
	}


	private async void GetAndSaveGoals(DataTypes dataType, DataPeriods dataPeriod, LoadedData saveInto)
	{
		DateTime start;
		DateTime end;
		GoogleFit.ApiData APIData;


        if (dataPeriod == DataPeriods.Day)
		{
			start = DateTime.Today;
			end = DateTime.Now;

            APIData = GoogleFit.GenerateAPIbody(start, end);
        }
		else if(dataPeriod == DataPeriods.Week)
		{
			start = UsefulFunctions.StartOfWeek();
			end = DateTime.Now;

            APIData = GoogleFit.GenerateAPIbody(start, end, 604800000);
        }
		else
		{
			throw new NotImplementedException();
		}

		double value = await statisticsController.LoadGoals(dataType, APIData);

		saveInto.value = value;

		if(dataPeriod == DataPeriods.Day)
		{
			if (dataType == DataTypes.Steps)
			{
				goalObject.icon.sprite = stepsIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = stepsColour;

				goalObject.percentText.text = Math.Round((value / UserGoals.GetDailyStepGoal()) * 100, 1).ToString() + "%";

				goalObject.goalTitle.text = "Steps made today";
				goalObject.goalProgressbar.valueLimit = UserGoals.GetDailyStepGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetDailyStepGoal();
				goalObject.goalProgressbar.decimals = 0;
				goalObject.goalProgressbar.loadingBar.color = stepsColour;
				goalObject.progressBarBackground.color = new Color(stepsColour.r, g: stepsColour.g, stepsColour.b, 0.07843138f);

				goalObject.goalMaxText.text = UserGoals.GetDailyStepGoal().ToString();


				saveInto.additionalActions += () =>
				{
					goalObject.goalProgressbar.loadingBar.color = stepsColour;
					goalObject.progressBarBackground.color = new Color(stepsColour.r,
						stepsColour.g,
						stepsColour.b,
						0.07843138f);
				};

                saveInto.progressBarMaxValue = UserGoals.GetDailyStepGoal();
				saveInto.progressBarDecimals = 0;
			}
			else
			{
				goalObject.icon.sprite = distanceIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = distanceColour;
				goalObject.goalTitle.text = "Distance covered today";

				goalObject.goalProgressbar.valueLimit = UserGoals.GetDailyDistanceGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetDailyDistanceGoal();
				goalObject.goalProgressbar.decimals = 2;
                goalObject.goalProgressbar.loadingBar.color = distanceColour;
                goalObject.progressBarBackground.color = new Color(distanceColour.r, g: distanceColour.g, distanceColour.b, 0.07843138f);

                goalObject.goalMaxText.text = UserGoals.GetDailyDistanceGoal().ToString();

				goalObject.percentText.text = Math.Round((value / UserGoals.GetDailyDistanceGoal()) * 100, 1).ToString() + "%";

                saveInto.additionalActions += () =>
                {
                    goalObject.goalProgressbar.loadingBar.color = distanceColour;
                    goalObject.progressBarBackground.color = new Color(distanceColour.r,
                        distanceColour.g,
                        distanceColour.b,
                        0.07843138f);
                };
                saveInto.progressBarMaxValue = UserGoals.GetDailyDistanceGoal();
				saveInto.progressBarDecimals = 2;
			}
		}
		else if(dataPeriod == DataPeriods.Week)
		{
			if (dataType == DataTypes.Steps)
			{
				goalObject.icon.sprite = stepsIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = stepsColour;

				goalObject.percentText.text = Math.Round((value / UserGoals.GetWeeklyStepGoal()) * 100, 1).ToString() + "%";

				goalObject.goalTitle.text = "Steps made this week";
				goalObject.goalProgressbar.valueLimit = UserGoals.GetWeeklyStepGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetWeeklyStepGoal();
				goalObject.goalProgressbar.decimals = 0;
                goalObject.goalProgressbar.loadingBar.color = stepsColour;
                goalObject.progressBarBackground.color = new Color(stepsColour.r, g: stepsColour.g, stepsColour.b, 0.07843138f);

                goalObject.goalMaxText.text = UserGoals.GetWeeklyStepGoal().ToString();


				saveInto.additionalActions += () =>
				{
					goalObject.goalProgressbar.loadingBar.color = stepsColour;
					goalObject.progressBarBackground.color = new Color(stepsColour.r,
						stepsColour.g,
						stepsColour.b,
						0.07843138f);
				};
				saveInto.progressBarMaxValue = UserGoals.GetWeeklyStepGoal();
				saveInto.progressBarDecimals = 0;
			}
			else
			{
				goalObject.icon.sprite = distanceIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = distanceColour;
				goalObject.goalTitle.text = "Distance covered this week";

				goalObject.goalProgressbar.valueLimit = UserGoals.GetWeeklyDistanceGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetWeeklyDistanceGoal();
				goalObject.goalProgressbar.decimals = 2;
                goalObject.goalProgressbar.loadingBar.color = distanceColour;
                goalObject.progressBarBackground.color = new Color(distanceColour.r, g: distanceColour.g, distanceColour.b, 0.07843138f);

                goalObject.goalMaxText.text = UserGoals.GetWeeklyDistanceGoal().ToString();

				goalObject.percentText.text = Math.Round((value / UserGoals.GetWeeklyDistanceGoal()) * 100, 1).ToString() + "%";

                saveInto.additionalActions += () =>
                {
                    goalObject.goalProgressbar.loadingBar.color = distanceColour;
                    goalObject.progressBarBackground.color = new Color(distanceColour.r,
                        distanceColour.g,
                        distanceColour.b,
                        0.07843138f);
                };
                saveInto.progressBarMaxValue = UserGoals.GetWeeklyDistanceGoal();
				saveInto.progressBarDecimals = 2;
			}
		}

		goalObject.goalProgressbar.ChangeValue((float) value);


		saveInto.icon = goalObject.icon.sprite;
		saveInto.parentColour = goalObject.icon.transform.parent.GetComponent<Image>().color;
		saveInto.percentText = goalObject.percentText.text;
		saveInto.goalTitle = goalObject.goalTitle.text;
		saveInto.dailyGoalMaxText = goalObject.goalMaxText.text;
	}


#elif UNITY_IOS


	private async Task GetDataDay(DataTypes dataType, LoadedData saveInto)
	{
		List<double> chartData = await statisticsController.LoadGraph(dataType, true);

		dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the day" : "Distance over the day");
        SetDayXAxis();

		dataOverPeriodChart.SetSerieData(chartData, 0);
		dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
		dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);


		GetAndSaveGoals(dataType, DataPeriods.Day, saveInto);


		Tuple<double, double> todayVsLastWeekToday = await statisticsController.LoadTodayVsLastWeekToday(dataType);

		thisWeekValue.text = todayVsLastWeekToday.Item1.ToString();
		lastWeekValue.text = todayVsLastWeekToday.Item2.ToString();


		saveInto.chartData = chartData;
		saveInto.additionalActions += () =>
		{
			dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the day" : "Distance over the day");
			SetDayXAxis();

			dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
			dataOverPeriodChart.SetItemCornerRadius(dayRoundedCorners, 0);
		};
		saveInto.valueThisWeek = todayVsLastWeekToday.Item1;
		saveInto.valueLastWeek = todayVsLastWeekToday.Item2;
	}

	private async Task GetDataWeek(DataTypes dataType, LoadedData saveInto)
	{
		List<double> chartData = await statisticsController.LoadGraph(dataType, false);

		dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the week" : "Distance over the week");
		SetWeekXAxis();

		dataOverPeriodChart.SetSerieData(chartData, 0);
		dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
		dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);


		GetAndSaveGoals(dataType, DataPeriods.Week, saveInto);


		Tuple<double, double> thisWeekVsLastWeek = await statisticsController.LoadThisWeekVsLastWeek(dataType);

		thisWeekValue.text = thisWeekVsLastWeek.Item1.ToString();
		lastWeekValue.text = thisWeekVsLastWeek.Item2.ToString();


		saveInto.chartData = chartData;
		saveInto.additionalActions += () =>
		{
			dataOverPeriodChart.SetChartTitle(dataType == DataTypes.Steps ? "Steps over the week" : "Distance over the week");
			SetWeekXAxis();

			dataOverPeriodChart.SetYAxisNumbericFormatter(dataType == DataTypes.Steps ? "###,###,###" : "0.## km");
			dataOverPeriodChart.SetItemCornerRadius(weekRoundedCorners, 0);
		};
		saveInto.valueThisWeek = thisWeekVsLastWeek.Item1;
		saveInto.valueLastWeek = thisWeekVsLastWeek.Item2;
	}


	private async void GetAndSaveGoals(DataTypes dataType, DataPeriods dataPeriod, LoadedData saveInto)
	{
		double value = await statisticsController.LoadGoals(dataType, dataPeriod);

		saveInto.value = value;

		if(dataPeriod == DataPeriods.Day)
		{
			if (dataType == DataTypes.Steps)
			{
				goalObject.icon.sprite = stepsIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = stepsColour;
				goalObject.goalTitle.text = "Steps made today";

				goalObject.goalProgressbar.valueLimit = UserGoals.GetDailyStepGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetDailyStepGoal();
				goalObject.goalProgressbar.decimals = 0;
                goalObject.goalProgressbar.loadingBar.color = stepsColour;
                goalObject.progressBarBackground.color = new Color(stepsColour.r, g: stepsColour.g, stepsColour.b, 0.07843138f);

                goalObject.goalMaxText.text = UserGoals.GetDailyStepGoal().ToString();

				goalObject.percentText.text = Math.Round((value / UserGoals.GetDailyStepGoal()) * 100, 1).ToString() + "%";

                saveInto.additionalActions += () =>
                {
                    goalObject.goalProgressbar.loadingBar.color = stepsColour;
                    goalObject.progressBarBackground.color = new Color(stepsColour.r,
                        stepsColour.g,
                        stepsColour.b,
                        0.07843138f);
                };
                saveInto.progressBarMaxValue = UserGoals.GetDailyStepGoal();
				saveInto.progressBarDecimals = 0;
			}
			else
			{
				goalObject.icon.sprite = distanceIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = distanceColour;
				goalObject.goalTitle.text = "Distance covered today";

				goalObject.goalProgressbar.valueLimit = UserGoals.GetDailyDistanceGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetDailyDistanceGoal();
				goalObject.goalProgressbar.decimals = 2;
                goalObject.goalProgressbar.loadingBar.color = distanceColour;
                goalObject.progressBarBackground.color = new Color(distanceColour.r, g: distanceColour.g, distanceColour.b, 0.07843138f);

                goalObject.goalMaxText.text = UserGoals.GetDailyDistanceGoal().ToString();

				goalObject.percentText.text = Math.Round((value / UserGoals.GetDailyDistanceGoal()) * 100, 1).ToString() + "%";

                saveInto.additionalActions += () =>
                {
                    goalObject.goalProgressbar.loadingBar.color = distanceColour;
                    goalObject.progressBarBackground.color = new Color(distanceColour.r,
                        distanceColour.g,
                        distanceColour.b,
                        0.07843138f);
                };
                saveInto.progressBarMaxValue = UserGoals.GetDailyDistanceGoal();
				saveInto.progressBarDecimals = 2;
			}
		}
		else if(dataPeriod == DataPeriods.Week)
		{
			if (dataType == DataTypes.Steps)
			{
				goalObject.icon.sprite = stepsIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = stepsColour;
				goalObject.goalTitle.text = "Steps made this week";

				goalObject.goalProgressbar.valueLimit = UserGoals.GetWeeklyStepGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetWeeklyStepGoal();
				goalObject.goalProgressbar.decimals = 0;
                goalObject.goalProgressbar.loadingBar.color = stepsColour;
                goalObject.progressBarBackground.color = new Color(stepsColour.r, g: stepsColour.g, stepsColour.b, 0.07843138f);

                goalObject.goalMaxText.text = UserGoals.GetWeeklyStepGoal().ToString();

				goalObject.percentText.text = Math.Round((value / UserGoals.GetWeeklyStepGoal()) * 100, 1).ToString() + "%";

                saveInto.additionalActions += () =>
                {
                    goalObject.goalProgressbar.loadingBar.color = stepsColour;
                    goalObject.progressBarBackground.color = new Color(stepsColour.r,
                        stepsColour.g,
                        stepsColour.b,
                        0.07843138f);
                };
                saveInto.progressBarMaxValue = UserGoals.GetWeeklyStepGoal();
				saveInto.progressBarDecimals = 0;
			}
			else
			{
				goalObject.icon.sprite = distanceIcon;
				goalObject.icon.transform.parent.GetComponent<Image>().color = distanceColour;
				goalObject.goalTitle.text = "Distance covered this week";

				goalObject.goalProgressbar.valueLimit = UserGoals.GetWeeklyDistanceGoal();
				goalObject.goalProgressbar.maxValue = UserGoals.GetWeeklyDistanceGoal();
				goalObject.goalProgressbar.decimals = 2;
                goalObject.goalProgressbar.loadingBar.color = distanceColour;
                goalObject.progressBarBackground.color = new Color(distanceColour.r, g: distanceColour.g, distanceColour.b, 0.07843138f);

                goalObject.goalMaxText.text = UserGoals.GetWeeklyDistanceGoal().ToString();

				goalObject.percentText.text = Math.Round((value / UserGoals.GetWeeklyDistanceGoal()) * 100, 1).ToString() + "%";

                saveInto.additionalActions += () =>
                {
                    goalObject.goalProgressbar.loadingBar.color = distanceColour;
                    goalObject.progressBarBackground.color = new Color(distanceColour.r,
                        distanceColour.g,
                        distanceColour.b,
                        0.07843138f);
                };
                saveInto.progressBarMaxValue = UserGoals.GetWeeklyDistanceGoal();
				saveInto.progressBarDecimals = 2;
			}
		}

		goalObject.goalProgressbar.ChangeValue((float) value);


		saveInto.icon = goalObject.icon.sprite;
		saveInto.parentColour = goalObject.icon.transform.parent.GetComponent<Image>().color;
		saveInto.percentText = goalObject.percentText.text;
		saveInto.goalTitle = goalObject.goalTitle.text;
		saveInto.dailyGoalMaxText = goalObject.goalMaxText.text;
	}

#endif

    #endregion


    private void LoadSavedData(LoadedData savedData)
	{
		dataOverPeriodChart.SetSerieData(savedData.chartData, 0);		

		thisWeekValue.text = savedData.valueThisWeek.ToString();
		lastWeekValue.text = savedData.valueLastWeek.ToString();

		goalObject.icon.sprite = savedData.icon;
		goalObject.icon.transform.parent.GetComponent<Image>().color = savedData.parentColour;
		goalObject.goalTitle.text = savedData.goalTitle;
		goalObject.percentText.text = savedData.percentText;
		goalObject.goalMaxText.text = savedData.dailyGoalMaxText;
		goalObject.goalProgressbar.maxValue = savedData.progressBarMaxValue;
		goalObject.goalProgressbar.valueLimit = savedData.progressBarMaxValue;
		goalObject.goalProgressbar.decimals = savedData.progressBarDecimals;
		goalObject.goalProgressbar.ChangeValue((float)savedData.value);

        savedData.additionalActions.Invoke();

        goalObject.goalProgressbar.UpdateUI();
	}

	private void FormatData(Views dataType)
	{
		#region daily/weekly goal

		TMP_Text progressText = goalObject.goalProgressbar.textPercent;
		TMP_Text maxProgressText = goalObject.goalMaxText;

        if (dataType == Views.StepsDay || dataType == Views.StepsWeek)
		{
			progressText.text = int.Parse(progressText.text).ToString("#,##0");
            maxProgressText.text = int.Parse(maxProgressText.text).ToString("#,##0");
        }
		else if(dataType == Views.DistanceDay || dataType == Views.DistanceWeek)
		{
			progressText.text += " km";
            maxProgressText.text += " km";
        }
		else { }

        #endregion
    }


    #region Android statistics

#if UNITY_ANDROID || UNITY_EDITOR

    public class AndroidStatistics
	{
		public async Task<List<double>> LoadGraph(DataTypes dataType, GoogleFit.ApiData APIData)
		{
			JsonData json;

			if (dataType == DataTypes.Steps) json = await GoogleFit.GetStepsBetweenMillis(APIData);
			else json = await GoogleFit.GetDistanceBetweenMillis(APIData);


			List<double> weekValues = new List<double>();
			float totalValue = 0;

			for (int i = 0; i < json["bucket"].Count; i++)
			{
				JsonData stepData = json["bucket"][i]["dataset"][0]["point"];
				double item = 0;

				try
				{
					item = double.Parse(stepData[0]["value"][0][(dataType == DataTypes.Steps ? "intVal" : "fpVal")].ToString());

					if (dataType == DataTypes.Distance)
					{
						item = Math.Round((float)(item / 1000), 2);
					}

					totalValue += (float)item;
				}
				catch (ArgumentOutOfRangeException) { }
				catch (KeyNotFoundException) { }

				weekValues.Add(item);
			}

			return weekValues;            
		}


		public async Task<double> LoadGoals(DataTypes dataType, GoogleFit.ApiData APIData)
		{
			JsonData json;

			if(dataType == DataTypes.Steps) json = await GoogleFit.GetStepsBetweenMillis(APIData);
			else json = await GoogleFit.GetDistanceBetweenMillis(APIData);

			//Debug.Log(json.ToJson());

			double value = double.Parse(json["bucket"][0]["dataset"][0]["point"][0]["value"][0][(dataType == DataTypes.Steps ? "intVal" : "fpVal")].ToString());


			if (dataType == DataTypes.Distance)
			{
				value = Math.Round((float)(value / 1000), 2);
			}

			return value;
		}


		public async Task<Tuple<double, double>> LoadTodayVsLastWeekToday(DataTypes dataType)
		{
			DateTime startRequest = DateTime.Today;
			DateTime endRequest = DateTime.Now;
			GoogleFit.ApiData APIData = GoogleFit.GenerateAPIbody(startRequest, endRequest);


			JsonData today;


			if (dataType == DataTypes.Steps) today = await GoogleFit.GetStepsBetweenMillis(APIData);
			else today = await GoogleFit.GetDistanceBetweenMillis(APIData);



			startRequest = DateTime.Today.AddDays(-7);
			endRequest = DateTime.Today.AddDays(-6);
			APIData = GoogleFit.GenerateAPIbody(startRequest, endRequest);


			JsonData lastWeekToday;


			if (dataType == DataTypes.Steps) lastWeekToday = await GoogleFit.GetStepsBetweenMillis(APIData);
			else lastWeekToday = await GoogleFit.GetDistanceBetweenMillis(APIData);



			double todayV = double.Parse(today["bucket"][0]
				["dataset"][0]["point"][0]
				["value"][0][(dataType == DataTypes.Steps ? "intVal" : "fpVal")].ToString());

			double lastWeekV = double.Parse(lastWeekToday["bucket"][0]
				["dataset"][0]["point"][0]
				["value"][0][(dataType == DataTypes.Steps ? "intVal" : "fpVal")].ToString());


			if(dataType == DataTypes.Distance)
			{
				todayV = Math.Round((float)(todayV / 1000), 2);
				lastWeekV = Math.Round((float)(lastWeekV / 1000), 2);
			}


			return new Tuple<double, double>(todayV, lastWeekV);
		}

		public async Task<Tuple<double, double>> LoadThisWeekVsLastWeek(DataTypes dataType)
		{
			DateTime startRequest = UsefulFunctions.StartOfWeek();
			DateTime endRequest = DateTime.Now;
			GoogleFit.ApiData APIData = GoogleFit.GenerateAPIbody(startRequest, endRequest, 604800000);


			JsonData thisWeek;


			if (dataType == DataTypes.Steps) thisWeek = await GoogleFit.GetStepsBetweenMillis(APIData);
			else thisWeek = await GoogleFit.GetDistanceBetweenMillis(APIData);


			startRequest = UsefulFunctions.StartOfWeek().AddDays(-7);
			endRequest = startRequest.AddDays(7);
			APIData = GoogleFit.GenerateAPIbody(startRequest, endRequest, 604800000);


			JsonData lastWeek;


			if (dataType == DataTypes.Steps) lastWeek = await GoogleFit.GetStepsBetweenMillis(APIData);
			else lastWeek = await GoogleFit.GetDistanceBetweenMillis(APIData);


			double todayV = double.Parse(thisWeek["bucket"][0]
			["dataset"][0]["point"][0]
				["value"][0][(dataType == DataTypes.Steps ? "intVal" : "fpVal")].ToString());

			double lastWeekV = double.Parse(lastWeek["bucket"][0]
			["dataset"][0]["point"][0]
				["value"][0][(dataType == DataTypes.Steps ? "intVal" : "fpVal")].ToString());


			if (dataType == DataTypes.Distance)
			{
				todayV = Math.Round((float)(todayV / 1000), 2);
				lastWeekV = Math.Round((float)(lastWeekV / 1000), 2);
			}


			return new Tuple<double, double>(todayV, lastWeekV);
		}
	}

#endif

	#endregion


	#region iOS statistics

#if UNITY_IOS || UNITY_EDITOR

	public class iOSStatistics
	{
		public async Task<List<double>> LoadGraph(DataTypes dataType, bool isDayData)
		{
			if (isDayData)
			{
				DateTime start = DateTime.Today;
				DateTime end = DateTime.Now;

				List<HealthKit.QuantityData> data;

				if (dataType == DataTypes.Steps)
				{
					data = await HealthKit.GetStepsList(start, end);
				}
				else
				{
					data = await HealthKit.GetDistanceList(start, end);
				}

				List<HealthKit.OrderedQuantityData> orderedData = HealthKit.OrderQuantityListHour(data);

				List<double> dayValues = new List<double>(new double[24]);


				for (int i = 0; i < orderedData.Count; i++)
				{
					dayValues[orderedData[i].timeOfData.Hour] = orderedData[i].value;
				}


				return dayValues;
			}


			else//dropdown is selected to week, so show the data over the week
			{
				DateTime start = UsefulFunctions.StartOfWeek();
				DateTime end = DateTime.Now;

				List<HealthKit.QuantityData> data;

				if (dataType == DataTypes.Steps)
				{
					data = await HealthKit.GetStepsList(start, end);
				}
				else
				{
					data = await HealthKit.GetDistanceList(start, end);
				}

				List<HealthKit.OrderedQuantityData> orderedData = HealthKit.OrderQuantityListDay(data);

				List<double> weekValues = new List<double>(new double[7]);


				for (int i = 0; i < orderedData.Count; i++)
				{
					weekValues[(int)orderedData[i].timeOfData.DayOfWeek - 1] = orderedData[i].value;
				}


				return weekValues;
			}
		}


		public async Task<double> LoadGoals(DataTypes dataType, DataPeriods dataPeriod)
		{
			DateTime start;
			DateTime end;

			if (dataPeriod == DataPeriods.Day)
			{
				start = DateTime.Today;
				end = DateTime.Now;
			}
			else if (dataPeriod == DataPeriods.Week)
			{
				start = UsefulFunctions.StartOfWeek();
				end = DateTime.Now;
			}
			else
			{
				throw new NotImplementedException();
			}


			if (dataType == DataTypes.Steps) return await HealthKit.GetSteps(start, end);
			else return await HealthKit.GetDistance(start, end);
		}


		public async Task<Tuple<double, double>> LoadTodayVsLastWeekToday(DataTypes dataType)
		{
			DateTime start = DateTime.Today;
			DateTime end = DateTime.Now;

			double today;

			if (dataType == DataTypes.Steps)
			{
				today = await HealthKit.GetSteps(start, end);
			}
			else
			{
				today = await HealthKit.GetDistance(start, end);
			}


			start = DateTime.Today.AddDays(-7);
			end = start.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(1000);

			double lastWeekToday;


			if (dataType == DataTypes.Steps) lastWeekToday = await HealthKit.GetSteps(start, end);
			else lastWeekToday = await HealthKit.GetDistance(start, end);


			return new Tuple<double, double>(today, lastWeekToday);
		}

		public async Task<Tuple<double, double>> LoadThisWeekVsLastWeek(DataTypes dataType)
		{
			DateTime start = UsefulFunctions.StartOfWeek();
			DateTime end = DateTime.Now;

			double thisWeek;

			if (dataType == DataTypes.Steps) thisWeek = await HealthKit.GetSteps(start, end);
			else thisWeek = await HealthKit.GetDistance(start, end);
		   

			start = UsefulFunctions.StartOfWeek().AddDays(-7);
			end = start.AddDays(7);


			double lastWeek;


			if (dataType == DataTypes.Steps) lastWeek = await HealthKit.GetSteps(start, end);
			else lastWeek = await HealthKit.GetDistance(start, end);


			return new Tuple<double, double>(thisWeek, lastWeek);
		}        
	}

#endif

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