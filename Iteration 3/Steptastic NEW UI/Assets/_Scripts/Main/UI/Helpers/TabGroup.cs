using Michsky.MUIP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//a script to control a collection of buttons to work as a tab group
public class TabGroup : MonoBehaviour
{
    public int selectedTabIndex = 0;
    public ButtonManager[] buttons;


    public Image tabIdentifier;

    private void Start()
    {
        buttons = GetComponentsInChildren<ButtonManager>();

        //for (int i = 0; i < buttons.Length; i++)
        //{
        //    buttons[i].onClick.AddListener(() =>
        //    {
        //        ChangeTab(i-2);
        //    });
        //
        //    Debug.Log(i + " for " + buttons[i].gameObject.name);
        //}
    }

    public void ChangeTab(int tab)
    {
        Debug.Log(() => tab);

        buttons[selectedTabIndex].highlightCG.alpha = 0;
        buttons[selectedTabIndex].Interactable(true);

        selectedTabIndex = tab;
    }


    private void LateUpdate()
    {
        buttons[selectedTabIndex].highlightCG.alpha = 1;
        buttons[selectedTabIndex].Interactable(false);
    }
}
