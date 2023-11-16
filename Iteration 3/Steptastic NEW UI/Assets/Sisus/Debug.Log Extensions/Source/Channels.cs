using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using System.Text;

namespace Sisus.Debugging
{
	/// <summary>
    /// Represents a collection of <see cref="ChannelData"/> and offers a number of methods and properties to get information about the channels or modify them.
    /// </summary>
	[Serializable]
	public class Channels : IReadOnlyCollection<string>, ISerializationCallbackReceiver
	{
		public event Action<Channels> OnEnabledChannelsChanged;

		[NonSerialized]
		private readonly SortedDictionary<string, ChannelData> channels = new SortedDictionary<string, ChannelData>();

		[SerializeField]
		private bool allChannelsEnabledByDefault = true;

		[SerializeField]
		private bool messagesWithNoChannelsEnabled = true;

		[SerializeField]
		private bool ignoreUnlistedChannels = true;

		[SerializeField, HideInInspector]
		private string[] colorOptions = new string[] { "green", "red", "orange", "brown", "cyan", "teal", "silver", "darkblue", "maroon", "olive", "purple", "lightblue", "blue" };

		[SerializeField, HideInInspector]
		private int nextColorIndex = 0;

		[SerializeField, HideInInspector]
		private ChannelSerializedData[] channelsDataSerialized = null;
	
		public bool AllChannelsEnabledByDefault
		{
			get
			{
				return allChannelsEnabledByDefault;
			}

			set
			{
				if(allChannelsEnabledByDefault == value)
				{
					return;
				}
				
				allChannelsEnabledByDefault = value;

				if(OnEnabledChannelsChanged != null)
				{
					OnEnabledChannelsChanged(this);
				}
			}
		}

		public bool MessagesWithNoChannelsEnabled
		{
			get
			{
				return messagesWithNoChannelsEnabled;
			}

			set
			{
				if(messagesWithNoChannelsEnabled == value)
				{
					return;
				}

				messagesWithNoChannelsEnabled = value;

				if(OnEnabledChannelsChanged != null)
				{
					OnEnabledChannelsChanged(this);
				}
			}
		}

		public bool IgnoreUnlistedChannels
		{
			get
			{
				return ignoreUnlistedChannels;
			}

			set
			{
				ignoreUnlistedChannels = value;
			}
		}

		public int Count
		{
			get
			{
				return channels.Count;
			}
		}

		public Channels()
		{
			var colors = new Color32[] { new Color32(0, 128, 128, 255), Color.magenta, new Color32(128, 0, 0, 255), Color.green, new Color32(255, 165, 0, 255), new Color32(128, 0, 128, 255), Color.blue, Color.yellow, new Color32(165, 42, 42, 255) };
			int count = colors.Length;
			colorOptions = new string[count];
			for(int n = 0; n < count; n++)
			{
				colorOptions[n] = "#" + ColorUtility.ToHtmlStringRGB(colors[n]);
			}
		}

		/// <summary>
		/// Determines whether or not a channel by the provided name has been registered or not.
		/// <para>
		/// Channels can be registered in Project Settings and also also registed automatically
		/// on the fly when channel prefixes are detected in logged messages if
		/// 'Ignore Unlisted Channel Prefixes' is not enabled in project settings.
		/// </para>
		/// </summary>
		/// <param name="channelName"></param>
		/// <returns> <see langword="true"/> if channel exists; otherwise, <see langword="false"/>. </returns>
		public bool Exists([NotNull] string channelName) => channels.ContainsKey(channelName);

		public void SetChannelColor([NotNull]string channelName, Color color)
		{
			SetChannelColor(channelName, "#" + ColorUtility.ToHtmlStringRGB(color));
		}

		public string GetChannelColor([NotNull]string channelName)
		{
			return GetData(channelName).colorTag;
		}

		public string GetRichTextPrefix(string channelName)
		{
			return "<color=" + GetData(channelName).colorTag + ">[" + channelName + "]</color>";
		}

		public string GetRichTextPrefix(int channel)
		{
			string channelName = Get(channel);
			return "<color=" + GetData(channelName).colorTag + ">[" + channelName + "]</color>";
		}

		public void GetRichTextPrefix(string channelName, StringBuilder sb)
		{
			sb.Append("<color=");
			sb.Append(GetData(channelName).colorTag);
			sb.Append(">[");
			sb.Append(channelName);
			sb.Append("]</color>");
		}

		public void GetRichTextPrefix(int channel, StringBuilder sb)
		{
			string channelName = Get(channel);
			sb.Append("<color=");
			sb.Append(GetData(channelName).colorTag);
			sb.Append(">[");
			sb.Append(channelName);
			sb.Append("]</color>");
		}

