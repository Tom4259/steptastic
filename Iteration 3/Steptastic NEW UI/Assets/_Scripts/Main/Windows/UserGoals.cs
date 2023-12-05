using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserGoals : MonoBehaviour
{

	public static void SetDailyStepGoal(int steps)
	{
        PlayerPrefsX.SetInt(PlayerPrefsLocations.User.Goals.dailyStepGoal, steps);
	}

	public static int GetDailyStepGoal()
	{
        return PlayerPrefsX.GetInt(PlayerPrefsLocations.User.Goals.dailyStepGoal, 10000);
	}


    public static void SetDailyDistanceGoal(float distance)
    {
        PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Goals.dailyDistanceGoal, distance);
    }

    public static float GetDailyDistanceGoal()
    {
        return PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Goals.dailyDistanceGoal, 8f);
    }


    public static void SetWeeklyStepGoal(int steps)
    {
        PlayerPrefsX.SetInt(PlayerPrefsLocations.User.Goals.weeklyStepGoal, steps);
    }

    public static int GetWeeklyStepGoal()
    {
        return PlayerPrefsX.GetInt(PlayerPrefsLocations.User.Goals.dailyStepGoal, 70000);
    }

    public static void SetWeeklyDistanceGoal(float distance)
    {
        PlayerPrefsX.SetFloat(PlayerPrefsLocations.User.Goals.weeklyDistanceGoal, distance);
    }

    public static float GetWeeklyDistanceGoal()
    {
        return PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Goals.dailyStepGoal, 56f);
    }
}