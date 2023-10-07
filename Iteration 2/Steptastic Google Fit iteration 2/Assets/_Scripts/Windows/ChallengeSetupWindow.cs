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
    private string countriesList;

    [Space]
    public CustomDropdown startLocation;
    public CustomDropdown endLocation;

    [Space]
    public Sprite dropdownIcon;

    [Space(20)]

    public ButtonManager saveButton;

    private void Start()
    {
        populateDropdowns();
    }

    /// <summary>
    /// Adds all country and capital data to the dropdowns
    /// </summary>
    public void populateDropdowns()
    {
        countriesList = Resources.Load<TextAsset>(pathToCountriesResource).ToString();
        JsonData itemData = JsonMapper.ToObject(countriesList);

        //creates a dropdown object and adds it to the dropdown list
        for (int i = 0; i < itemData["Countries"].Count; i++)
        {
            string country = itemData["Countries"][i]["Country"].ToString();

            CustomDropdown.Item option = new CustomDropdown.Item();
            option.itemName = country;
            option.itemIcon = dropdownIcon;

            startLocation.items.Add(option);
            endLocation.items.Add(option);

            startLocation.SetupDropdown();
            endLocation.SetupDropdown();
        }

        //this is where to set the closest locaion to he user if they have accpted location services
        if(PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Permissions.location, false))
        {

        }

        startLocation.selectedItemIndex = 1;
        endLocation.selectedItemIndex = 2;

        startLocation.SetupDropdown();
        endLocation.SetupDropdown();
    }

    public void onDropdownChanged()
    {
        checkValidDropdownItems();
    }

    /// <summary>
    /// checks to see if the user has selected valid options in the dropdowns
    /// </summary>
    /// <returns></returns>
    public bool checkValidDropdownItems()
    {
        if (startLocation.selectedItemIndex == endLocation.selectedItemIndex)
        {
            Debug.LogError("chosen locations are the same");

            // SHOW A MODAL WINDOW HERE!

            saveButton.Interactable(false);

            return false;
        }
        else if ((startLocation.selectedItemIndex == 0) || (endLocation.selectedItemIndex == 0))
        {
            Debug.LogError("need to choose a location");

            // SHOW A MODAL WINDOW HERE!

            saveButton.Interactable(false);

            return false;
        }

        saveButton.Interactable(true);

        return true;
    }


    /// <summary>
    /// saves the start and end lcoation as with the relevant latitude and longitudes to device
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

        PlayerPrefsX.Save();

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
        CanvasManager.instance.SetupCompleted();
    }
}
