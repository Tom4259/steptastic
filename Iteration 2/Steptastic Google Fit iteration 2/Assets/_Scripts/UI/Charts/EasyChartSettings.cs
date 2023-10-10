using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class EasyChartSettings : MonoBehaviour
{
    private BaseChart chart;


    void Start()
    {
        chart = GetComponent<BaseChart>();
    }


    public void setXAxisPoints(List<string> points)
    {
        chart.GetChartComponent<XAxis>().data = points;
    }

    public void setSerieData(SerieData data)
    {
        chart.series[0].AddSerieData(data);
    }

    public void setSerieData(List<double> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            chart.series[0].UpdateData(i, 1, data[i]);
        }
    }

    public void addSerieData(double value)
    {
        SerieData d = new SerieData
        {
            data = new List<double>
            {
                0,
                value
            }
        };

        chart.series[0].AddSerieData(d);
    }

    public void addSerieData(double value, string axisPoint)
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

        if (points.Count < chart.series[0].data.Count)
        {
            points.Add(axisPoint);

            setXAxisPoints(points);
        }

        chart.series[0].AddSerieData(d);
    }
}