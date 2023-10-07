#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Sisus.Debugging
{
	[InitializeOnLoad]
	internal static class EditorDebugger
	{
		private static GUIStyle onGUITextStyle;

		static EditorDebugger()
		{
			#if !UNITY_2018_4
			SceneView.duringSceneGui += DrawTrackedValuesInSceneView;
			#endif

			if(!Application.isPlaying)
			{
				EditorApplication.update += Debug.Update;
			}
		}

		private static void DrawTrackedValuesInSceneView(SceneView sceneView)
		{
			if(onGUITextStyle == null)
			{
				onGUITextStyle = new GUIStyle(GUI.skin.label);
				onGUITextStyle.normal.textColor = Color.black;
				onGUITextStyle.richText = true;
			}

			var displayedOnScreen = Debug.DisplayedOnScreen;
			int count = displayedOnScreen.Count;
			if(count == 0)
			{
				return;
			}

			Handles.BeginGUI();
			GUILayout.BeginHorizontal();
			GUILayout.Space(20f);
			GUILayout.BeginVertical(EditorStyles.helpBox);
		
			foreach(var displayed in displayedOnScreen)
			{
				displayed.Draw(onGUITextStyle);
			}

			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			bool repaintSceneView = false;

			foreach(var displayed in displayedOnScreen)
			{
				if(displayed.WantsRepaint)
				{
					displayed.WantsRepaint = false;
					repaintSceneView = true;
				}
			}

			if(repaintSceneView)
			{
				sceneView.Repaint();
			}

			Handles.EndGUI();
		}
	}
}
#endif