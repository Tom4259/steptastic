using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LitJson;
using System;
using Coordinates = UsefulFunctions.Coordinates;
using Michsky.MUIP;

public class ChallengeSetupWindow : MonoBehaviour
{
    public string pathToCountriesResource = "Text/countryCapitalList";
    private JsonData countriesList;

    [Space]
    public CustomDropdown startLocation;
    public CustomDropdown endLocation;

    [Space]
    public Sprite dropdownIcon;

    [Space]
    public NotificationManager sameLocationMessage;

    [Space(20)]
    public ButtonManager saveButton;

    /// <summary>
    /// Adds all country and capital data to the dropdowns
    /// </summary>
    public void PopulateDropdowns()
    {
        countriesList = JsonMapper.ToObject(Resources.Load<TextAsset>(pathToCountriesResource).ToString());

        //loops through the countries list and adds each country as a selectable item
        for (int i = 0; i < countriesList["Countries"].Count; i++)
        {
            string country = countriesList["Countries"][i]["Country"].ToString();

            startLocation.CreateNewItem(country, dropdownIcon);
            endLocation.CreateNewItem(country, dropdownIcon);
        }

        //this is where to set the closest locaion to he user if they have accpted Location services
        if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Permissions.Location, false))
        {

        }

        startLocation.SetupDropdown();
        endLocation.SetupDropdown();
    }


    /// <summary>
    /// Gets the users Location and compares with the whole of the couuntries list to see which is the closest place.
    /// the closest point gets set as the start Location, for ease of access for the user
    /// </summary>
    public void SetStartLocationUsingGPS()
    {
        float lat = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLatitude);
        float lon = PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.UserData.setupLongitude);

        Coordinates userCoords = new Coordinates
        {
            Lat = lat,
            Long = lon
        };


        double closestDistance = double.MaxValue;
        int closestIndex = -1;

        //loops through the whole of the countries file and finds the closest country to the user
        for (int i = 0; i < countriesList["Countries"].Count; i++)
        {
            float itemLat = float.Parse(countriesList["Countries"][i]["Latitude"].ToString());
            float itemLong = float.Parse(countriesList["Countries"][i]["Longitude"].ToString());

            Coordinates itemCoords = new Coordinates
            {
                Lat = itemLat,
                Long = itemLong
            };

            double itemDistance = UsefulFunctions.DistanceTo(userCoords, itemCoords);

            if (itemDistance < closestDistance)
            {
                closestIndex = i;
                closestDistance = itemDistance;

                //Debug.Log("[" + GetType().Name + "] ", "Closest city: " + countriesList["Countries"][i]["Country"].ToString());
            }
        }

        Debug.Log("[" + GetType().Name + "] " + "Closest city to the user is " + countriesList["Countries"][closestIndex]["Country"].ToString() + ", " + countriesList["Countries"][closestIndex]["Capital"].ToString());

        if (closestIndex >= 0)
        {
            startLocation.SetDropdownIndex(closestIndex);
        }
        else
        {
            Debug.LogError("[" + GetType().Name + "]", "closestIndex: " + closestIndex + ", closestDistance: " + closestDistance);
        }

        //remove the SELECT option from the dropdown items
        startLocation.RemoveItem("SELECT", true);
    }

    /// <summary>
    /// called when the selected dropdown item has been changed
    /// </summary>
    public void onDropdownChanged()
    {
        if(startLocation.selectedItemIndex != 0)
        {
            StartCoroutine(updateDropdown(startLocation));
        }

        if(endLocation.selectedItemIndex != 0)
        {
            StartCoroutine(updateDropdown(endLocation));
        }        
    }

    /// <summary>
    /// waits a certain amount of time before updating the dropdown list to prevent a UI lag
    /// </summary>
    private IEnumerator updateDropdown(CustomDropdown d)
    {
        yield return new WaitForSeconds(0.2f);

        if (d.items[0].itemName == "SELECT")
        {
            d.SetDropdownIndex(d.selectedItemIndex - 1);
        } 
        
        d.RemoveItem("SELECT", true);

        checkValidDropdownItems();
    }


    /// <summary>
    /// checks to see if the user has selected valid options in the dropdowns
    /// </summary>
    /// <returns></returns>
    public bool checkValidDropdownItems()
    {
        if (dropdownsTheSame())
        {
            Debug.LogError("[" + GetType().Name + "]", "chosen locations are the same");

            sameLocationMessage.Open();

            saveButton.Interactable(false);

            return false;
        }
        else if ((startLocation.items[startLocation.selectedItemIndex].itemName == "SELECT") || (endLocation.items[endLocation.selectedItemIndex].itemName == "SELECT"))
        {
            Debug.LogWarning("[" + GetType().Name + "]", "need to choose a location");

            saveButton.Interactable(false);

            return false;
        }

        saveButton.Interactable(true);

        return true;
    }

    /// <summary>
    /// returns a bool depending on if the same item is selected in both dropdowns
    /// </summary>
    /// <returns></returns>
    private bool dropdownsTheSame()
    {
        string startName = startLocation.items[startLocation.selectedItemIndex].itemName;
        string endName = endLocation.items[endLocation.selectedItemIndex].itemName;

        Debug.Log("[" + GetType().Name + "]", () => startName);
        Debug.Log("[" + GetType().Name + "]", () => endName);

        return (startLocation.items[startLocation.selectedItemIndex].itemName == endLocation.items[endLocation.selectedItemIndex].itemName);
    }


    /// <summary>
    /// saves the start and end lation as with the relevant latitude and longitudes to device
    /// </summary>
    public void SaveChallengeData()
    {
        if (!checkValidDropdownItems()) return;


        //goes through all of the countries list and finds the start and end Location, saves the latitude and longitude to the device
        for (int i = 0; i < countriesList["Countries"].Count; i++)
        {
            if (countriesList["Countries"][i]["Country"].ToString() == startLocation.items[startLocation.selectedItemIndex].itemName)
            {
                //save as start Location
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationName, countriesList["Countries"][i]["Country"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationCapital, countriesList["Countries"][i]["Capital"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong,
                    countriesList["Countries"][i]["Latitude"].ToString() + "," + countriesList["Countries"][i]["Longitude"].ToString());
            }

            if(countriesList["Countries"][i]["Country"].ToString() == endLocation.items[endLocation.selectedItemIndex].itemName)
            {
                //save as end Location
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationName, countriesList["Countries"][i]["Country"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationCapital, countriesList["Countries"][i]["Capital"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong,
                    countriesList["Countries"][i]["Latitude"].ToString() + "," + countriesList["Countries"][i]["Longitude"].ToString());
            }
        }

        PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, DateTime.Today);

        calculateDistanceToTarget();
    }


    private void calculateDistanceToTarget()
    {
        //calculates the distance between 2 latitude and longitudes

        Coordinates start = new Coordinates { Lat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[0]),
            Long = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong).Split(',')[1])};

        Coordinates end = new Coordinates { Lat = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[0]),
            Long = float.Parse(PlayerPrefs.GetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong).Split(',')[1]) };

        double distance = UsefulFunctions.DistanceTo(start, end);

        Debug.Log("[" + GetType().Name + "]", () => distance);

        PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget, (float)distance);

        closeChallengeSetup();
    }

    private void closeChallengeSetup() 
    {
        PlayerPrefsX.Save();

        CanvasManager.instance.SetupCompleted();
    }
}