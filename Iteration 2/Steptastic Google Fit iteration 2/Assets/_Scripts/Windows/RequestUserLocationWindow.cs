using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AndroidRuntimePermissionsNamespace;
using Michsky.MUIP;
using UnityEngine.UI;
using Sisus.Debugging.Console;
#if !UNITY_EDITOR
//using Debug = Sisus.Debugging.Debug;
#endif

public class RequestUserLocationWindow : MonoBehaviour
{

    public bool testWithLocation;

    [Space]
    public Toggle toggle;

    [Space(10)]
    public ButtonManager continueButton;

    public void requestLocation()
    {
        if (toggle.isOn)
        {
            Debug.Log("[" + GetType().Name + "] " + "Getting location");

            StartCoroutine(getLocation());
        }
    }

    private IEnumerator getLocation()
    {
        continueButton.Interactable(false);

#if !UNITY_EDITOR
        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("[" + GetType().Name + "] " + "Location not enabled on device or app does not have permission to access location");
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.location, false);

            toggle.isOn = false;
            toggle.GetComponent<CustomToggle>().UpdateState();

            continueButton.Interactable(true);
        }


        Input.location.Start();

        // Waits until the location service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds this cancels location service use.
        if (maxWait < 1)
        {
            Debug.Log("[" + GetType().Name + "] " + "Timed out");

            toggle.isOn = false;
            toggle.GetComponent<CustomToggle>().UpdateState();

            continueButton.Interactable(true);

            yield break;
        }

        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("[" + GetType().Name + "] " + "Unable to determine device location");

            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.location, false);

            toggle.isOn = false;
            toggle.GetComponent<CustomToggle>().UpdateState();

            continueButton.Interactable(true);

            yield break;
        }
        else
        {
            float lat = Input.location.lastData.latitude;
            float lon = Input.location.lastData.longitude;

            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
            Debug.Log("[" + GetType().Name + "] " + "Location: " + lat + " " + lon + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);


            PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLatitude, lat);
            PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLongitude, lon);

            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.location, true);

            CanvasManager.instance.challengeSetupWindow.SetStartLocationUsingGPS();
        }

        continueButton.Interactable(true);

        PlayerPrefs.Save();

        // Stops the location service if there is no need to query location updates continuously.
        Input.location.Stop();
#else

        Debug.Log("[" + GetType().Name + "]", () => testWithLocation);

        if (testWithLocation)
        {
            float lat = 51.8278225f;
            float lon = -0.339568f;

            PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLatitude, lat);
            PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLongitude, lon);

            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.location, true);
        }
        else
        {
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.location, false);

            toggle.isOn = false;
            toggle.GetComponent<CustomToggle>().UpdateState();
        }

        continueButton.Interactable(true);

        CanvasManager.instance.challengeSetupWindow.SetStartLocationUsingGPS();

        PlayerPrefs.Save();

        yield return null;

#endif
    }

    public void completedLocation()
    {
        if (toggle.isOn)
        {
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.location, true);
        }
        else
        {
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.location, false);
        }
    }
}