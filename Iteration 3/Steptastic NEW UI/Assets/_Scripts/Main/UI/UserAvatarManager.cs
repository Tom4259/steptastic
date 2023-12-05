using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserAvatarManager : MonoBehaviour
{
    public static UserAvatarManager instance;



    public Sprite[] avatars;


    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }


    public void SetUserSprite(int index)
    {
        PlayerPrefsX.SetInt(PlayerPrefsLocations.User.Details.avaterIndex, index);
    }

    public Sprite GetUserSprite()
    {
        return avatars[PlayerPrefsX.GetInt(PlayerPrefsLocations.User.Details.avaterIndex)];
    }
}
