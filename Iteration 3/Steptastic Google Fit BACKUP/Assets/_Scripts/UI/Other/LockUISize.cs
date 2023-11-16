using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockUISize : MonoBehaviour
{
    public bool lockWidth = false;
    public bool lockHeight = false;

    public Vector2 lockSize;

    private RectTransform rt;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Update()
    {
        rt.sizeDelta = new Vector2(lockWidth ? lockSize.x : rt.sizeDelta.x,
            lockHeight ? lockSize.y : rt.sizeDelta.y);
    }
}
