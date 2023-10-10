using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using LitJson;
using System.Text;
using UnityEngine.UI;
using System.Threading.Tasks;

public class APIManager : MonoBehaviour
{
    /// <summary>
    /// I have created classes for different types of web requests
    /// </summary>
    public class GoogleFit
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
        public static apiData GenerateAPIbody(DateTime start, DateTime end, long timeGap = 86400000)
        {
            long milliseconds = ((DateTimeOffset)end).ToUnixTimeMilliseconds();

            apiData apiData = new apiData()
            {
                startTimeMillis = milliseconds - (milliseconds - ((DateTimeOffset)start).ToUnixTimeMilliseconds()),
                endTimeMillis = milliseconds,
                durationMillis = timeGap
            };

            //Debug.Log("[APIManager]", () => apiData.startTimeMillis);
            //Debug.Log("[APIManager]", () => apiData.endTimeMillis);
            //Debug.Log("[APIManager]", () => apiData.durationMillis);

            return apiData;
        }


        public class Authorization
        {
            private static readonly string clientID = "452921919955-n5pr35harq133jfkf2kosvq4kbc724ps.apps.googleusercontent.com";
            private static readonly string clientSecret = "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";
            private static readonly string redirectURL = "https://steptastic-ad9d9.web.app";

            /// <summary>
            /// redirects the user to the google authorization page
            /// </summary>
            public static void GetAuthorizationCode()
            {
                string scope = "https://www.googleapis.com/auth/fitness.activity.read https://www.googleapis.com/auth/fitness.location.read";

                string authURL = "https://accounts.google.com/o/oauth2/v2/auth";

                string URL = authURL +
                    "?client_id=" + clientID +
                    "&redirect_uri=https://steptastic-ad9d9.web.app" +
                    "&scope=" + scope +
                    "&response_type=code" +
                    "&access_type=offline";

                Application.OpenURL(URL);
            }


            /// <summary>
            /// when the user has pressed authorize, my application will be supplied with a authorization token. i then
            /// need to exchange this for an access token and refresh token so i can request the users data
            /// </summary>
            public static async Task<string> ExchangeAuthCodeForToken()
            {
                // adds fields to the request
                WWWForm form = new WWWForm();
                form.AddField("client_id", clientID);
                form.AddField("client_secret", clientSecret);
                form.AddField("code", PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.authorizationCode));
                form.AddField("grant_type", "authorization_code");
                form.AddField("redirect_uri", redirectURL);

                UnityWebRequest www = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);

                var operation = www.SendWebRequest();

