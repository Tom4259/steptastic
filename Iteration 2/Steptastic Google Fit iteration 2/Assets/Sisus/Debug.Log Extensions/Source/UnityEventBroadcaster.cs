using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Debugging
{
	[AddComponentMenu("")] // hide in the add component menu
	public class UnityEventBroadcaster : MonoBehaviour
	{
		private Action onGUI;
		private Action onUpdate;

		public void RegisterOnGUIEventReceiver(Action everyOnGUI)
		{
			onGUI = everyOnGUI;
		}

		public void RegisterUpdateEventReceiver(Action everyUpdate)
		{
			onUpdate = everyUpdate;
		}

		[UsedImplicitly]
		private void Awake()
		{
			gameObject.hideFlags = HideFlags.HideInHierarchy;
			DontDestroyOnLoad(gameObject);
		}

		[UsedImplicitly]
		private void OnGUI()
		{
			if(onGUI != null)
			{
				onGUI();
			}
		}

		[UsedImplicitly]
		private void LateUpdate()
		{
			if(onUpdate != null)
			{
				onUpdate();
			}
		}
	}
}