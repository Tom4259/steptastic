using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        canvasManager.SetupCompleted();
    }
}