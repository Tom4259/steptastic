using UnityEngine;

namespace Sisus.Debugging
{
	/// <summary>
	/// Represents a channel on which Console messages can be logged.
	/// </summary>
	public sealed class DebugChannelInfo
	{
		/// <summary>
		/// The name that identifies the channel.
		/// </summary>
		public readonly string id = "";

		/// <summary>
		/// The color used for the channel prefix in the Console for messages logged on this channel.
		/// </summary>
		public readonly Color color = Color.white;

		/// <summary>
		/// Is this channel enabled for all users by default?
		/// <para>
		/// If <see langword="true"/> channel is enabled for all users unless they blacklist it.
		/// </para>
		/// <para>
		/// If <see langword="false"/> channel is disabled for all users unless they whitelist it.
		/// </para>
		/// </summary>
		public readonly bool enabledByDefault = true;

		/// <summary>
		/// The color of the channel as a hexadecimal string in the format "RRGGBB" or an <see cref="string.Empty">empty string</see> if <see cref="HasColor"/> is <see langword="false"/>.
		/// </summary>
		public readonly string colorText;

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

		public DebugChannelInfo(string id, Color color, bool enabledByDefault, string colorText)
		{
			this.id = id;
			this.color = color;
			this.enabledByDefault = enabledByDefault;
			this.colorText = colorText;
		}
	}
}