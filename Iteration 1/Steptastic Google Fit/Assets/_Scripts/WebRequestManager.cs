using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using LitJson;

public class WebRequestManager : MonoBehaviour
{
    public class GoogleFit
    {
        private static string clientID = "452921919955-n5pr35harq133jfkf2kosvq4kbc724ps.apps.googleusercontent.com";
        private static string clientSecret = "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";
        private static string redirectURL = "https://steptastic-ad9d9.web.app";



        public static IEnumerator getAccessToken(UnityAction<JsonData> callback)
        {
            WWWForm form = new WWWForm();
            form.AddField("client_id", clientID);
            form.AddField("client_secret", clientSecret);
            form.AddField("redirect_uri", redirectURL);
            form.AddField("grant_type", "authorization_code");
            form.AddField("code", PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.authorizationCode));

            UnityWebRequest www = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);// +
            //    "?client_id=" + clientID +
            //    "&client_secret=" + clientSecret +
            //    "&redirect_uri=" + redirectURL +
            //    "&grant_type=authorization_code&code=" +
            //    PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.authorizationCode), form);

            //UnityWebRequest www = UnityWebRequest.PostWwwForm("https://oauth2.googleapis.com/token", "{\"client_id\":" +  clientID + ", \"client_secret\" :" + clientSecret + ", \"redirect_uri\" :" + redirectURL + ", \"grant_type\" : \"authorization_code\", \"code\" :" + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.authorizationCode) + "}");

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.Log(www.error);
                Debug.Log(www.downloadHandler.text);
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

        public static IEnumerator sendRequestToGoogle(string targetUrl, string body, UnityAction<JsonData> callback)
        {
            UnityWebRequest www = UnityWebRequest.Get(targetUrl);

            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Codes.accessToken));
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));

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
