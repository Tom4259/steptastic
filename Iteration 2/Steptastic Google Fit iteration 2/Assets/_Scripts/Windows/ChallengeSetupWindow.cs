using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LitJson;
using System;
using Coordinates = UsefulFunctions.Coordinates;
using Michsky.MUIP;
#if !UNITY_EDITOR
//using Debug = Sisus.Debugging.Debug;
#endif

public class ChallengeSetupWindow : MonoBehaviour
{
    public string pathToCountriesResource = "Text/countryCapitalList";
    private string countriesList;

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
        countriesList = Resources.Load<TextAsset>(pathToCountriesResource).ToString();
        JsonData itemData = JsonMapper.ToObject(countriesList);

        //creates a dropdown object and adds it to the dropdown list
        for (int i = 0; i < itemData["Countries"].Count; i++)
        {
            string country = itemData["Countries"][i]["Country"].ToString();

            startLocation.CreateNewItem(country, dropdownIcon);
            endLocation.CreateNewItem(country, dropdownIcon);
        }

        //this is where to set the closest locaion to he user if they have accpted location services
        if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Permissions.location, false))
        {

        }

        startLocation.SetupDropdown();
        endLocation.SetupDropdown();
    }

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


        countriesList = Resources.Load<TextAsset>(pathToCountriesResource).ToString();
        JsonData itemData = JsonMapper.ToObject(countriesList);

        //goes through all of the countries list and finds the start and end location, saves the latitude and longitude to the device
        for (int i = 0; i < itemData["Countries"].Count; i++)
        {
            if (itemData["Countries"][i]["Country"].ToString() == startLocation.items[startLocation.selectedItemIndex].itemName)
            {
                //save as start location
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationName, itemData["Countries"][i]["Country"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationCapital, itemData["Countries"][i]["Capital"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.startLocationLatLong,
                    itemData["Countries"][i]["Latitude"].ToString() + "," + itemData["Countries"][i]["Longitude"].ToString());
            }

            if(itemData["Countries"][i]["Country"].ToString() == endLocation.items[endLocation.selectedItemIndex].itemName)
            {
                //save as end location
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationName, itemData["Countries"][i]["Country"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationCapital, itemData["Countries"][i]["Capital"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.ChallengeData.endLocationLatLong,
                    itemData["Countries"][i]["Latitude"].ToString() + "," + itemData["Countries"][i]["Longitude"].ToString());
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
