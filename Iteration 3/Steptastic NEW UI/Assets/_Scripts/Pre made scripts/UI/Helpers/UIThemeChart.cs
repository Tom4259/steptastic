using Michsky.MUIP;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts;
using XCharts.Runtime;


public class UIThemeChart : MonoBehaviour
{
	private UIThemeManager manager;

	private bool isDark;

	private BaseChart chart;

	[Space]
	public Color darkTheme;
	public Color lightTheme;

	[Space]
	public int backgroundPixelsPerUnitMultiplier = 12;

	[Space]
	public TextStyle darkText;
	public TextStyle lightText;

	[Space]
	public LineStyle darkLine;
	public LineStyle lightLine;

	[Space]
	public AxisTick darkTick;
	public AxisTick lightTick;

	[Space]
	[HideInInspector] public string XAxisNumericFormatter;
	[HideInInspector] public string YAxisNumericFormatter;
	[HideInInspector] public string singleAxisNumericFormatter;


	private void Start()
	{
		manager = FindObjectOfType<UIThemeManager>();

		manager.onDarkMode += SetDarkMode;
		manager.onLightMode += SetLightMode;

		chart = GetComponent<BaseChart>();
	}

	private void SetDarkMode()
	{
		isDark = true;

		UpdateUI();
	}

	private void SetLightMode()
	{
		isDark = false;

		UpdateUI();
	}

	private void UpdateUI()
	{
		ThemeStyle newTheme = new ThemeStyle
		{
			customBackgroundColor = isDark ? darkTheme : lightTheme,
			enableCustomTheme = true,
			customColorPalette = new List<Color32>
			{
				Color.white
			}
		};

		chart.theme = newTheme;


		AxisLine newAxisLine = new AxisLine
		{
			lineStyle = isDark ? darkLine : lightLine
		};

		AxisLabel newXAxisLabel = new AxisLabel
		{
			textStyle = isDark ? darkText : lightText,
			distance = 8,
			numericFormatter = XAxisNumericFormatter
		};

		AxisLabel newYAxisLabel = new AxisLabel
		{
			textStyle = isDark ? darkText : lightText,
			distance = 8,
			numericFormatter = YAxisNumericFormatter
		};

		AxisLabel newSingleAxisLabel = new AxisLabel()
		{
			textStyle = isDark ? darkText : lightText,
			distance = 8,
			numericFormatter = singleAxisNumericFormatter
		};



		chart.GetChartComponent<Title>().labelStyle.textStyle = isDark ? darkText : lightText;
		chart.GetChartComponent<Title>().subLabelStyle.textStyle = isDark ? darkText : lightText;


		try
		{
			chart.GetChartComponent<SingleAxis>().axisLabel = newSingleAxisLabel;
			chart.GetChartComponent<SingleAxis>().axisLine = newAxisLine;
			chart.GetChartComponent<SingleAxis>().axisTick = isDark ? darkTick : lightTick;
		}
		catch(NullReferenceException) { }


		chart.GetChartComponent<XAxis>().axisLabel = newXAxisLabel;
		chart.GetChartComponent<XAxis>().axisLine = newAxisLine;
		chart.GetChartComponent<XAxis>().axisTick = isDark ? darkTick : lightTick;


		chart.GetChartComponent<YAxis>().axisLabel = newYAxisLabel;
		chart.GetChartComponent<YAxis>().axisLine = newAxisLine;
		chart.GetChartComponent<YAxis>().axisTick = isDark ? darkTick : lightTick;


		chart.GetChartComponent<Background>().pixelsPerUnitMultiplier = backgroundPixelsPerUnitMultiplier;


		try
		{
			chart.GetChartComponent<SingleAxis>().axisLabel.show = true;
			chart.GetChartComponent<SingleAxis>().axisLine.show = true;
			chart.GetChartComponent<SingleAxis>().axisTick.show = true;
		}
		catch(NullReferenceException) { }

		chart.GetChartComponent<XAxis>().axisLabel.show = true;
		chart.GetChartComponent<XAxis>().axisLine.show = true;
		chart.GetChartComponent<XAxis>().axisTick.show = true;

		chart.GetChartComponent<YAxis>().axisLabel.show = true;
		chart.GetChartComponent<YAxis>().axisLine.show = true;
		chart.GetChartComponent<YAxis>().axisTick.show = true;

		chart.RefreshAllComponent();
		chart.RefreshChart();
	}
}