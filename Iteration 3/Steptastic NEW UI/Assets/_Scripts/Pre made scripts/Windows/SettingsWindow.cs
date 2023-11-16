using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.MUIP;
using UnityEngine.UI;

public class SettingsWindow : MonoBehaviour
{
    public GameObject challengeSettingsWindow;

    [Space]
    public SwitchManager unitsSwitch;


    private void Start()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        unitsSwitch.isOn = PlayerPrefsX.GetBool(PlayerPrefsLocations.User.Preferences.unitsMetric, true);

        unitsSwitch.UpdateUI();
    }



    public void CloseAllSubwindows()
    {

    }

    public void UpdateUnits()
    {
        PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Preferences.unitsMetric, unitsSwitch.isOn);
    }
}
