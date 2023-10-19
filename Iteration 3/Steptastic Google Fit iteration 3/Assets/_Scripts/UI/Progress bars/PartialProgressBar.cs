using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[ExecuteInEditMode]
public class PartialProgressBar : MonoBehaviour
{
    [Range(0f, 100f)]
    public float percent = 50;
    public int decimalPlaces = 2;
    public string suffix = " %";

    public Image fill;
    public TMP_Text percentText;

    [Space]
    public Color UIColour;


    private float startRotation = -90f;
    private float startFill = 0.35f;

    private void Update()
    {
        float rawPercent = UsefulFunctions.Map(percent, 0, 100, 0, 55.7f);
        float angle = ((rawPercent / 74.2f) * 100) / 360;

        fill.rectTransform.rotation = Quaternion.Euler(0, 0, -(angle * 1294.964f) + startRotation);

        fill.fillAmount = (percent / 125) + startFill;


        //setting percent text

        percentText.text = Math.Round(percent, decimalPlaces).ToString("f") + suffix;


        fill.color = UIColour;
        percentText.color = UIColour;
    }
}
