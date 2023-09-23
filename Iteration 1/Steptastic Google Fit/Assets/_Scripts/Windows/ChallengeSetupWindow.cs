using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LitJson;

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
        if(startLocation.itemText.text == endLocation.itemText.text)
        {
            Debug.LogError("chosen locations are the same");

            return;
        }
    }
}
