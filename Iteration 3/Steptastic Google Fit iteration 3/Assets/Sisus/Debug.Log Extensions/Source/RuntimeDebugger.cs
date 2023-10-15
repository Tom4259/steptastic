using System;
using UnityEngine;

namespace Sisus.Debugging
{
	public static class RuntimeDebugger
	{
		/// <summary>
		/// Called whenever the GUI is opened due to user input.
		/// </summary>
		public static Action onGUIOpened;

		/// <summary>
		/// Called whenever the GUI is closed due to user input.
		/// </summary>
		public static Action onGUIClosed;

		/// <summary>
		/// Returns value determining whether or not RuntimeDebugger is currently active.
		/// </summary>
		public static bool Enabled
		{
			get;
			private set;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		private const float displayViewModeDuration = 1f;
		private static readonly GUIContent[] ViewLabels = new GUIContent[] { new GUIContent("Visualized Values"), new GUIContent("Channels") }; // TO DO: Make Dynamic so users can extend + add Tracked Values
		private static int selectedView = 0;
		private const int lastViewIndex = 1;
		private static bool? forceShowView = null;
		private static float displayViewModeUntil;

		private static GUIStyle onGUITextStyle;
		private static UnityEventBroadcaster onGUIHelper;
		private static KeyConfig toggleView;
		#endif

		#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
		[System.Diagnostics.Conditional("FALSE")]
		#endif
		public static void Initialize()
		{
			#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG

			#if !DEBUG
			var args = Environment.GetCommandLineArgs();

			bool enabledViaCommandLineArgument = false;
			for(int i = 0; i < args.Length; i++)
			{
				if(string.Equals(args[i], "-log-gui-enable", StringComparison.OrdinalIgnoreCase))
				{
					enabledViaCommandLineArgument = true;
					break;
				}
			}

			if(!enabledViaCommandLineArgument)
			{
				Enabled = false;
				return;
			}
			#endif

			Enabled = true;

			#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				return;
			}
			#endif

			if(onGUIHelper != null)
            {
				return;
            }

			toggleView = new KeyConfig(KeyCode.Insert, false, false, false);

			onGUIHelper = new GameObject("DebugOnGUIHelper").AddComponent<UnityEventBroadcaster>();
			onGUIHelper.RegisterOnGUIEventReceiver(OnGUI);
			onGUIHelper.RegisterUpdateEventReceiver(Debug.Update);
			#endif
		}

		#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
		[System.Diagnostics.Conditional("FALSE")]
		#endif
		public static void OpenGUI()
		{
			#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
			forceShowView = true;
			if(onGUIOpened != null)
			{
				onGUIOpened();
			}

			displayViewModeUntil = Time.realtimeSinceStartup + displayViewModeDuration;
			#endif
		}

		#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
		[System.Diagnostics.Conditional("FALSE")]
		#endif
		public static void CloseGUI()
		{
			#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
			forceShowView = false;
			if(onGUIClosed != null)
			{
				onGUIClosed();
			}

			displayViewModeUntil = Time.realtimeSinceStartup + displayViewModeDuration;
			#endif
		}

		#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
		[System.Diagnostics.Conditional("FALSE")]
		#endif
		public static void SelectPreviousGUIView()
		{
			#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
			if(selectedView == 0)
			{
				selectedView = lastViewIndex;
			}
			else
			{
				selectedView--;
			}

			displayViewModeUntil = Time.realtimeSinceStartup + displayViewModeDuration;
			#endif
		}

