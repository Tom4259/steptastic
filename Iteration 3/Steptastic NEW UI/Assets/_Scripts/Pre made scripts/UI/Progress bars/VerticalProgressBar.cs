using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class VerticalProgressBar : MonoBehaviour
{
    [Range(0, 100)]
    public float percent;

    public SlicedFilledImage fill;
    public Image roundedCap;

    private void Update()
    {
        float endCapPercent = UsefulFunctions.Map(percent, 0, 100, 4.387f, 95.613f);
        
        fill.fillAmount = endCapPercent / 100;
        roundedCap.rectTransform.anchoredPosition = new Vector2(0, 400 * (endCapPercent / 100));
    }
}