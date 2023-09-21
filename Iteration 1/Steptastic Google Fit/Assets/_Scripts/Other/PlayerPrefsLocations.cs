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
            }

        }
    }
}