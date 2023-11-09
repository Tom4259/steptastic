using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;
using System.Threading.Tasks;


[RequireComponent(typeof(BaseChart))]
public class EasyChartSettings : MonoBehaviour
{
    private BaseChart chart;


    private void Awake()
    {
        chart = GetComponent<BaseChart>();
    }


	#region title


	public void SetChartTitle(string title)
    {
        chart.GetChartComponent<Title>().text = title;
    }

    public void SetChartSubText(string subtext)
    {
        chart.GetChartComponent<Title>().subText = subtext;
    }


	#endregion

	#region axis points


	public void SetSingleAxisPoints(List<string> points)
    {
        chart.GetChartComponent<SingleAxis>().ClearData();
        chart.GetChartComponent<XAxis>().ClearData();
        chart.GetChartComponent<XAxis>().data = new List<string>(new string[points.Count]);
        chart.GetChartComponent<SingleAxis>().data = points;
    }

    public void SetXAxisPoints(List<string> points)
    {
        chart.GetChartComponent<XAxis>().ClearData();
        chart.GetChartComponent<XAxis>().data = points;
    }


	#endregion

	#region split number


	public void SetSingleAxisSplitNumber(int splitNumber)
    {
        chart.GetChartComponent<SingleAxis>().splitNumber = splitNumber;
    }

    public void SetXAxisSplitNumber(int splitNumber)
    {
        chart.GetChartComponent<XAxis>().splitNumber = splitNumber;
    }


	#endregion

	#region setting, adding and clearing chart


	public void SetSerieData(List<double> data, int serieIndex, bool animate = false)
    {
        ClearChart(serieIndex);

        for (int i = 0; i < data.Count; i++)
        {
            chart.series[serieIndex].UpdateData(i, 1, data[i]);
        }

        if (!animate)
        {
			chart.AnimationEnable(false);
            chart.RefreshAllComponent();
		}
    }

    public void SetSerieData(List<double> data, List<bool> ignorePoints, int serieIndex)
    {
        if(data.Count != ignorePoints.Count)
            Debug.LogWarning("[Charts] provided lists are not the same length");


        for (int i = 0; i < data.Count; i++)
        {
            chart.series[serieIndex].UpdateData(i, 1, data[i]);
        }


        for (int i = 0; i < ignorePoints.Count; i++)
        {
            SerieData s = chart.series[serieIndex].data[i];
        
            s.ignore = ignorePoints[i];
        }
    }


    public void AddSerieData(SerieData data, int serieIndex)
    {
        chart.series[serieIndex].AddSerieData(data);
    }

    public void AddSerieData(double value, int serieIndex)
    {
        SerieData d = new SerieData
        {
            data = new List<double>
            {
                0,
                value
            }            
        };

        chart.series[serieIndex].AddSerieData(d);
    }

    public void AddSerieData(double value, string axisPoint, int serieIndex)
    {
        SerieData d = new SerieData
        {
            data = new List<double>
            {
                0,
                value
            }
        };

        List<string> points = chart.GetChartComponent<XAxis>().data;

        if (points.Count < chart.series[serieIndex].data.Count)
        {
            points.Add(axisPoint);

            SetSingleAxisPoints(points);
        }

        chart.series[serieIndex].AddSerieData(d);
    }




    public void ClearChart(int serieIndex)
    {
		for (int i = 0; i < chart.series[serieIndex].data.Count; i++)
		{
			chart.series[serieIndex].UpdateData(i, 1, 0);
		}
	}

    public void ClearData(int seriesIndex)
    {
        chart.series[seriesIndex].data.Clear();
    }


	#endregion

	#region corner radius


	public void SetItemCornerRadius(List<float> cornerRadius, int seriesIndex)
    {
        chart.series[seriesIndex].itemStyle.cornerRadius = cornerRadius.ToArray();
    }


	#endregion

	#region numeric formatter


	public void SetXAxisNumbericFormatter(string formatter)
    {
        try
        {
            GetComponent<UIThemeChart>().XAxisNumericFormatter = formatter;
        }
        catch (NullReferenceException)
        {
			chart.GetChartComponent<XAxis>().axisLabel.numericFormatter = formatter;
		}

        chart.RefreshAllComponent();
    }

	public void SetYAxisNumbericFormatter(string formatter)
	{
		try
		{
			GetComponent<UIThemeChart>().YAxisNumericFormatter = formatter;
		}
		catch (NullReferenceException)
		{
			chart.GetChartComponent<YAxis>().axisLabel.numericFormatter = formatter;
		}

		chart.RefreshAllComponent();
	}


	public void SetSingleAxisNumbericFormatter(string formatter)
	{
		try
		{
			GetComponent<UIThemeChart>().singleAxisNumericFormatter = formatter;
		}
		catch (NullReferenceException)
		{
			chart.GetChartComponent<SingleAxis>().axisLabel.numericFormatter = formatter;
		}

		chart.RefreshAllComponent();
	}


	#endregion




	public void AnimateGraph()
    {
        chart.AnimationReset();
        chart.AnimationResume();
        chart.AnimationFadeIn();
    }


    public void RefreshGraph(bool animation)
    {
        chart.RefreshAllComponent();

        if (animation) AnimateGraph();
    }
}