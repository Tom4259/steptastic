using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using LitJson;
using System.Text;

public class APIManager : MonoBehaviour
{
    /// <summary>
    /// a simple class so it is easier to customise each api request
    /// </summary>
    public struct apiData
    {
        public long startTimeMillis;
        public long endTimeMillis;

        public long durationMillis;
    }

    /// <summary>
    /// a method to create a simple and clean way of passing data into another method
    /// </summary>
    /// <param name="start">the point in history where to start returning data from now on</param>
    /// <param name="end">the point in history to stop getting data from</param>
    /// <param name="timeGap">the amount of milliseconds between each data point</param>
    /// <returns></returns>
    public static apiData GenerateAPIbody(DateTime start, DateTime end, long timeGap = 8600000)
    {
        apiData apiData = new apiData();

        long milliseconds = ((DateTimeOffset)end).ToUnixTimeMilliseconds();

        apiData.startTimeMillis = milliseconds - (milliseconds - ((DateTimeOffset)start).ToUnixTimeMilliseconds());
        apiData.endTimeMillis = milliseconds;
        apiData.durationMillis = timeGap;

        return apiData;
    }


    /// <summary>
    /// I have created classes for different types of web requests
    /// </summary>
    public class GoogleFit
    {
        public class Authorization
        {
            private static readonly string clientID = "452921919955-n5pr35harq133jfkf2kosvq4kbc724ps.apps.googleusercontent.com";
            private static readonly string clientSecret = "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";
            private static readonly string redirectURL = "https://steptastic-ad9d9.web.app";

            /// <summary>
            /// redirects the user to the google authorization page
            /// </summary>
            public static void GetAuthorizationCode(string URL)
            {
                Application.OpenURL(URL);
            }


            /// <summary>
            /// when the user has pressed authorize, my application will be supplied with a authorization token. i then
            /// need to exchange this for an access token and refresh token so i can request the users data
            /// </summary>
            public static IEnumerator ExchangeAuthCodeForToken(UnityAction<string> callback)
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

                    DateTime d = DateTime.Now.AddSeconds(int.Parse(json["expires_in"].ToString()));

                    //saving access and refresh token to the users device
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, json["access_token"].ToString());
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, json["refresh_token"].ToString());
                    PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, d);

                    PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, true);

                    PlayerPrefsX.Save();

                    callback.Invoke(json["access_token"].ToString());
                }
            }

            //havent yet tested or coded this into my application
            public static IEnumerator RefreshAccessToken(UnityAction<JsonData> callback)
            {
                WWWForm form = new WWWForm();
                form.AddField("client_id", clientID);
                form.AddField("client_secret", clientSecret);
                form.AddField("refresh_token", PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken));
                form.AddField("grant_type", "authorization_code");

                Debug.Log(PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken));

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
        public static IEnumerator GetStepsBetweenMillis(apiData data, UnityAction<JsonData> callback)
        {
            string body = "{\"aggregateBy\":[{\"dataTypeName\":\"com.google.step_count.delta\",\"dataSourceId\":\"derived:com.google.step_count.delta:com.google.android.gms:estimated_steps\"}],\"bucketByTime\":{\"durationMillis\":" + data.durationMillis + "},\"startTimeMillis\":" + data.startTimeMillis + ",\"endTimeMillis\":" + data.endTimeMillis + "}";

            UnityWebRequest www = UnityWebRequest.PostWwwForm("https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate", body);

            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken));
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            www.uploadHandler.contentType = "application/json";

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                //callback.Invoke(www.error);
                Debug.Log(www.downloadHandler.text);

                //checkIfRefreshNeeded(data, callback);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                callback.Invoke(JsonMapper.ToObject(www.downloadHandler.text));
            }
        }


        public static IEnumerator GetDistanceBetweenMillis(apiData data, UnityAction<JsonData> callback)
        {
            string body = "{\"aggregateBy\":[{\"dataTypeName\":\"com.google.distance.delta\"}],\"bucketByTime\":{\"durationMillis\":" + data.durationMillis + "},\"startTimeMillis\":" + data.startTimeMillis + ",\"endTimeMillis\":" + data.endTimeMillis + "}";

            UnityWebRequest www = UnityWebRequest.PostWwwForm("https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate", body);

            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken));
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            www.uploadHandler.contentType = "application/json";

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                //callback.Invoke(www.error);
                Debug.Log(www.downloadHandler.text);

                //checkIfRefreshNeeded(data, callback);
            }
            else
            {
                //Debug.Log(www.downloadHandler.text);
                callback.Invoke(JsonMapper.ToObject(www.downloadHandler.text));
            }
        }

        /*
        private static void checkIfRefreshNeeded(apiData d, UnityAction<JsonData> c)
        {
            if(PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken) != null)
            {
                ProcessDeepLinkMngr.Instance.refreshAccessToken(d, c);
            }
        }
        */
    }
}