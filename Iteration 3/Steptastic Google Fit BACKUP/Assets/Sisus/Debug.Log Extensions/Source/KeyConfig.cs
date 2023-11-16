using UnityEngine;

namespace Sisus.Debugging
{
	public struct KeyConfig
	{
		public readonly KeyCode keyCode;
		public readonly bool control;
		public readonly bool alt;
		public readonly bool shift;

		public KeyConfig(KeyCode defaultKey, bool defaultControl, bool defaultAlt, bool defaultShift)
		{
			keyCode = defaultKey;

			control = defaultControl;
			alt = defaultAlt;
			shift = defaultShift;
		}

		public bool DetectInput(Event onGuiEvent)
		{
			if(onGuiEvent.type != EventType.KeyDown)
			{
				return false;
			}

			return onGuiEvent.keyCode == keyCode && onGuiEvent.control == control && onGuiEvent.alt == alt && onGuiEvent.shift == shift;
		}
	}
}