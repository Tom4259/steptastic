using Michsky.MUIP;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NavigationBar : MonoBehaviour
{
    [Serializable]
    public class ButtonConfig
    {
        public ButtonManager buttonObject;
        public float width;
        public int windowTarget;
    }

    public RectTransform mainWindow;

    [Space]
    public Image windowPointer;
    public float animationTime = 0.3f;
    public float windowChangeMultiplier = 1.75f;

    [Space]
    public ButtonConfig[] buttons;
    public ButtonManager startingSelected;

    [Space]
    public Color selectedColour;
    public Color deselectedColour;


    private int currentWindowIndex = 0;
    private int minimumIndex = 0;
    private int maximumIndex = 1;


    private void Start()
    {
        SwipeManager.instance.onSwipeLeft += OnSwipeLeft;
        SwipeManager.instance.onSwipeRight += OnSwipeRight;
    }



    public void OpenWindow(ButtonManager btn)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].buttonObject == btn)
            {
                NavBarTransition(buttons[i]);

                ChangeWindow(buttons[i].windowTarget);
            }
            else
            {
                if (buttons[i].buttonObject.normalText.color != deselectedColour)
                {
                    FadeColours(buttons[i], selectedColour, deselectedColour);
                }
            }
        }
    }

    public void OpenWindow(int index)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].windowTarget == index)
            {
                NavBarTransition(buttons[i]);

                ChangeWindow(buttons[i].windowTarget);
            }
            else
            {
                if (buttons[i].buttonObject.normalText.color != deselectedColour)
                {
                    FadeColours(buttons[i], selectedColour, deselectedColour);
                }
            }
        }
    }

    public void OpenWindow(int index, UnityAction additionalAction)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].windowTarget == index)
            {
                NavBarTransition(buttons[i]);

                ChangeWindow(buttons[i].windowTarget);
            }
            else
            {
                if (buttons[i].buttonObject.normalText.color != deselectedColour)
                {
                    FadeColours(buttons[i], selectedColour, deselectedColour);
                }
            }
        }

        additionalAction.Invoke();
    }


    #region nav bar changes

    private void NavBarTransition(ButtonConfig config)
    {
        //setting window pointers size
        LeanTween.value(gameObject, (float f) =>
        {
            windowPointer.rectTransform.sizeDelta = new Vector2(f, windowPointer.rectTransform.sizeDelta.y);
        },
        windowPointer.rectTransform.rect.width, config.width, animationTime).setEaseInOutCubic();

        //setting window pointer position
        LeanTween.value(gameObject, (float f) =>
        {
            windowPointer.rectTransform.anchoredPosition = new Vector2(f, windowPointer.rectTransform.anchoredPosition.y);
        },
        windowPointer.rectTransform.anchoredPosition.x,
        config.buttonObject.GetComponent<RectTransform>().anchoredPosition.x,
        animationTime).setEaseInOutCubic(); ;


        //setting button text colour
        FadeColours(config, deselectedColour, selectedColour);
    }

    private void FadeColours(ButtonConfig config, Color start, Color end)
    {
        LeanTween.value(gameObject, (Color c) =>
        {
            config.buttonObject.normalText.color = c;
        }, start, end, animationTime).setEaseInOutCubic();
    }

    #endregion

    private void ChangeWindow(int windowIndex)
    {
        currentWindowIndex = windowIndex;

        float newPosition = (800 * -windowIndex);

        LeanTween.value(gameObject, (float f) =>
        {
            mainWindow.anchoredPosition = new Vector2(f, mainWindow.anchoredPosition.y);
        }, mainWindow.anchoredPosition.x, newPosition, animationTime * windowChangeMultiplier).setEaseInOutCubic();
    }



    public void OnSwipeLeft()
    {
        if(currentWindowIndex + 1 <= maximumIndex && CanvasManager.instance.isMainWindowOpen)
        {
            OpenWindow(currentWindowIndex + 1);
        }
    }

    public void OnSwipeRight()
    {
        if (currentWindowIndex - 1 >= minimumIndex && CanvasManager.instance.isMainWindowOpen)
        {
            OpenWindow(currentWindowIndex - 1);
        }
    }
}