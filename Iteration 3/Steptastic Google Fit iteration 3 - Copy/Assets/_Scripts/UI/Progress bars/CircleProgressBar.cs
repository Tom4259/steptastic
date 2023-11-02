using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CircleProgressBar : MonoBehaviour
{
    public Color barColour;

    [Space]
    [Range(0f, 100f)]
    public float percent;
    public int decimalPlaces = 2;

    public TMP_Text percentText;
    public string suffix = "%";

    [Space]
    public Image fillImage;
    public RectTransform endCapHolder;

    private void Update()
    {
        fillImage.fillAmount = percent / 100;
        fillImage.color = barColour;

        float angle = percent / 360;
        endCapHolder.rotation = Quaternion.Euler(0, 0, -(angle * 1294.964f));

        percentText.text = Math.Round(percent, decimalPlaces) + suffix;
    }
}