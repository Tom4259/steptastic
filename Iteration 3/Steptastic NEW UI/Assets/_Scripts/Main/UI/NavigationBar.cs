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
    private RectTransform rect;


    [Serializable]
    public class ButtonConfig
    {
        public Button buttonObject;
        public Image buttonImage;
        public int windowTarget;
    }

    public RectTransform mainWindow;

    [Space]
    public Image windowPointer;
    public float animationTime = 0.3f;
    public float windowChangeMultiplier = 1.75f;

    [Space]
    public ButtonConfig[] buttons;
    public Button startingSelected;

    [Space]
    public Color selectedColour;
    public Color deselectedColour;


    private int currentWindowIndex = 0;
    private int minimumIndex = 0;
    private int maximumIndex = 3;


    private Vector2 startPosition;


	private void Awake()
	{
        startPosition = GetComponent<RectTransform>().anchoredPosition;
        rect = GetComponent<RectTransform>();
	}

	private async void Start()
    {
        SwipeManager.instance.onSwipeLeft += OnSwipeLeft;
        SwipeManager.instance.onSwipeRight += OnSwipeRight;


        await System.Threading.Tasks.Task.Delay(100);
        startingSelected.onClick.Invoke();
    }



    public void OpenWindow(Button btn)
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
                if (buttons[i].buttonImage.color != deselectedColour)
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
                if (buttons[i].buttonImage.color != deselectedColour)
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
                if (buttons[i].buttonImage.color != deselectedColour)
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
            config.buttonImage.color = c;
        }, start, end, animationTime).setEaseInOutCubic();
    }

    public void ShowNavigationBar()
    {
        LeanTween.value(gameObject, (Vector2 p) =>
        {
            rect.anchoredPosition = p;
        }, new Vector2(0, -25), startPosition, animationTime);
    }

    public void HideNavigationBar()
    {
		LeanTween.value(gameObject, (Vector2 p) =>
		{
			rect.anchoredPosition = p;
		}, startPosition, new Vector2(0, -25), animationTime);
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