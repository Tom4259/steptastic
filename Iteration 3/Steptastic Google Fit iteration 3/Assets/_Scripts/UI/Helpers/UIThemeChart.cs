using Michsky.MUIP;
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

        AxisLabel newAxisLabel = new AxisLabel
        {
            textStyle = isDark ? darkText : lightText,
            distance = 8,

        };


        chart.GetChartComponent<Title>().labelStyle.textStyle = isDark ? darkText : lightText;
        chart.GetChartComponent<Title>().subLabelStyle.textStyle = isDark ? darkText : lightText;


        chart.GetChartComponent<SingleAxis>().axisLabel = newAxisLabel;
        chart.GetChartComponent<SingleAxis>().axisLine = newAxisLine;
        chart.GetChartComponent<SingleAxis>().axisTick = isDark ? darkTick : lightTick;


        chart.GetChartComponent<XAxis>().axisLabel = newAxisLabel;
        chart.GetChartComponent<XAxis>().axisLine = newAxisLine;
        chart.GetChartComponent<XAxis>().axisTick = isDark ? darkTick : lightTick;


        chart.GetChartComponent<YAxis>().axisLabel = newAxisLabel;
        chart.GetChartComponent<YAxis>().axisLine = newAxisLine;
        chart.GetChartComponent<YAxis>().axisTick = isDark ? darkTick : lightTick;


        chart.GetChartComponent<Background>().pixelsPerUnitMultiplier = backgroundPixelsPerUnitMultiplier;


        chart.GetChartComponent<SingleAxis>().axisLabel.show = true;
        chart.GetChartComponent<SingleAxis>().axisLine.show = true;
        chart.GetChartComponent<SingleAxis>().axisTick.show = true;

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