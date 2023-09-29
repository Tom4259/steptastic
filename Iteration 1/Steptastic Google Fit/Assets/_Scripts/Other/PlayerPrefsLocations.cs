using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrefsLocations : MonoBehaviour
{
    /// <summary>
    /// This script is for the file locations of data i have saved to the users device
    /// </summary>
    public class User
    {
        public class Account
        {
            /// <summary>
            /// Data type: Bool
            /// If the user has logged in, returns true
            /// </summary>
            public static string authenticated = "user.account.authenticated";

            public class Credentials
            {
                /// <summary>
                /// Data type: String
                /// The auth code to get data from the user
                /// </summary>
                public static string authorizationCode = "user.account.authorizationCode";

                /// <summary>
                /// Data type: String
                /// The token to access users data
                /// </summary>
                public static string accessToken = "user.account.codes.accessToken";


                /// <summary>
                /// Data type: String
                /// A refresh  token for the accessToken
                /// </summary>
                public static string refreshToken = "user.account.codes.refreshToken";


                /// <summary>
                /// Data type: DateTime
                /// When the access token expires
                /// </summary>
                public static string expiresIn = "user.account.codes.expiresin";
            }

        }

        public class Permissions
        {
            /// <summary>
            /// Data type: Bool
            /// Returns true if the user has allowed access to testUserGetLocation services
            /// </summary>
            public static string location = "user.permisssions.location";
        }

        public class Challenge
        {
            public class ChallengeData
            {
                public static string startDate = "user.challenge.userdata.startdate";

                public static string startLocationName = "user.challenge.userdata.startlocationname";
                public static string startLocationCapital = "user.challenge.userdata.startlocationcapital";
                public static string startLocationLatLong = "user.challenge.userdata.startlocationlatlong";//seperated by just a ','

                public static string endLocationName = "user.challenge.userdata.endlocationname";
                public static string endLocationCapital = "user.challenge.userdata.endlocationcapital";
                public static string endLocationLatLong = "user.challenge.userdata.endlocationlatlong";//seperated by just a ','

                public static string totalDistanceToTarget = "user.challenge.totaldistancetotarget";// in KM
            }

            public class UserData
            {
                public static string currentLatLong = "user.challenge.userdata.currentlatlong";
                public static string percentCompleted = "user.challenge.userdata.percentcompleted";
            }
        }

        public class CompletedWindows
        {
            //all of these are of data type Bool

            /// <summary>
            /// takes the user straight to the main screen if it returns true
            /// </summary>
            public static string mainScreen = "user.completedwindows.all";

            public static string loggedIn = "user.completedwindows.loggedin";

            public static string createdChallenge = "user.completedwindows.createdchallenge";

            public static string requestedUserLocation = "user.completedwindows.requesteduserlocation";
        }
    }

    public class Developer
    {
        public class Keys
        {
            public static string clientID = "developer.keys.clientid";
            public static string clientSecret = "developer.keys.clientsecret";
        }
    }
}