using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class GoalsWindow : MonoBehaviour
{

	// Start is called before the first frame update
	public void LoadGoalsWindow()
    {
		GoalsOverWeek();
    }

	private async void GoalsOverWeek()
	{
		List<APIManager.HealthKit.QuantityData> d = await APIManager.HealthKit.GetStepsList(UsefulFunctions.StartOfWeek(), DateTime.Now);
		Debug.Log("got data " + d.Count);
	}
}
