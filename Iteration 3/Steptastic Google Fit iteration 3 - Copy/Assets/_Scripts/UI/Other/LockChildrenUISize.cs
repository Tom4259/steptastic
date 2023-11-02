using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockChildrenUISize : MonoBehaviour
{
    public bool lockWidth = false;
    public bool lockHeight = false;

    public Vector2 lockSize;

    private List<RectTransform> rt = new List<RectTransform>();

    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            rt.Add(transform.GetChild(i).GetComponent<RectTransform>());
        }
    }

    private void Update()
    {
        for (int i = 0; i < rt.Count; i++)
        {
            rt[i].sizeDelta = new Vector2(lockWidth ? lockSize.x : rt[i].sizeDelta.x,
            lockHeight ? lockSize.y : rt[i].sizeDelta.y);
        }
    }
}
