using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testingScript : MonoBehaviour
{
    public void Start()
    {
        List<string> thelist = new List<string>(new string[10]);

        for (int i = 0; i < thelist.Count; i++)
        {
            Debug.Log(thelist[i]);
        }
    }
}
