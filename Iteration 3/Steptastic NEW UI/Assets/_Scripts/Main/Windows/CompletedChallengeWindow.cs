using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompletedChallengeWindow : MonoBehaviour
{
	private RectTransform rect;


	private void Awake()
	{
		rect = GetComponent<RectTransform>();
	}



	public void OpenWindow(bool animation = true)
    {
		if (animation)
		{
			LeanTween.value(gameObject, (float f) =>
			{
				rect.anchoredPosition = new Vector2(f, 0);
			}, -800, 0, CanvasManager.fastWindowAnimationTime).setEaseInOutCubic();
		}
		else
		{
			rect.anchoredPosition = new Vector2(0, 0);
		}
	}

    public void CloseWindow(bool animation = true)
    {
		if (animation)
		{
			LeanTween.value(gameObject, (float f) =>
			{
				rect.anchoredPosition = new Vector2(f, 0);
			}, 0, -800, CanvasManager.fastWindowAnimationTime).setEaseInOutCubic();
		}
		else
		{
			rect.anchoredPosition = new Vector2(-800, 0);
		}
	}
}
