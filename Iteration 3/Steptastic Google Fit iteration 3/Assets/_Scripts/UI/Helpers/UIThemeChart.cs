using Michsky.MUIP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using XCharts;
using XCharts.Runtime;


public class UIThemeChart : MonoBehaviour
{
    private UIThemeManager manager;

    private bool isDark;

    private BaseChart chart;

    public Font chartFont;

    [Space]
    public Color32 darkBackground;
    public Color32 lightBackground;

    [Space]
    public Color32 darkText;
    public Color32 lightText;

    [Space]
    public Color32 darkLine;
    public Color32 lightLine;


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
            customBackgroundColor = isDark ? darkBackground : lightBackground,
            enableCustomTheme = true,
            customColorPalette = new List<Color32>
            {
                Color.white
            }
        };

        chart.theme = newTheme;



        TextStyle newTextStyle = new TextStyle
        {
            color = isDark ? darkText : lightText,
            font = chartFont
        };

        TitleStyle newTitle = new TitleStyle
        {
            textStyle = newTextStyle
        };


        LineStyle newLineStyle = new LineStyle
        {
            color = isDark ? darkLine : lightLine,
        };


        AxisLine newAxisLine = new AxisLine
        {
            lineStyle = newLineStyle
        };

        AxisLabel newAxisLabel = new AxisLabel
        {
            textStyle = newTextStyle,
            distance = 8
        };

        AxisTick newAxisTick = new AxisTick
        {
            lineStyle = newLineStyle,
            alignWithLabel = true // single axis's tick is in the middle of the chart
        };


        chart.GetChartComponent<Title>().labelStyle.textStyle = newTextStyle;
        chart.GetChartComponent<Title>().subLabelStyle.textStyle = newTextStyle;


        chart.GetChartComponent<SingleAxis>().axisLabel = newAxisLabel;
        chart.GetChartComponent<SingleAxis>().axisLine = newAxisLine;
        chart.GetChartComponent<SingleAxis>().axisTick = newAxisTick;


        chart.GetChartComponent<XAxis>().axisLabel = newAxisLabel;
        chart.GetChartComponent<XAxis>().axisLine = newAxisLine;
        chart.GetChartComponent<XAxis>().axisTick = newAxisTick;
        

        chart.GetChartComponent<YAxis>().axisLabel = newAxisLabel;
        chart.GetChartComponent<YAxis>().axisLine = newAxisLine;
        chart.GetChartComponent<YAxis>().axisTick = newAxisTick;


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