		public void SetChannelColor([NotNull]string channelName, string colorTag)
		{
			ChannelData channel;
			if(channels.TryGetValue(channelName, out channel))
			{
				channel.colorTag = colorTag;
				return;
			}

			channels.Add(channelName, new ChannelData(channelName, ChannelEnabled.Default, colorTag));

			if(OnEnabledChannelsChanged != null)
			{
				OnEnabledChannelsChanged(this);
			}
		}

		public bool IsEnabled([NotNull]string channelName)
		{
			#if DEV_MODE
			UnityEngine.Debug.Assert(channelName != null);
			UnityEngine.Debug.Assert(channelName.Length > 0);
			UnityEngine.Debug.Assert(!channelName.StartsWith("["));
			UnityEngine.Debug.Assert(channelName.Equals(channelName.Trim(), StringComparison.InvariantCulture));
			#endif

			var channel = GetData(channelName);
			if(channel == null)
			{
				return MessagesWithNoChannelsEnabled;
			}
			return channel.IsEnabled(allChannelsEnabledByDefault);
		}

		public bool IsEitherEnabled(int channel1, int channel2)
		{
			if(channel1 == 0)
            {
				return allChannelsEnabledByDefault;
			}

			var channelData = GetData(Get(channel1));
			if(channelData == null)
			{
				return allChannelsEnabledByDefault;
			}

			if(channelData.IsEnabled(allChannelsEnabledByDefault))
            {
				return true;
            }

			if(channel2 == 0)
            {
				return false;
            }

			channelData = GetData(Get(channel2));
			return channelData != null && channelData.IsEnabled(allChannelsEnabledByDefault);
		}

		public bool IsEnabled(int channel)
		{
			if(channel == 0)
            {
				return allChannelsEnabledByDefault;
			}

			var channelData = GetData(Get(channel));
			if(channelData == null)
			{
				return allChannelsEnabledByDefault;
			}
			return channelData.IsEnabled(allChannelsEnabledByDefault);
		}

		public static string Get(int channel)
        {
			if(channel <= 0)
            {
				return "";
            }
			var channels = DebugLogExtensionsProjectSettings.Get().channels;
			int index = channel - 1;
			if(index >= channels.Length)
            {
				return channel.ToString();
            }
			return channels[index].id;
        }

		public ChannelEnabled GetForceEnabledState([NotNull]string channelName)
		{
			return GetData(channelName).enabled;
		}

		public bool ShouldShowMessageWithChannels([CanBeNull]IList<string> messageChannels)
		{
			if(messageChannels == null)
			{
				return messagesWithNoChannelsEnabled;
			}

			int count = messageChannels.Count;
			if(count == 0)
			{
				return messagesWithNoChannelsEnabled;
			}

			for(int n = count - 1; n >= 0; n--)
			{
				if(IsEnabled(messageChannels[n]))
				{
					return true;
				}
			}

			return false;
		}

		public void SetChannelEnabled([NotNull]string channelName, bool enabled)
		{
			if(enabled)
			{
				EnableChannel(GetData(channelName));
			}
			else
			{
				DisableChannel(GetData(channelName));
			}
		}

		/// <summary>
		/// Enables logging of messages on the given <paramref name="channelName">channel</paramref>.
		/// </summary>
		/// <param name="channelName"> Name of the channel to enable. </param>
		public void EnableChannel([NotNull]string channelName)
		{
			EnableChannel(GetData(channelName));
		}

		/// <summary>
		/// Enables logging of messages on the given <paramref name="channel"/>.
		/// </summary>
		/// <param name="channel"> Data for the channel to enable. </param>
		private void EnableChannel([NotNull]ChannelData channel)
		{
			bool wasEnabled = channel.IsEnabled(allChannelsEnabledByDefault);

			channel.enabled = ChannelEnabled.ForceEnabled;

			if(!wasEnabled && OnEnabledChannelsChanged != null)
			{
				OnEnabledChannelsChanged(this);
			}
		}

		/// <summary>
		/// Disables logging of messages on the given <paramref name="channelName">channel</paramref>.
		/// </summary>
		/// <param name="channelName"> Name of the channel to disable. </param>
		public void DisableChannel([NotNull]string channelName)
		{
			DisableChannel(GetData(channelName));
		}

		/// <summary>
		/// Disables logging of messages on the given <paramref name="channel"/>.
		/// </summary>
		/// <param name="channel"> Data for the channel to disable. </param>
		private void DisableChannel([NotNull]ChannelData channel)
		{
			bool wasEnabled = channel.IsEnabled(allChannelsEnabledByDefault);

			channel.enabled = ChannelEnabled.ForceDisabled;

			if(wasEnabled && OnEnabledChannelsChanged != null)
			{
				OnEnabledChannelsChanged(this);
			}
		}

