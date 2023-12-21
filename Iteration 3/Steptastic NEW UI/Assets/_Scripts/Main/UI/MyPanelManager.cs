using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MyPanelManager : MonoBehaviour
{

    public RectTransform[] panels;

    public Color currentPanelColour;
    public Color backgroundPanelColour;

    [HideInInspector]
    public int currentPanelIndex = 0;

    private int maxPanels;



    //puts all of the panels in the right order, makes them visible, and sets the correct position
    private void Start()
    {
        maxPanels = panels.Length - 1;

        //set first, second and third panel locations
        panels[0].anchoredPosition = new Vector2(0, 725);
        panels[0].GetComponent<Image>().color = currentPanelColour;
        panels[0].sizeDelta = new Vector2(panels[0].sizeDelta.x, 1450);
        panels[0].gameObject.SetActive(true);

        panels[1].anchoredPosition = new Vector2(0, 855);
        panels[1].GetComponent<Image>().color = backgroundPanelColour;
        panels[1].transform.localScale = new Vector3(0.88f, 0.88f, 0.88f);
        panels[1].gameObject.SetActive(true);

        for (int i = 2; i < panels.Length; i++)
        {
            panels[i].anchoredPosition = new Vector2(0, 775);
            panels[i].GetComponent<Image>().color = backgroundPanelColour;

            panels[i].transform.localScale = new Vector3(0.88f, 0.88f, 0.88f);
            panels[i].gameObject.SetActive(true);
        }
    }

    //called on button press, updates the current panel, next panel, and animates then animates the screen
    public void NextPanel()
    {
        if(currentPanelIndex < maxPanels)
        {
            try
            {
                AnimatePanels(panels[currentPanelIndex],
                panels[currentPanelIndex + 1],
                panels[currentPanelIndex + 2]);
            }
            catch (IndexOutOfRangeException)
            {
                try
                {
                    AnimatePanels(panels[currentPanelIndex],
                    panels[currentPanelIndex + 1],
                    null);
                }
                catch (IndexOutOfRangeException)
                {
                    AnimatePanels(panels[currentPanelIndex],
                    null,
                    null);
                }
            }

            currentPanelIndex++;
        }
        else
        {
            Debug.Log("No more panels to cycle through!");
        }        
    }


    //animates all of the visible panels
    private async void AnimatePanels(RectTransform currentPanel, RectTransform secondPanel, RectTransform thirdPanel)
    {
        //setting current panel position
        LeanTween.move(currentPanel,
            new Vector2(0, -currentPanel.anchoredPosition.y),
            1.1f).setEaseOutQuint();


        if(secondPanel != null)
        {
            await Task.Delay(150);

            //setting panel position
            LeanTween.value(gameObject, (float f) =>
            {
                secondPanel.anchoredPosition = new Vector2(0, f);
            },
            855,
            725f,
            0.75f).setEaseOutQuint();

            //setting panel colour
            LeanTween.value(gameObject, (Color c) =>
            {
                secondPanel.GetComponent<Image>().color = c;
            },
            backgroundPanelColour,
            currentPanelColour,
            0.75f).setEaseOutQuint();

            //setting panel size
            LeanTween.value(gameObject, (float f) =>
            {
                secondPanel.transform.localScale = new Vector3(f, f, f);
            }, 0.88f, 1, 0.75f).setEaseOutQuint();
        }


        if(thirdPanel != null)
        {
            await Task.Delay(450);

            //setting panel position
            LeanTween.value(gameObject, (float f) =>
            {
                thirdPanel.anchoredPosition = new Vector2(0, f);
            }, 775f, 855f, 0.8f).setEaseOutBack();
        }
    }
}