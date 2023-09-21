using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using LitJson;
using System.Text;

public class WebRequestManager : MonoBehaviour
{
    public class GoogleFit
    {
        public class Authorization
        {
            private static string clientID = "452921919955-n5pr35harq133jfkf2kosvq4kbc724ps.apps.googleusercontent.com";
            private static string clientSecret = "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";
            private static string redirectURL = "https://steptastic-ad9d9.web.app";

            public static void getAuthorizationCode(string URL)
            {
                Application.OpenURL(URL);
            }


            public static IEnumerator exchangeAuthCodeForToken(UnityAction<string> callback)
            {
                WWWForm form = new WWWForm();
                form.AddField("client_id", clientID);
                form.AddField("client_secret", clientSecret);
                form.AddField("code", PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode));
                form.AddField("grant_type", "authorization_code");
                form.AddField("redirect_uri", redirectURL);

                UnityWebRequest www = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);

                yield return www.SendWebRequest();

                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError(www.downloadHandler.text);
                }
                else
                {
                    //Debug.Log(www.downloadHandler.text);

                    JsonData json = JsonMapper.ToObject(www.downloadHandler.text);

                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, json["access_token"].ToString());
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, json["refresh_token"].ToString());

                    callback.Invoke(json["access_token"].ToString());
                }
            }

            public static IEnumerator refreshAccessToken(UnityAction<JsonData> callback)
            {
                WWWForm form = new WWWForm();
                form.AddField("client_id", clientID);
                form.AddField("client_secret", clientSecret);
                form.AddField("refresh_token", PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken));
                form.AddField("grant_type", "authorization_code");

                UnityWebRequest www = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);

                yield return www.SendWebRequest();

                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError(www.downloadHandler.text);
                }
                else
                {
                    Debug.Log(www.downloadHandler.text);

                    JsonData json = JsonMapper.ToJson(www.downloadHandler.text);

                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, json["access_token"].ToString());

                    callback.Invoke(json);
                }
            }
        }

        /*
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
        */

        public static IEnumerator getStepsBetweenMillis(string body, UnityAction<JsonData> callback)
        {
            UnityWebRequest www = UnityWebRequest.PostWwwForm("https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate", body);

            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken));
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            www.uploadHandler.contentType = "application/json";

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                callback.Invoke(www.error);
                Debug.Log(www.downloadHandler.text);
            }
            else
            {
                //Debug.Log(www.downloadHandler.text);
                callback.Invoke(JsonMapper.ToObject(www.downloadHandler.text));
            }
        }
    }
}
