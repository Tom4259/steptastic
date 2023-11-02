using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Debugging
{
    public sealed class DebugLogExtensionsProjectSettings
	{
		// Note: Don't remove this section!
		// It is utilized by DebugLogExtensionsProjectSettingsProvider via reflection.
		#pragma warning disable 0414
		[UsedImplicitly]
		public static readonly bool LogEnabledInBuilds =
		#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED
			false;
		#else
			true;
		#endif
		#pragma warning restore 0414

		private const string ResourcePath = "DebugLogExtensionsProjectSettings";
		private static DebugLogExtensionsProjectSettings instance;

		public bool useGlobalNamespace = true;
		public bool stripAllCallsFromBuilds = false;
		public bool unlistedChannelsEnabledByDefault = true;
		public DebugChannelInfo[] channels = new DebugChannelInfo[0];
		public KeyConfig toggleView = new KeyConfig(KeyCode.Insert, false, false, false);
		public IgnoredStackTraceInfo[] hideStackTraceRows = new IgnoredStackTraceInfo[0];

		public bool useGlobalNamespaceDetermined = false;
		public bool ignoreUnlistedChannelPrefixes = false;
		public bool autoAddDevUniqueChannels = true;

		[NotNull]
		public static DebugLogExtensionsProjectSettings Get()
		{
			if(instance == null)
			{
				instance = new DebugLogExtensionsProjectSettings();
				var settingsAsset = (IDebugLogExtensionsProjectSettingsAsset)Resources.Load<ScriptableObject>(ResourcePath);

				#if DEV_MODE
				Debug.Log("DebugLogExtensionsProjectSettings.Get - created new instance and loaded IDebugLogExtensionsProjectSettingsAsset", settingsAsset);
				#endif

				if(settingsAsset != null)
				{
					settingsAsset.Apply(instance);
				}
				#if DEV_MODE && UNITY_EDITOR
				else { Debug.LogWarning(ResourcePath + " not found. EditorApplication.isUpdating=" + UnityEditor.EditorApplication.isUpdating); }
				#endif
			}
			return instance;
		}

		public void Apply()
		{
			const int UseProjectSettings = 0;
			const int AllEnabledByDefault = 1;
			const int AllDisabledByDefault = 2;
			switch(PlayerPrefs.GetInt("DebugLogExtensions.DeterminingEnabledChannels", 0))
			{
				case UseProjectSettings:
					Debug.channels.AllChannelsEnabledByDefault = unlistedChannelsEnabledByDefault;
					break;
				case AllEnabledByDefault:
					Debug.channels.AllChannelsEnabledByDefault = true;
					break;
				case AllDisabledByDefault:
					Debug.channels.AllChannelsEnabledByDefault = false;
					break;
			}

			// Allow developers to prefix log messages with their username to have it not
			// be shown for other developers when allChannelsEnabledByDefault is false.
			if(UnityEngine.Debug.isDebugBuild || RuntimeDebugger.Enabled)
			{
				var uniqueChannel = Dev.PersonalChannelName;
				Debug.channels.SetChannelColor(uniqueChannel, Color.grey);
				Debug.channels.EnableChannel(uniqueChannel);
			}

			// Apply channel enabledness based on project settings
			for(int n = 0, count = channels.Length; n < count; n++)
			{
				var channel = channels[n];
				var id = channel.id.Trim();

				// skip empty entries
				if(id.Length == 0)
                {
					continue;
                }

				Debug.channels.SetChannelColor(id, channel.color);
				Debug.channels.SetChannelEnabled(id, channel.enabledByDefault);
			}

			Debug.channels.IgnoreUnlistedChannels = ignoreUnlistedChannelPrefixes;
		}
	}
}