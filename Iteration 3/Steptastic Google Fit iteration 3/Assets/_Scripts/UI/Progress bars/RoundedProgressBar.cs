using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class RoundedProgressBar : MonoBehaviour
{
    [Range(0f, 100f)]
    public float percent;

    public Image fillImage;
    public Image roundedEdge;
    private RectTransform edgeRT;

    public Color barColour;

    private RectTransform thisRT;

    private void Start()
    {
        thisRT = GetComponent<RectTransform>();
        edgeRT = roundedEdge.GetComponent<RectTransform>();
    }

    private void Update()
    {
        float rawPercent = UsefulFunctions.Map(percent, 0, 100, 5.75f, 100);

        fillImage.fillAmount = rawPercent / 100;

        edgeRT.anchoredPosition = new Vector2(0, ((thisRT.rect.height / 100) *  rawPercent));

        fillImage.color = barColour;
        roundedEdge.color = barColour;
    }
}
