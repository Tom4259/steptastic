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
    /// <summary>
    /// I have created classes for different types of web requests
    /// </summary>
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


            /// <summary>
            /// when the user has pressed authorize, my application will be supplied with a authorization token. i then
            /// need to exchange this for an access token and refresh token so i can request the users data
            /// </summary>
            public static IEnumerator exchangeAuthCodeForToken(UnityAction<string> callback)
            {
                // adds fields to the request
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

                    //saving access and refresh token to the users device
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, json["access_token"].ToString());
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, json["refresh_token"].ToString());

                    callback.Invoke(json["access_token"].ToString());
                }
            }

            //havent yet tested or coded this into my application
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

        /// <summary>
        /// this coroutine takes in the requests body as a parameter, as the start and end time of the request can change.
        /// the request will then get the step count between the timestamps
        /// </summary>
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