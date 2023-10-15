using Michsky.MUIP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class roundedProgressbar : MonoBehaviour
{
    public RectTransform endCap;
    private ProgressBar bar;

    private void Start()
    {
        bar = GetComponent<ProgressBar>();
    }

    /// <summary>
    /// gets the fill amount and places that percentage around a circle, rotates a rect transform
    /// containing an image to be at that angle so the end of the bar isn't flat, but is rounded
    /// </summary>
    private void Update()
    {
        float fillAmount = bar.currentPercent;
        float angle = fillAmount / 360;

        endCap.rotation = Quaternion.Euler(0, 0, -(angle * 1294.964f));
    }
}
