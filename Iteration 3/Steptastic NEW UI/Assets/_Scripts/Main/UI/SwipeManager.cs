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
        //checking for a touch input
        if(Input.touchCount > 0)
        {
            if(Input.GetTouch(0).phase == TouchPhase.Began)
            {
                startPosition = Input.GetTouch(0).position;
            }
            else if(Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                endPosition = Input.GetTouch(0).position;
                
                //checking the distance of the swipe to make usre it wasn't accidental
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

    //calculates the distance of the swipe
    private float DistanceOfSwipe(float start, float end)
    {
        return (end - start) > 0 ? (end - start) : ((end - start) * -1);
    }


    private void SwipeLeft()
    {
        //Debug.Log("[SwipeManager] Swiped left");

        onSwipeLeft?.Invoke();
    }

    private void SwipeRight()
    {
		//Debug.Log("[SwipeManager] Swiped right");

		onSwipeRight?.Invoke();
    }

    private void SwipeDown()
    {
		//Debug.Log("[SwipeManager] Swiped down");

		onSwipeDown?.Invoke();
    }

    private void SwipeUp()
    {
		//Debug.Log("[SwipeManager] Swiped up");

		onSwipeUp?.Invoke();
    }
}