using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

public class EasyChartSettings : MonoBehaviour
{
    private BaseChart chart;


    private void Awake()
    {
        chart = GetComponent<BaseChart>();
    }


    public void SetXAxisPoints(List<string> points)
    {
        chart.GetChartComponent<SingleAxis>().data = points;
    }

    public void SetSerieData(SerieData data)
    {
        chart.series[0].AddSerieData(data);
    }

    public void SetSerieData(List<double> data, int serieIndex)
    {
        for (int i = 0; i < data.Count; i++)
        {
            chart.series[serieIndex].UpdateData(i, 1, data[i]);
        }
    }

    public void SetSerieData(List<double> data, List<bool> ignorePoints)
    {
        if(data.Count != ignorePoints.Count)
            Debug.LogWarning("[Charts] provided lists are not ther same length");


        for (int i = 0; i < data.Count; i++)
        {
            chart.series[0].UpdateData(i, 1, data[i]);
        }


        for (int i = 0; i < ignorePoints.Count; i++)
        {
            SerieData s = chart.series[0].data[i];
        
            s.ignore = ignorePoints[i];
        }
    }

    public void AddSerieData(double value)
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

    public void AddSerieData(double value, string axisPoint)
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

            SetXAxisPoints(points);
        }

        chart.series[0].AddSerieData(d);
    }

    public void AnimateGraph()
    {
        chart.AnimationReset();
        chart.AnimationResume();
    }
}