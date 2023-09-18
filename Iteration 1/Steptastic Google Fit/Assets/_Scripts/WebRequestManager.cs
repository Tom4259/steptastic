using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using LitJson;

public class WebRequestManager : MonoBehaviour
{
    public class Fitbit
    {
        public enum fitbitURLs
        {
            steps,
            distance,
            floors,
            calories
        }

        public Dictionary<fitbitURLs, string> urlList = new Dictionary<fitbitURLs, string>
        {
            {fitbitURLs.steps, "https://api.fitbit.com/1/user/-/activities/steps/date/today/1m.json" },
            {fitbitURLs.distance, "https://api.fitbit.com/1/user/-/activities/distance/date/today/1m.json" },
            {fitbitURLs.floors, "https://api.fitbit.com/1/user/-/activities/floors/date/today/1m.json" },
            {fitbitURLs.calories, "https://api.fitbit.com/1/user/-/activities/calories/date/today/1m.json" }

        };

        public static IEnumerator sendRequestToFitbit(fitbitURLs targetUrl, UnityAction<string> callback)
        {
            
            yield return null;
        }

    }


    public class GoogleFit
    {
        private static string clientID = "";
        private static string clientSecret = "";
        private static string redirectURL = "";



        public static IEnumerator getAccessToken(UnityAction<JsonData> callback)
        {
            UnityWebRequest www = UnityWebRequest.PostWwwForm("https://oauth2.googleapis.com/token", "?client_id=" + clientID +
                "&client_secret=" +
                clientSecret +
                "&redirect_uri=" +
                redirectURL +
                "&grant_type=authorization_code&code=" +
                PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.authorizationCode));

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.Log(www.error);
            }
            else
            {
                callback.Invoke(JsonMapper.ToObject(www.downloadHandler.text));
            }
        }


        public static IEnumerator sendRequestToGoogle(string targetUrl, UnityAction<JsonData> callback)
        {
            UnityWebRequest www = UnityWebRequest.Get(targetUrl);

            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Codes.accessToken));

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                //callback.Invoke(www.error);
            }
            else
            {
                callback.Invoke(JsonMapper.ToObject(www.downloadHandler.text));
            }
        }
    }
}
