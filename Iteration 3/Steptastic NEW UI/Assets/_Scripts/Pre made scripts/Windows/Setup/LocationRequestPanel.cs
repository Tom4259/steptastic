using Michsky.MUIP;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationRequestPanel : MonoBehaviour
{
    private SetupManager setupManager;


    public bool testWithLocation;


    [Space(30)]
    public ButtonManager continueButton;



    private void Start()
    {
        setupManager = GetComponentInParent<SetupManager>();
    }


    //will request the user's location (only on android)
    public void RequestLocation()
    {
        Debug.Log("[LocationRequest] " + "Getting location");

        GetLocation();
    }


#if UNITY_ANDROID && !UNITY_EDITOR
    
    //requests the user's location and saves to device
    private async void GetLocation()
    {
        continueButton.Interactable(false);

        useLocationToggle.isOn = true;
        useLocationToggle.GetComponent<CustomToggle>().UpdateState();

        AndroidRuntimePermissions.Permission[] result = await AndroidRuntimePermissions.RequestPermissionsAsync("android.permission.ACCESS_FINE_LOCATION", "android.permission.ACCESS_COARSE_LOCATION");

        //Debug.Log(result[0].ToString());

        //fine Location
        if (result[0] == AndroidRuntimePermissions.Permission.Granted)
        {
            // Check if the user has Location service enabled.
            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("[LocationRequest] " + "Location not enabled on device or app does not have permission to access location");
                PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, false);

                useLocationToggle.isOn = false;
                useLocationToggle.GetComponent<CustomToggle>().UpdateState();

                continueButton.Interactable(true);
            }


            Input.location.Start();

            // Waits until the Location service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                await Task.Delay(1000);

                //Debug.Log("[LocationRequest] " + "Location services are initializing: maxWait " + maxWait);

                useLocationToggle.GetComponent<CustomToggle>().UpdateState();

                maxWait--;
            }

            // If the service didn't initialize in 20 seconds this cancels Location service use.
            if (maxWait < 1)
            {
                Debug.Log("[LocationRequest] " + "Timed out");

                PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, false);

                useLocationToggle.isOn = false;
                useLocationToggle.GetComponent<CustomToggle>().UpdateState();

                continueButton.Interactable(true);

                return;
            }

            // If the connection failed this cancels Location service use.
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("[LocationRequest] " + "Unable to determine device location");

                PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, false);

                useLocationToggle.isOn = false;
                useLocationToggle.GetComponent<CustomToggle>().UpdateState();

                continueButton.Interactable(true);

                return;
            }
            else
            {
                useLocationToggle.GetComponent<CustomToggle>().UpdateState();

                float lat = Input.location.lastData.latitude;
                float lon = Input.location.lastData.longitude;

                // If the connection succeeded, this retrieves the device's current Location and displays it in the Console window.
                Debug.Log("[LocationRequest] " + "Location: " + lat + " " + lon + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);


                PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLatitude, lat);
                PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLongitude, lon);

                PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, true);

                CanvasManager.instance.challengeSetupWindow.SetStartLocationUsingGPS();
            }

            continueButton.Interactable(true);

            PlayerPrefs.Save();

            // Stops the Location service if there is no need to query Location updates continuously.
            Input.location.Stop();
        }
        else
        {
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, false);
        }

        continueButton.onClick.Invoke();
    }

#elif UNITY_IOS

    public void GetLocation()
    {
        Debug.Log("[LocationRequest] Can code this in another time, skipping location for now");

        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, false);

        continueButton.Interactable(true);
        continueButton.onClick.Invoke();
    }

#else
    //uses a set location for development purposes
    private void GetLocation()
    {
        Debug.Log("[LocationRequest]", () => testWithLocation);

        if (testWithLocation)
        {
            float lat = 51.8278225f;
            float lon = -0.339568f;

            PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLatitude, lat);
            PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLongitude, lon);

            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, true);
        }
        else
        {
            PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Permissions.Location, false);
        }

        continueButton.Interactable(true);

        CanvasManager.instance.challengeSetupPanel.SetStartLocationUsingGPS();

        PlayerPrefs.Save();

        continueButton.onClick.Invoke();
    }

#endif


    public void NextPanel()
    {
        setupManager.NextPanel();
    }
}