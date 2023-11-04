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


            //only used in Android build
            public class Credentials
            {
                /// <summary>
                /// Data type: String
                /// The auth code to get data from the user
                /// </summary>
                public static string authorizationCode = "user.account.authorizationcode";

                /// <summary>
                /// Data type: String
                /// The token to access users data
                /// </summary>
                public static string accessToken = "user.account.codes.accesstoken";


                /// <summary>
                /// Data type: String
                /// A refresh  token for the accessToken
                /// </summary>
                public static string refreshToken = "user.account.codes.refreshtoken";


                /// <summary>
                /// Data type: DateTime
                /// When the access token expires
                /// </summary>
                public static string expiresIn = "user.account.codes.expiresin";
            }
        }


        //only used in Android build
        public class Permissions
        {
            /// <summary>
            /// Data type: Bool
            /// Returns true if the user has allowed access to testUserGetLocation services
            /// </summary>
            public static string Location = "user.permisssions.location";
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

                /// <summary>
                /// Data type: float
                /// </summary>
                public static string setupLatitude = "user.challenge.userdata.setuplatitude";


                /// <summary>
                /// Data type: float
                /// </summary>
                public static string setupLongitude = "user.challenge.userdata.setuplongitude";
            }
        }

        public class CompletedWindows
        {
            //all of these are of data type Bool

            /// <summary>
            /// takes the user straight to the main screen if it returns true
            /// </summary>
            public static string setup = "user.completedwindows.setup";


            public static string setGoals = "user.completedwindows.setgoals";
        }

        public class Goals
        {
            public static string dailyStepGoal = "user.goals.dailystepgoal";

            public static string weeklyStepGoal = "user.goals.weeklystepgoal";


            public static string dailyDistanceGoal = "user.goals.dailydistamcegoal";

            public static string weeklyDistanceGoal = "user.goals.weeklydistancegoal";
        }
    }

    public class Developer
    {
        public class Keys
        {
            public static string clientID = "developer.keys.clientid";
            public static string clientSecret = "developer.keys.clientsecret";
        }

        public static string developerControls = "developer.developercontrols";
    }
}