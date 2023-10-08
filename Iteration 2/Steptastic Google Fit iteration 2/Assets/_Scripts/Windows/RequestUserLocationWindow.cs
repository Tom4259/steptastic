using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AndroidRuntimePermissionsNamespace;
using Michsky.MUIP;
#if !UNITY_EDITOR
//using Debug = Sisus.Debugging.Debug;
#endif

public class RequestUserLocationWindow : MonoBehaviour
{
    private void Start()
    {
        RequestPermission();
    }

    async void RequestPermission()
    {
        AndroidRuntimePermissions.Permission[] result = await AndroidRuntimePermissions.RequestPermissionsAsync("android.permission.ACCESS_FINE_LOCATION", "android.permission.BACKGROUND_LOCATION");
        if (result[0] == AndroidRuntimePermissions.Permission.Granted)
            Debug.Log("We have permission to access fine location!");
        else
            Debug.Log("Permission state: " + result);


        if (result[1] == AndroidRuntimePermissions.Permission.Granted)
            Debug.Log("We have permission to access background location!");
        else
            Debug.Log("Permission state: " + result);

        // Requesting ACCESS_FINE_LOCATION and CAMERA permissions simultaneously
        //AndroidRuntimePermissions.Permission[] result2 = await AndroidRuntimePermissions.RequestPermissionsAsync( "android.permission.ACCESS_FINE_LOCATION", "android.permission.CAMERA" );
        //if( result[0] == AndroidRuntimePermissions.Permission.Granted && result[1] == AndroidRuntimePermissions.Permission.Granted )
        //	Debug.Log( "We have all the permissions!" );
        //else
        //	Debug.Log( "Some permission(s) are not granted..." );
    }
}
