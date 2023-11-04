using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SwipeManager : MonoBehaviour
{
    public static SwipeManager instance;



    public enum paddingOptions
    {
        Inside,
        Outside,
        Ignored
    }

    private Vector2 startPosition;
    private Vector2 endPosition;

    public float swipeThreshhold = 20f;

    public paddingOptions padding;
    public float paddingDistance;


    public UnityAction onSwipeLeft;
    public UnityAction onSwipeRight;


    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }


    private void Update()
    {
        if(Input.touchCount > 0)
        {
            if(Input.GetTouch(0).phase == TouchPhase.Began)
            {
                startPosition = Input.GetTouch(0).position;
            }
            else if(Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                endPosition = Input.GetTouch(0).position;
                

                if ((DistanceOfSwipe(startPosition.x, endPosition.x) >= swipeThreshhold) && ValidSwipe(startPosition.x))
                {
                    if (endPosition.x < startPosition.x)
                    {
                        SwipeLeft();
                    }
                    else if (endPosition.x > startPosition.x)
                    {
                        SwipeRight();
                    }
                }
            }            
        }
    }

    private float DistanceOfSwipe(float start, float end)
    {
        return (end - start) > 0 ? (end - start) : ((end - start) * -1);
    }

    private bool ValidSwipe(float start)
    {
        if(padding == paddingOptions.Ignored)
        {
            return true;
        }
        else if(padding == paddingOptions.Inside)
        {
            if(Screen.currentResolution.width - paddingDistance < start)
            {
                return true;
            }

            if(start < paddingDistance)
            {
                return true;
            }
        }
        else if(padding == paddingOptions.Outside)
        {
            if (Screen.currentResolution.width - paddingDistance > start)
            {
                return true;
            }

            if (start > paddingDistance)
            {
                return true;
            }
        }

        return false;
    }



    private void SwipeLeft()
    {
        onSwipeLeft.Invoke();
    }

    private void SwipeRight()
    {
        onSwipeRight.Invoke();
    }
}