                while(!operation.isDone)
                {
                    await Task.Yield();
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError(www.downloadHandler.text);
                    return www.downloadHandler.text;
                }
                else
                {
                    Debug.Log("[APIManager]", () => www.downloadHandler.text);

                    JsonData json = JsonMapper.ToObject(www.downloadHandler.text);

                    DateTime d = DateTime.Now.AddSeconds(int.Parse(json["expires_in"].ToString()));

                    //saving access and refresh token to the users device
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, json["access_token"].ToString());
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken, json["refresh_token"].ToString());
                    PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, d);

                    PlayerPrefsX.SetBool(PlayerPrefsLocations.User.Account.authenticated, true);

                    PlayerPrefsX.Save();

                    return json["access_token"].ToString();

                    //callback.Invoke(json["access_token"].ToString());
                }
            }

            /// <summary>
            /// when the access token expires, this methods is called. google then supplies my application with a new access token
            /// to access the user's data with
            /// </summary>
            public static async Task<JsonData> RefreshAccessToken()
            {
                WWWForm form = new WWWForm();
                form.AddField("client_id", clientID);
                form.AddField("client_secret", clientSecret);
                form.AddField("refresh_token", PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken));
                form.AddField("grant_type", "refresh_token");

                Debug.Log("[APIManager]" + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken));

                UnityWebRequest www = UnityWebRequest.Post("https://oauth2.googleapis.com/token", form);

                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError(www.downloadHandler.text);
                    return (JsonMapper.ToObject(www.downloadHandler.text));
                }
                else
                {
                    Debug.Log("[APIManager]", () => www.downloadHandler.text);

                    JsonData json = JsonMapper.ToObject(www.downloadHandler.text);

                    DateTime d = DateTime.Now.AddSeconds(int.Parse(json["expires_in"].ToString()));

                    //saving new access and refresh token to the users device
                    PlayerPrefsX.SetString(PlayerPrefsLocations.User.Account.Credentials.accessToken, json["access_token"].ToString());
                    PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Account.Credentials.expiresIn, d);

                    return json;

                    //callback.Invoke(json);
                }
            }
        }


        /// <summary>
        /// this coroutine takes in the requests body as a parameter, as the start and end time of the request can change.
        /// the request will then get the step count between the timestamps
        /// </summary>
        public static async Task<JsonData> GetStepsBetweenMillis(apiData data)
        {
            string body = "{\"aggregateBy\":[{\"dataTypeName\":\"com.google.step_count.delta\",\"dataSourceId\":\"derived:com.google.step_count.delta:com.google.android.gms:estimated_steps\"}],\"bucketByTime\":{\"durationMillis\":" + data.durationMillis + "},\"startTimeMillis\":" + data.startTimeMillis + ",\"endTimeMillis\":" + data.endTimeMillis + "}";

            UnityWebRequest www = UnityWebRequest.PostWwwForm("https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate", body);

            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken));
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            www.uploadHandler.contentType = "application/json";

            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogWarning("[APIManager] Attempting to refresh the access token");

                //tries to refresh the token if the request has failed
                JsonData errorRefresh = await Authorization.RefreshAccessToken();

                try
                {
                    string _ = errorRefresh["access_token"].ToString();

                    JsonData json = await GetStepsBetweenMillis(data);

                    return json;
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError("[APIManager] failed to send request and refresh attempt: \n" + www.downloadHandler.text + "\n" + errorRefresh.ToJson());

                    return JsonMapper.ToObject(www.downloadHandler.text);
                }
            }
            else
            {
                Debug.Log("[APIManager]", () => www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
        }

        /// <summary>
        /// this coroutine takes in the requests body as a parameter, as the start and end time of the request can change.
        /// the request will then get the distance made between the timestamps
        /// </summary>
        public static async Task<JsonData> GetDistanceBetweenMillis(apiData data)
        {
            string body = "{\"aggregateBy\":[{\"dataTypeName\":\"com.google.distance.delta\"}],\"bucketByTime\":{\"durationMillis\":" + data.durationMillis + "},\"startTimeMillis\":" + data.startTimeMillis + ",\"endTimeMillis\":" + data.endTimeMillis + "}";

            UnityWebRequest www = UnityWebRequest.PostWwwForm("https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate", body);

            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.accessToken));
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            www.uploadHandler.contentType = "application/json";

            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogWarning("[APIManager] Attempting to refresh the access token");

                //tries to refresh the token if the request has failed
                JsonData errorRefresh = await Authorization.RefreshAccessToken();

                try
                {
                    string _ = errorRefresh["access_token"].ToString();

                    JsonData json = await GetStepsBetweenMillis(data);

                    return json;
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError("[APIManager] failed to send request and refresh attempt: \n" + www.downloadHandler.text + "\n" + errorRefresh.ToJson());

                    return JsonMapper.ToObject(www.downloadHandler.text);
                }
            }
            else
            {
                Debug.Log("[APIManager]", () => www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
        }
    }

    public class MapQuest
    {
        public class MapData
        {
            public string startLocation;
            public string endLocation;

            public string location1;
            public string location2;

            public float currentLattitude;
            public float currentLongitude;

            public int imageWidth;
            public int imageHeight;

            public int zoom;

            public Image imageToSet;

            public UnityAction callback = null;
        }

        public static async void getMapImage(MapData data)
        {
            string URL = "https://www.mapquestapi.com/staticmap/v5/map?key=frXZBd4uCdYXhcwhVMPsug3yjf6oXQ5b";
            URL += "&shape=" + data.startLocation + "|" + data.endLocation;
            URL += "&locations=" + data.location1 + "|flag-start-md||" + data.location2 + "|flag-end-md||" + data.currentLattitude + "," + data.currentLongitude + "|flag-you-sm";
            URL += "&size=" + data.imageWidth + "," + data.imageHeight + "@2x";
            URL += "&zoom=" + data.zoom;
            URL += "&routeArc=true";

            //Debug.Log("[APIManager]", () => URL);

            UnityWebRequest www = UnityWebRequestTexture.GetTexture(URL);

            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError(string.Format("Error: {0}", www.error));
            }
            else
            {
                Texture2D t = DownloadHandlerTexture.GetContent(www);

                data.imageToSet.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);
                data.imageToSet.color = Color.white;

                if(data.callback != null)
                {
                    data.callback.Invoke();
                }
            }
        }
    }
}