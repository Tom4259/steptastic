using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class UIThemeManager : MonoBehaviour
{

#if UNITY_EDITOR

    public bool isDarkMode = false;

    public bool IsDarkMode()
    {
        return isDarkMode;
    }


#elif UNITY_ANDROID && !UNITY_EDITOR                               

    private AndroidJavaObject darkModeHelper;

    private void Start()
    {
        darkModeHelper = new AndroidJavaObject("com.TomIndustries.Steptastic.DarkModeHelper");

        Debug.Log(IsDarkMode());
    }

    public bool IsDarkMode()
    {
        if (darkModeHelper != null)
        {
            return darkModeHelper.CallStatic<bool>("isDarkMode", new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"));
        }
        else
        {
            Debug.LogError("DarkModeHelper not initialized.");
            return false;
        }
    }

#elif UNITY_IOS && !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern bool IsDarkModeEnabled();

    private void Start()
    {
        bool isDarkMode = IsDarkModeEnabled();
        Debug.Log("Dark Mode is enabled: " + isDarkMode);
    }

    public bool IsDarkMode()
    {
        return IsDarkModeEnabled();
    }

#endif
}