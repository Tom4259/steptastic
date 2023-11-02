using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[ExecuteInEditMode]
public class setRectTransformPosition : MonoBehaviour
{
    public bool onStart = true;

    public Vector2 position = Vector2.zero;

    private RectTransform rt;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private async void Start()
    {
        await Task.Delay(1000);

        rt.anchoredPosition = position;
    }
}
