using Michsky.MUIP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class roundedProgressbar : MonoBehaviour
{
    public RectTransform endCap;
    private ProgressBar bar;

    public bool circular = true;

    private RectTransform thisRect;

    private void Start()
    {
        bar = GetComponent<ProgressBar>();
        thisRect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// gets the fill amount and places that percentage around a circle, rotates a rect transform
    /// containing an image to be at that angle so the end of the bar isn't flat, but is rounded
    /// </summary>
    private void Update()
    {
        if (circular)
        {
            float fillAmount = bar.currentPercent;
            float angle = fillAmount / 360;

            endCap.rotation = Quaternion.Euler(0, 0, -(angle * 1294.964f));
        }
        else
        {
            float rawPercent = UsefulFunctions.Map(bar.currentPercent, 0, 100, 2.5f, 97.5f);

            endCap.anchoredPosition = new Vector2((thisRect.rect.width / 100) * rawPercent, 0);
        }
    }
}
