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

        addSerieData(122, "testing!");
    }


    public void setXAxisPoints(List<string> points)
    {
        chart.GetChartComponent<XAxis>().data = points;
    }

    public void setSerieData(SerieData data)
    {
        chart.series[0].AddSerieData(data);
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