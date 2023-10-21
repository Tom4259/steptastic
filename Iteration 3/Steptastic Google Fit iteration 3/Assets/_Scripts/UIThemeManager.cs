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

    //private AndroidJavaObject javaClass = new AndroidJavaObject("com.tomindustries.darkmodedetectorlibrary.AndroidThemDetector");


    private void Start()
    {
        bool dark = IsDarkMode();

        Debug.Log(dark);
    }

    
    public bool IsDarkMode()
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

    public bool IsDarkMode()
    {
        return IsDarkModeEnabled();
    }

#endif
}