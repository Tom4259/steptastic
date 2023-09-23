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

        public class Challenge
        {
            public static string startLocation = "user.challenge.startlocation";

            public static string endLocation = "user.challenge.endlocation";

            public static string startDate = "user.challenge.startdate";
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