using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SetupManager : MonoBehaviour
{
    private CanvasManager canvasManager;
    private MyPanelManager panelManager;

    public ProfilePanel profileSetup;
    public AuthenticatePanel authenticatePanel;
    public LocationRequestPanel locationRequestPanel;
    public ChallengeSetupPanel challengeSetupPanel;


    private void Start()
    {
        canvasManager = GetComponentInParent<CanvasManager>();
        panelManager = GetComponentInChildren<MyPanelManager>();
    }

    //opens the next panel for the user to complete
    public void NextPanel()
    {
        if(panelManager.currentPanelIndex != 3)
        {
            panelManager.NextPanel();
        }
        else
        {
            CompleteSetup();
        }        
    }

    public void CompleteSetup()
    {
        Debug.Log("[SetupManager] Completed setup");

        PlayerPrefsX.SetDateTime(PlayerPrefsLocations.User.Challenge.ChallengeData.startDate, DateTime.Today);

        canvasManager.SetupCompleted();
    }
}