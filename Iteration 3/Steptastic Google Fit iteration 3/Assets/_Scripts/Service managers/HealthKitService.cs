using BeliefEngine.HealthKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthKitService : MonoBehaviour
{
    public static HealthKitService Instance;

    [HideInInspector]
    public HealthStore healthStore;

    [HideInInspector]
    public HealthKitDataTypes dataTypes;

    private void Awake()
    {
        if(Application.platform != RuntimePlatform.IPhonePlayer)
        {
            Debug.Log("[HealthKitService] ", "Not running on iOS platform, disabling Health Kit service");

            gameObject.SetActive(false);

            return;
        }

        if(Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }


        healthStore = GetComponent<HealthStore>();
        dataTypes = GetComponent<HealthKitDataTypes>();        
    }
}
