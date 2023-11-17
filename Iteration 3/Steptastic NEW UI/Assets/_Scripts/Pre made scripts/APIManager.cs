using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using LitJson;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

#if UNITY_IOS || UNITY_EDITOR
using BeliefEngine.HealthKit;
#endif

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

			Debug.Log("[GoogleFitAPI]", () => apiData.startTimeMillis);
			Debug.Log("[GoogleFitAPI]", () => apiData.endTimeMillis);
			Debug.Log("[GoogleFitAPI]", () => apiData.durationMillis);

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

#if UNITY_IOS || UNITY_EDITOR

	public class HealthKit
	{
		static HealthKitService HK = HealthKitService.Instance;



		private static SemaphoreSlim HKFree = new SemaphoreSlim(1, 1);



		public class Authorisation
		{
			private static bool authorisationWindowCompleted = false;


			public static async Task<bool> Authorise()
			{
				authorisationWindowCompleted = false;

				//initialise healthkit here
				Debug.Log("[HealthKitAPI] Authorising HealthKit");

				HK.healthStore.Authorize(HK.dataTypes, delegate (bool success)
				{
					Debug.LogFormat("[HealthKitAPI] HealthKit authorisation: {0}", success);

					authorisationWindowCompleted = true;
				});


				while (!authorisationWindowCompleted)
				{
					//Debug.Log("[HealthKitAPI] Waiting for authorisation to finish...");

					await Task.Delay(250);
				}

				return true;
			}



			//makes a sample request to check if the correct permissions were set
			// returns true if the user is authenticated
			public static async Task<bool> CheckPermissions()
			{
				double stepsOver3Months = await GetSteps(DateTime.Today.AddMonths(-3), DateTime.Now);

				double distanceOver3Months = await GetDistance(DateTime.Today.AddMonths(-3), DateTime.Now);


				return !(stepsOver3Months == 0 || distanceOver3Months == 0);
			} 
		}


		public class QuantityData
		{
			public DateTime startDate;
			public DateTime endDate;
			public double value;
		}

		public class OrderedQuantityData
		{
			public DateTime timeOfData;
			public double value;
		}


		#region getting data

		#region values


		public static async Task<double> GetSteps(DateTime startPoint, DateTime endPoint)
		{
			await HKFree.WaitAsync();


			double totalSteps = 0;
			bool done = false;

			try
			{
                HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierStepCount, startPoint, endPoint, delegate (List<QuantitySample> samplesW, Error e)
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
                        Debug.LogWarning("[HealthKitAPI] samples count is " + samplesW.Count + " Start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));

                        done = true;
                    }
                });

                while (!done)
                {
                    await Task.Delay(100);
                }
            }
			finally
			{
                HKFree.Release();
            }
			

			return totalSteps;
		}


		public static async Task<double> GetDistance(DateTimeOffset startPoint, DateTimeOffset endPoint)
		{
			await HKFree.WaitAsync();


			double totalDistance = 0;
			bool done = false;

			try
			{
                HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierDistanceWalkingRunning, startPoint, endPoint, delegate (List<QuantitySample> samplesW, Error e)
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
                        Debug.LogWarning("[HealthKitAPI] samples count is " + samplesW.Count + " Start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));

                        done = true;
                    }
                });


                while (!done)
                {
                    await Task.Delay(100);
                }
            }
			finally
			{
                HKFree.Release();
            }
			

			return totalDistance;
		}

		#endregion

		#region lists

		public static async Task<List<QuantityData>> GetStepsList(DateTime startPoint, DateTime endPoint)
		{
			await HKFree.WaitAsync();


			List<QuantityData> stepsList = new List<QuantityData>();
			bool done = false;

			try
			{
                HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierStepCount, startPoint, endPoint, delegate (List<QuantitySample> samplesW, Error e)
                {
                    if (samplesW.Count > 0)
                    {
                        foreach (QuantitySample sample in samplesW)
                        {
                            //Debug.Log(String.Format("[HealthKitAPI] {0} from {1} to {2}", sample.quantity.doubleValue, sample.startDate, sample.endDate));

                            QuantityData item = new QuantityData()
                            {
                                startDate = sample.startDate.DateTime,
                                endDate = sample.endDate.DateTime,
                                value = sample.quantity.doubleValue
                            };

                            stepsList.Add(item);
                        }

                        done = true;
                    }
                    else
                    {
                        Debug.LogWarning("[HealthKitAPI] Samples count is " + samplesW.Count + " start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));

                        done = true;
                    }
                });

                while (!done)
                {
                    await Task.Delay(100);
                }
            }
			finally
			{
                HKFree.Release();
            }
			

			return stepsList;
		}


		public static async Task<List<QuantityData>> GetDistanceList(DateTime startPoint, DateTime endPoint)
		{
			await HKFree.WaitAsync();


			List<QuantityData> distanceList = new List<QuantityData>();
			bool done = false;

			try
			{
                HK.healthStore.ReadQuantitySamples(HKDataType.HKQuantityTypeIdentifierDistanceWalkingRunning, startPoint, endPoint, delegate (List<QuantitySample> samplesW, Error e)
                {
                    if (samplesW.Count > 0)
                    {
                        foreach (QuantitySample sample in samplesW)
                        {
                            //Debug.Log(String.Format("[HealthKitAPI] {0} from {1} to {2}", sample.quantity.doubleValue, sample.startDate, sample.endDate));

                            QuantityData item = new QuantityData()
                            {
                                startDate = sample.startDate.DateTime,
                                endDate = sample.endDate.DateTime,
                                value = sample.quantity.doubleValue
                            };

                            distanceList.Add(item);
                        }

                        done = true;
                    }
                    else
                    {
                        Debug.LogWarning("[HealthKitAPI] samples count is " + samplesW.Count + " Start date is " + startPoint.ToString("G") + " end date is " + endPoint.ToString("G"));

                        done = true;
                    }
                });

                while (!done)
                {
                    await Task.Delay(250);
                }
            }
			finally
			{
                HKFree.Release();
            }
			

			return distanceList;
		}

		#endregion

		#endregion


		/// <summary>
		/// Orders a list of quantity data in hours in the day
		/// </summary>
		/// <param name="unorderedData"></param>
		/// <returns></returns>		
		public static List<OrderedQuantityData> OrderQuantityListHour(List<QuantityData> unorderedData)
		{
			List<OrderedQuantityData> sortedData = new List<OrderedQuantityData>();


			for (int i = 0; i < unorderedData.Count; i++)
			{
				DateTime average = UsefulFunctions.AverageDateBetweenDateTimes(new List<DateTime> { unorderedData[i].startDate, unorderedData[i].endDate });

				DateTime itemTime = new DateTime(average.Year, average.Month, average.Day, average.Hour, average.Minute, 0);



				OrderedQuantityData item = new OrderedQuantityData
				{
					timeOfData = itemTime,
					value = unorderedData[i].value
				};

				sortedData.Add(item);                
			}

			sortedData.Sort((x, y) => DateTime.Compare(x.timeOfData, y.timeOfData));
			


			List<OrderedQuantityData> cleanedData = new List<OrderedQuantityData>();


			//Debug.Log("[HealthKitAPI]", () => sortedData.Count);


			for (int i = 0; i < sortedData.Count; i++)
			{

				OrderedQuantityData newItem = new OrderedQuantityData
				{
					timeOfData = sortedData[i].timeOfData,
					value = 0
				};

				for (int z = 0; z < sortedData.Count; z++)
				{

					if (sortedData[i].timeOfData.Hour == sortedData[z].timeOfData.Hour)
					{
						newItem.value += sortedData[z].value;
					}
				}

				cleanedData.Add(newItem);
			}

			cleanedData = cleanedData
				.GroupBy(item => item.timeOfData.Hour)
				.Select(group => group.First())
				.ToList();


			return cleanedData;
		}


		/// <summary>
		/// Orders a list of quantity data in days. THIS DOESNT WORK FOR LISTS OF DATA OVER A THE MOUNTHS DAY COUNT
		/// </summary>
		/// <param name="unorderedData"></param>
		/// <returns></returns>		
		public static List<OrderedQuantityData> OrderQuantityListDay(List<QuantityData> unorderedData)
		{
			List<OrderedQuantityData> sortedData = new List<OrderedQuantityData>();


			for (int i = 0; i < unorderedData.Count; i++)
			{
				DateTime average = UsefulFunctions.AverageDateBetweenDateTimes(new List<DateTime> { unorderedData[i].startDate, unorderedData[i].endDate });

				DateTime itemTime = new DateTime(average.Year, average.Month, average.Day, average.Hour, average.Minute, 0);



				OrderedQuantityData item = new OrderedQuantityData
				{
					timeOfData = itemTime,
					value = unorderedData[i].value
				};

				sortedData.Add(item);
			}

			sortedData.Sort((x, y) => DateTime.Compare(x.timeOfData, y.timeOfData));


			List<OrderedQuantityData> cleanedData = new List<OrderedQuantityData>();


			Debug.Log("[HealthKitAPI]", () => sortedData.Count);


			for (int i = 0; i < sortedData.Count; i++)
			{

				OrderedQuantityData newItem = new OrderedQuantityData
				{
					timeOfData = sortedData[i].timeOfData,
					value = 0
				};

				for (int z = 0; z < sortedData.Count; z++)
				{
					if (sortedData[i].timeOfData.Day == sortedData[z].timeOfData.Day)
					{
						newItem.value += sortedData[z].value;
					}
				}

				cleanedData.Add(newItem);
			}

			cleanedData = cleanedData
				.GroupBy(item => item.timeOfData.Day)
				.Select(group => group.First())
				.ToList();


			return cleanedData;
		}
	}

#endif

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
		}

		/// <summary>
		/// Downloads image from GF using specified data passed into the method
		/// </summary>
		public static async Task<Sprite> GetMapImage(MapData data)
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

				return null;
			}
			else
			{
				Texture2D t = DownloadHandlerTexture.GetContent(www);

				Sprite s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);

				return s;
			}
		}
	}
}