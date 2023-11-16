using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Image))]
public class ImageFillController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float percent;

    private Image img;

    private void Start()
    {
        img = GetComponent<Image>();
    }

    private void Update()
    {
        img.fillAmount = percent;
    }
}
