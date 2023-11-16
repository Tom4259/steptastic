using System;
using System.Diagnostics;
using UnityEngine;

namespace Sisus.Debugging.Settings
{
	/// <summary>
	/// Represents a channel on which Console messages can be logged.
	/// </summary>
	[Serializable]
	public class DebugChannelInfo
	{
		/// <summary>
		/// The name that identifies the channel.
		/// </summary>
		public string id = "";

		/// <summary>
		/// The color used for the channel prefix in the Console for messages logged on this channel.
		/// </summary>
		public Color color = Color.white;

		/// <summary>
		/// Is this channel enabled for all users by default?
		/// <para>
		/// If <see langword="true"/> channel is enabled for all users unless they blacklist it.
		/// </para>
		/// <para>
		/// If <see langword="false"/> channel is disabled for all users unless they whitelist it.
		/// </para>
		/// </summary>
		public bool enabledByDefault = true;

		/// <summary>
		/// The color of the channel as a hexadecimal string in the format "RRGGBB" or an <see cref="string.Empty">empty string</see> if <see cref="HasColor"/> is <see langword="false"/>.
		/// </summary>
		[SerializeField, HideInInspector]
		public string colorText;

		/// <summary>
		/// Does this channel have a non-white <see cref="color"/>?
		/// <para>
		/// <see langword="true"/> if <see cref="color"/> equals <see cref="Color.white"/>; otherwise, <see langword="false"/>.
		/// </para>
		/// </summary>
		public bool HasColor
		{
			get
			{
				return color != Color.white;
			}
		}

		/// <summary>
		/// This should be called during the OnValidate event for all <see cref="DebugChannelInfo"/> fields on a <see cref="UnityEngine.Object"/>.
		/// </summary>
		[Conditional("UNITY_EDITOR")]
		public void OnValidate()
		{
			id = id.Replace("[", "").Replace("]", "");
			color.a = 1f;
			colorText = HasColor ? "" : ColorUtility.ToHtmlStringRGB(color);
		}
	}
}