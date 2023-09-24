using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LitJson;
using System;

public class ChallengeSetupWindow : MonoBehaviour
{
    public string pathToCountriesResource = "Text/countryCapitalList";
    private string countriesList;

    [Space]
    public TMP_Dropdown startLocation;
    public TMP_Dropdown endLocation;

    private void Start()
    {
        populateDropdowns();
    }

    public void populateDropdowns()
    {
        countriesList = Resources.Load<TextAsset>(pathToCountriesResource).ToString();

        JsonData itemData = JsonMapper.ToObject(countriesList);

        //creates a dropdown object and adds it to the dropdown list
        for (int i = 0; i < itemData["Countries"].Count; i++)
        {
            string country = itemData["Countries"][i]["Country"].ToString();

            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
            option.text = country;
            startLocation.options.Add(option);
            endLocation.options.Add(option);
        }

        startLocation.value = 0;
        endLocation.value = 1;

        startLocation.RefreshShownValue();
        endLocation.RefreshShownValue();
    }


    public void saveChallengeData()
    {
        //code in a better way of showing the user, or add functionality to remove selected item from other dropdown
        if(startLocation.captionText.text == endLocation.captionText.text)
        {
            Debug.LogError("chosen locations are the same");

            return;
        }

        countriesList = Resources.Load<TextAsset>(pathToCountriesResource).ToString();
        JsonData itemData = JsonMapper.ToObject(countriesList);

        //goes through all of the countries list and finds the start and end location, saves the latitude and longitude to the device
        for (int i = 0; i < itemData["Countries"].Count; i++)
        {
            if (itemData["Countries"][i]["Country"].ToString() == startLocation.captionText.text)
            {
                //save as start location
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.startLocationName, itemData["Countries"][i]["Country"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.startLocationCapital, itemData["Countries"][i]["Capital"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.startLocationLatLong,
                    itemData["Countries"][i]["Latitude"].ToString() + "," + itemData["Countries"][i]["Longitude"].ToString());
            }

            if(itemData["Countries"][i]["Country"].ToString() == endLocation.captionText.text)
            {
                //save as end location
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.endLocationName, itemData["Countries"][i]["Country"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.endLocationCapital, itemData["Countries"][i]["Capital"].ToString());
                PlayerPrefsX.SetString(PlayerPrefsLocations.User.Challenge.endLocationLatLong,
                    itemData["Countries"][i]["Latitude"].ToString() + "," + itemData["Countries"][i]["Longitude"].ToString());
            }
        }

        PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Challenge.startDate, DateTime.Today);

        PlayerPrefsX.Save();

        CanvasManager.instance.UserSetUpChallenge();
    }
}
