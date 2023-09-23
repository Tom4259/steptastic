using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class editorPlayerPrefs : MonoBehaviour
{
    public string key;
    public string value;

    private void Start()
    {
        PlayerPrefsX.SetString(key, value);
    }
}