		/// <summary>
		/// Adds the given <paramref name="channel"/> to the collection and selects a color that will be used for the channel prefix in logged messages.
		/// </summary>
		/// <param name="channel"> Name of the channel to register. </param>
		public void RegisterChannel(string channel)
		{
			GetData(channel);
		}

		[NotNull]
		private ChannelData GetData(string channelName)
		{
			#if DEV_MODE
			UnityEngine.Debug.Assert(channelName != null);
			UnityEngine.Debug.Assert(channelName.Length > 0);
			UnityEngine.Debug.Assert(!channelName.StartsWith("["));
			UnityEngine.Debug.Assert(channelName.Equals(channelName.Trim(), StringComparison.InvariantCulture));
			#endif

			ChannelData channel;
			if(channels.TryGetValue(channelName, out channel))
			{
				return channel;
			}

			channel = new ChannelData(channelName, ChannelEnabled.Default, NextChannelColor());
			channels.Add(channelName, channel);

			if(OnEnabledChannelsChanged != null)
			{
				OnEnabledChannelsChanged(this);
			}

			return channel;
		}

		public void SetDefaultChannelColors(string[] value)
		{
			colorOptions = value;
		}

		public string NextChannelColor()
		{
			var colorTag = colorOptions[nextColorIndex];

			nextColorIndex++;
			if(nextColorIndex >= colorOptions.Length)
			{
				nextColorIndex = 0;
			}

			return colorTag;
		}

		public void Clear()
		{
			channels.Clear();
			allChannelsEnabledByDefault = true;
			messagesWithNoChannelsEnabled = true;

			if(OnEnabledChannelsChanged != null)
			{
				OnEnabledChannelsChanged(this);
			}
		}

		public void ResetToDefaults()
		{
			channels.Clear();
			allChannelsEnabledByDefault = true;
			messagesWithNoChannelsEnabled = true;

			DebugLogExtensionsProjectSettings.Get().Apply();

			if(OnEnabledChannelsChanged != null)
			{
				OnEnabledChannelsChanged(this);
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			foreach(string key in channels.Keys)
			{
				yield return key;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return channels.Keys.GetEnumerator();
		}

		public void OnBeforeSerialize()
		{
			if(channels == null)
			{
				return;
			}

			int count = channels.Count;
			channelsDataSerialized = new ChannelSerializedData[count];
			int index = 0;
			foreach(var channel in channels.Values)
			{
				channelsDataSerialized[index] = new ChannelSerializedData() { name = channel.name, enabled = channel.enabled, colorTag = channel.colorTag };
				index++;
			}
		}

		public void OnAfterDeserialize()
		{
			if(channelsDataSerialized == null)
			{
				return;
			}

			channels.Clear();

			for(int n = 0, count = channelsDataSerialized.Length; n < count; n++)
			{
				var channel = channelsDataSerialized[n];
				channels[channel.name] = new ChannelData(channel.name, channel.enabled, channel.colorTag);
			}

			nextColorIndex = channels.Count % colorOptions.Length; 

			if(OnEnabledChannelsChanged != null)
			{
				OnEnabledChannelsChanged(this);
			}
		}

		[Serializable]
		private class ChannelData : IEquatable<string>
		{
			public readonly string name = "";
			public ChannelEnabled enabled = ChannelEnabled.Default;
			public string colorTag = "";

			public ChannelData()
			{
				name = "";
				enabled = ChannelEnabled.Default;
				colorTag = "";
			}

			public ChannelData(string channelName, ChannelEnabled channelEnabled, string channelColorTag)
			{
				name = channelName == null ? "" : channelName;
				enabled = channelEnabled;
				colorTag = channelColorTag;
			}

			public bool IsEnabled(bool enabledByDefault)
			{
				switch(enabled)
				{
					case ChannelEnabled.ForceDisabled:
						return false;
					case ChannelEnabled.ForceEnabled:
						return true;
					default:
						return enabledByDefault;
				}
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if(ReferenceEquals(obj, null))
				{
					return false;
				}

				var channel = obj as ChannelData;
				if(channel != null)
				{
					return name.Equals(channel.name, StringComparison.OrdinalIgnoreCase);
				}

				var channelName = obj as string;
				if(channelName != null)
				{
					return name.Equals(channelName);
				}

				return false;
			}

			public bool Equals(ChannelData other)
			{
				return !ReferenceEquals(other, null) && name.Equals(other.name);
			}

			public bool Equals(string channelName)
			{
				return name.Equals(channelName, StringComparison.OrdinalIgnoreCase);
			}

			public override string ToString()
			{
				return name;
			}
		}

		[Serializable]
		private class ChannelSerializedData
		{
			public string name;
			public ChannelEnabled enabled;
			public string colorTag;
		}
	}
}