		#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
		[System.Diagnostics.Conditional("FALSE")]
		#endif
		public static void SelectNextGUIView()
		{
			#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
			if(selectedView == lastViewIndex)
			{
				selectedView = 0;
			}
			else
			{
				selectedView++;
			}

			displayViewModeUntil = Time.realtimeSinceStartup + displayViewModeDuration;
			#endif
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		private static void OnGUI()
		{
			if(toggleView.DetectInput(Event.current))
			{
				if(!forceShowView.HasValue)
				{
					selectedView = 0;
					OpenGUI();
				}
				else if(forceShowView.Value == false)
				{
					forceShowView = null;
					selectedView = 0;
				}
				else if(selectedView == lastViewIndex)
				{
					selectedView = 0;
					CloseGUI();
				}
				else
				{
					selectedView++;
				}

				displayViewModeUntil = Time.realtimeSinceStartup + displayViewModeDuration;

				GUIUtility.ExitGUI();
			}
			
			if(displayViewModeUntil > Time.realtimeSinceStartup)
			{
				string label;
				if(forceShowView.HasValue)
				{
					if(forceShowView.Value)
					{
						label = "View "+ (selectedView + 1) + " / "+(lastViewIndex + 1);
					}
					else
					{
						label = "Always Hidden";
					}
				}
				else
				{
					label = "Dynamic";
				}
				GUI.Label(new Rect(20f, 0f, 100f, 20f), label);
			}

			bool showView;
			if(forceShowView.HasValue)
			{
				showView = forceShowView.Value;
			}
			else
			{
				showView = Debug.DisplayedOnScreen.Count > 0;
			}

			if(!showView)
			{
				return;
			}

			if(onGUITextStyle == null)
			{
				onGUITextStyle = new GUIStyle(GUI.skin.label);
				onGUITextStyle.normal.textColor = Color.white;
				onGUITextStyle.richText = true;
			}

			GUILayout.Space(20f);
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(20f);
				GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(500f));
				{
					if(forceShowView.HasValue)
					{
						GUILayout.Label(" ", GUI.skin.box, GUILayout.Height(20f));

						GUILayout.Space(-25f);

						GUILayout.BeginHorizontal();
						{
							GUILayout.Space(5f);
							if(selectedView > 0)
							{
								if(GUILayout.Button("←", GUI.skin.label, GUILayout.Width(18f)))
								{
									selectedView = Mathf.Max(0, selectedView - 1);
									forceShowView = true;
								}
							}
							else
							{
								GUILayout.Space(18f);
							}
							GUILayout.Space(5f);
							GUILayout.Label(ViewLabels[selectedView]);
							GUILayout.Space(5f);
							if(selectedView < lastViewIndex)
							{
								if(GUILayout.Button("→", GUI.skin.label, GUILayout.Width(18f)))
								{
									selectedView = Mathf.Min(lastViewIndex, selectedView + 1);
									forceShowView = true;
								}
							}
							else
							{
								GUILayout.Space(18f);
							}
							GUILayout.Space(5f);
						}
						GUILayout.EndHorizontal();
					}

					switch(forceShowView.HasValue ? selectedView : 0)
					{
						case 0:
							DrawDisplayedValues();
							break;
						case 1:
							DrawChannels();
							break;
					}
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}

		private static void DrawDisplayedValues()
		{
			var displayedOnScreen = Debug.DisplayedOnScreen;
			foreach(var displayed in displayedOnScreen)
			{
				displayed.Draw(onGUITextStyle);
			}
		}

		private static void DrawChannels()
		{
			var guiColorWas = GUI.color;

			Debug.channels.AllChannelsEnabledByDefault = GUILayout.Toggle(Debug.channels.AllChannelsEnabledByDefault, "All Enabled By Default");

			GUILayout.Space(5f);

			foreach(var channel in Debug.channels)
			{
				GUILayout.BeginHorizontal();
				{
					Color color;
					if(ColorUtility.TryParseHtmlString(Debug.channels.GetChannelColor(channel), out color))
					{
						GUI.color = color;
					}
					bool enabled = !Debug.channels.IsEnabled(channel);
					bool setEnabled = GUILayout.Toggle(enabled, channel);
					if(enabled != setEnabled)
					{
						Debug.channels.SetChannelEnabled(channel, setEnabled);
					}
				}
				GUILayout.EndHorizontal();
			}

			GUI.color = guiColorWas;
		}
		#endif
	}
}