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
using System.Runtime.CompilerServices;
using BeliefEngine.HealthKit;
using UnityEngine.XR;

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
        public struct ApiData
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
        public static ApiData GenerateAPIbody(DateTime start, DateTime end, long timeGap = 86400000)
        {
            long milliseconds = ((DateTimeOffset)end).ToUnixTimeMilliseconds();

            ApiData apiData = new ApiData()
            {
                startTimeMillis = milliseconds - (milliseconds - ((DateTimeOffset)start).ToUnixTimeMilliseconds()),
                endTimeMillis = milliseconds,
                durationMillis = timeGap
            };

            //Debug.Log("[GoogleFitAPI]", () => apiData.startTimeMillis);
            //Debug.Log("[GoogleFitAPI]", () => apiData.endTimeMillis);
            //Debug.Log("[GoogleFitAPI]", () => apiData.durationMillis);

            return apiData;
        }


        public class Authorisation
        {
            private static readonly string clientID = "452921919955-n5pr35harq133jfkf2kosvq4kbc724ps.apps.googleusercontent.com";
            private static readonly string clientSecret = "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";
            private static readonly string redirectURL = "https://steptastic-ad9d9.web.app";

            /// <summary>
            /// redirects the user to the google authorization page
            /// </summary>
            public static void GetAuthorisationCode()
            {
                string scope = "https://www.googleapis.com/auth/fitness.activity.read https://www.googleapis.com/auth/fitness.location.read";

                string authURL = "https://accounts.google.com/o/oauth2/v2/auth";

                string URL = authURL +
                    "?client_id=" + clientID +
                    "&redirect_uri=https://steptastic-ad9d9.web.app" +
                    "&scope=" + scope +
                    "&response_type=code" +
                    "&access_type=offline" +
                    "&include_granted_scopes=true";

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

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError("[GoogleFitAPI]" + www.downloadHandler.text);
                    return www.downloadHandler.text;
                }
                else
                {
                    Debug.Log("[GoogleFitAPI]", () => www.downloadHandler.text);

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

                Debug.Log("[GoogleFitAPI]" + PlayerPrefsX.GetString(PlayerPrefsLocations.User.Account.Credentials.refreshToken));

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
                    Debug.Log("[GoogleFitAPI]", () => www.downloadHandler.text);

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

        #region steps between millis

        /// <summary>
        /// this coroutine takes in the requests body as a parameter, as the start and end time of the request can change.
        /// the request will then get the step count between the timestamps
        /// </summary>
        public static async Task<JsonData> GetStepsBetweenMillis(ApiData data)
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
                Debug.LogWarning("[GoogleFitAPI] Attempting to refresh the access token");

                //tries to refresh the token if the request has failed
                JsonData errorRefresh = await Authorisation.RefreshAccessToken();

                try
                {
                    string _ = errorRefresh["access_token"].ToString();

                    JsonData json = await GetStepsBetweenMillisRetry(data);

                    return json;
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError("[GoogleFitAPI] failed to send request and refresh attempt: \n" + www.downloadHandler.text + "\n" + errorRefresh.ToJson());

                    return JsonMapper.ToObject(www.downloadHandler.text);
                }
            }
            else
            {
                Debug.Log("[GoogleFitAPI]", () => www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
        }

        //if the token refresh doesn't work, then stop
        private static async Task<JsonData> GetStepsBetweenMillisRetry(ApiData data)
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
                Debug.LogError("[GoogleFitAPI] Failed to send request and refresh attempt: \n" + www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
            else
            {
                Debug.Log("[GoogleFitAPI]", () => www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
        }


        #endregion

        #region distance between millis

        /// <summary>
        /// this coroutine takes in the requests body as a parameter, as the start and end time of the request can change.
        /// the request will then get the distance made between the timestamps
        /// </summary>
        public static async Task<JsonData> GetDistanceBetweenMillis(ApiData data)
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
                Debug.LogWarning("[GoogleFitAPI] Attempting to refresh the access token");

                //tries to refresh the token if the request has failed
                JsonData errorRefresh = await Authorisation.RefreshAccessToken();

                try
                {
                    string _ = errorRefresh["access_token"].ToString();

                    JsonData json = await GetDistanceBetweenMillisRetry(data);

                    return json;
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogError("[GoogleFitAPI] Failed to send request and refresh attempt: \n" + www.downloadHandler.text + "\n" + errorRefresh.ToJson());

                    return JsonMapper.ToObject(www.downloadHandler.text);
                }
            }
            else
            {
                Debug.Log("[GoogleFitAPI]", () => www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
        }

        //if the token refresh doesn't work, then stop
        private static async Task<JsonData> GetDistanceBetweenMillisRetry(ApiData data)
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
                Debug.LogError("[GoogleFitAPI] Failed to send request and refresh attempt: \n" + www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
            else
            {
                Debug.Log("[GoogleFitAPI]", () => www.downloadHandler.text);

                return JsonMapper.ToObject(www.downloadHandler.text);
            }
        }

        #endregion
    }

    public class HealthKit
    {
        static HealthKitService HK = HealthKitService.Instance;


        public class Authorisation
        {
            public static void Authorise(UnityAction<bool> callback)
            {
                //initialise healthkit here
                Debug.Log("[HealthKitAPI] Authorising HealthKit");

                HK.healthStore.Authorize(HK.dataTypes, delegate (bool success)
                {
                    Debug.LogFormat("[HealthKitAPI] HealthKit authorisation: {0}", success);
                });

                callback.Invoke(HK.healthStore.IsHealthDataAvailable());
            }
        }



        public class QuantityData
        {
            public DateTimeOffset startDate; 
            public DateTimeOffset endDate;
            public double value;
        }


        #region values


        public static async Task<double> GetSteps(DateTime startPoint, DateTime endPoint)
        {
            double totalSteps = 0;
            bool done = false;

            HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierStepCount, startPoint, endPoint, delegate (List<QuantitySample> samplesW)
            {
                if (samplesW.Count > 0)
                {
                    foreach (QuantitySample sample in samplesW)
                    {
                        //Debug.Log(String.Format("[HealthKitAPI] {0} from {1} to {2}", sample.quantity.doubleValue, sample.startDate, sample.endDate));
                        totalSteps += sample.quantity.doubleValue;
                    }

                    done = true;
                }
                else
                {
                    Debug.LogError("[HealthKitAPI] samples count is " + samplesW.Count + " Start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));
                }
            });

            while (!done)
            {
                await Task.Delay(500);
            }

            return totalSteps;
        }


        public static async Task<double> GetDistance(DateTimeOffset startPoint, DateTimeOffset endPoint)
        {
            double totalDistance = 0;
            bool done = false;

            HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierDistanceWalkingRunning, startPoint, endPoint, delegate (List<QuantitySample> samplesW)
            {
                if (samplesW.Count > 0)
                {
                    foreach (QuantitySample sample in samplesW)
                    {
                        //Debug.Log(String.Format("[HealthKitAPI] {0} from {1}{3} to {2}", sample.quantity.doubleValue, sample.startDate, sample.endDate, sample.quantity.unit));
                        totalDistance += sample.quantity.doubleValue;
                    }

                    //can set units in playerprefs to the value unit

                    done = true;
                }
                else
                {
                    Debug.LogError("[HealthKitAPI] samples count is " + samplesW.Count + " Start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));
                }
            });


            while (!done)
            {
                await Task.Delay(500);
            }

            return totalDistance;
        }

        #endregion

        #region lists

        public static async Task<List<QuantityData>> GetStepsList(DateTime startPoint, DateTime endPoint)
        {
            List<QuantityData> stepsList = new List<QuantityData>();
            bool done = false;

            HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierStepCount, startPoint, endPoint, delegate (List<QuantitySample> samplesW)
            {
                if (samplesW.Count > 0)
                {
                    foreach (QuantitySample sample in samplesW)
                    {
                        //Debug.Log(String.Format("[HealthKitAPI] {0} from {1} to {2}", sample.quantity.doubleValue, sample.startDate, sample.endDate));

                        QuantityData item = new QuantityData()
                        {
                            startDate = sample.startDate,
                            endDate = sample.endDate,
                            value = sample.quantity.doubleValue
                        };

                        stepsList.Add(item);
                    }

                    done = true;
                }
                else
                {
                    Debug.LogError("[HealthKitAPI] samples count is " + samplesW.Count + " Start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));
                }
            });

            while (!done)
            {
                await Task.Delay(500);
            }

            return stepsList;
        }


        public static async Task<List<QuantityData>> GetDistanceList(DateTime startPoint, DateTime endPoint)
        {
            List<QuantityData> distanceList = new List<QuantityData>();
            bool done = false;

            HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierDistanceWalkingRunning, startPoint, endPoint, delegate (List<QuantitySample> samplesW)
            {
                if (samplesW.Count > 0)
                {
                    foreach (QuantitySample sample in samplesW)
                    {
                        //Debug.Log(String.Format("[HealthKitAPI] {0} from {1} to {2}", sample.quantity.doubleValue, sample.startDate, sample.endDate));

                        QuantityData item = new QuantityData()
                        {
                            startDate = sample.startDate,
                            endDate = sample.endDate,
                            value = sample.quantity.doubleValue
                        };

                        distanceList.Add(item);
                    }

                    done = true;
                }
                else
                {
                    Debug.LogError("[HealthKitAPI] samples count is " + samplesW.Count + " Start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));
                }
            });

            while (!done)
            {
                await Task.Delay(500);
            }

            return distanceList;
        }

        #endregion
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

        /// <summary>
        /// Downloads image from API using specified data passed into the method
        /// </summary>
        public static async void GetMapImage(MapData data)
        {
            string URL = "https://www.mapquestapi.com/staticmap/v5/map?key=frXZBd4uCdYXhcwhVMPsug3yjf6oXQ5b";
            URL += "&shape=" + data.startLocation + "|" + data.endLocation;
            URL += "&locations=" + data.location1 + "|flag-start-md||" + data.location2 + "|flag-end-md||" + data.currentLattitude + "," + data.currentLongitude + "|flag-you-sm";
            URL += "&size=" + data.imageWidth + "," + data.imageHeight + "@2x";
            URL += "&zoom=" + data.zoom;
            URL += "&routeArc=true";

            //Debug.Log("[MapQuestAPI]", () => URL);

            UnityWebRequest www = UnityWebRequestTexture.GetTexture(URL);

            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError(string.Format("[MapQuestAPI] Error: {0}", www.error));
            }
            else
            {
                Texture2D t = DownloadHandlerTexture.GetContent(www);

                data.imageToSet.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);
                data.imageToSet.color = Color.white;

                if (data.callback != null)
                {
                    data.callback.Invoke();
                }
            }
        }
    }
}