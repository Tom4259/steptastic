using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Events;
using System;

[ExecuteInEditMode]
public class UIThemeManager : MonoBehaviour
{
    public UnityAction onDarkMode;
    public UnityAction onLightMode;


#if UNITY_EDITOR

    public bool isDarkMode = false;


    private void Update()
    {
        try
        {
            if (isDarkMode) onDarkMode.Invoke();
            else onLightMode.Invoke();
        }
        catch (NullReferenceException) { }
    }

    public bool IsDarkMode()
    {
        return isDarkMode;
    }


#elif UNITY_ANDROID && !UNITY_EDITOR                               

    private void Update()
    {
        AndroidJavaObject javaClass = new AndroidJavaObject("com.tomindustries.darkmodedetectorlibrary.AndroidThemDetector");
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

        bool dark = javaClass.Call<bool>("IsDarkTheme", context);

        if (dark) onDarkMode.Invoke();
        else onLightMode.Invoke();
    }


    private bool IsDarkMode()
    {
        AndroidJavaObject javaClass = new AndroidJavaObject("com.tomindustries.darkmodedetectorlibrary.AndroidThemDetector");
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

        bool dark = javaClass.Call<bool>("IsDarkTheme", context);

        return dark;
    }

#elif UNITY_IOS && !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern bool IsDarkModeEnabled();

    private void Start()
    {
        bool isDarkMode = IsDarkModeEnabled();
        Debug.Log("Dark Mode is enabled: " + isDarkMode);
    }

    private void Update()
    {
        if(IsDarkModeEnabled()) onDarkMode.Invoke();
        else onLightMode?.Invoke();
    }

    public bool IsDarkMode()
    {
        return IsDarkModeEnabled();
    }

#endif
}