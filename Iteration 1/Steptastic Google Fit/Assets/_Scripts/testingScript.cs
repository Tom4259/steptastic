using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testingScript : MonoBehaviour
{
    public void setStartDate()
    {
        string s = GetComponent<TMPro.TMP_InputField>().text;

        DateTime d = Convert.ToDateTime(s);

        Debug.Log("Set custom datew to: " + d.Day + " " + d.Month + " " + d.Year);

        PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, d);

        PlayerPrefs.Save();
    }
}
