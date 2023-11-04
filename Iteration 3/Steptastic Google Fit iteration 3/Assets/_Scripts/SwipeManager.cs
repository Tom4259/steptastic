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
    public UnityAction onSwipeDown;
    public UnityAction onSwipeUp;


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
                

                if(DistanceOfSwipe(startPosition.x, endPosition.x) > DistanceOfSwipe(startPosition.y, endPosition.y))
                {
                    if ((DistanceOfSwipe(startPosition.x, endPosition.x) >= swipeThreshhold))
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
                else
                {
                    if ((DistanceOfSwipe(startPosition.y, endPosition.y) >= swipeThreshhold))
                    {
                        if (endPosition.y < startPosition.y)
                        {
                            SwipeDown();
                        }
                        else if (endPosition.y > startPosition.y)
                        {
                            SwipeUp();
                        }
                    }
                }                
            }            
        }
    }

    private float DistanceOfSwipe(float start, float end)
    {
        return (end - start) > 0 ? (end - start) : ((end - start) * -1);
    }


    private void SwipeLeft()
    {
        onSwipeLeft?.Invoke();
    }

    private void SwipeRight()
    {
        onSwipeRight?.Invoke();
    }

    private void SwipeDown()
    {
        onSwipeDown?.Invoke();
    }

    private void SwipeUp()
    {
        onSwipeUp?.Invoke();
    }
}