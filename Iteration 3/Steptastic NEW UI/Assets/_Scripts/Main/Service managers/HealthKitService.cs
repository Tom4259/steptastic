#if UNITY_IOS || UNITY_EDITOR
using BeliefEngine.HealthKit;
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthKitService : MonoBehaviour
{
    public static HealthKitService Instance;
#if UNITY_IOS || UNITY_EDITOR
    [HideInInspector]
    public HealthStore healthStore;

    [HideInInspector]
    public HealthKitDataTypes dataTypes;
#endif

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

#if UNITY_IOS || UNITY_EDITOR
        //sets the local variables to components on the gameobject
        healthStore = GetComponent<HealthStore>();
        dataTypes = GetComponent<HealthKitDataTypes>();
#endif
    }
}