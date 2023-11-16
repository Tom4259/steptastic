using System;
using System.Diagnostics;
using UnityEngine;

namespace Sisus.Debugging.Settings
{
	[Serializable]
	public struct KeyConfig
	{
		public const string ToggleViewKey = "DebugLogExtensions.ToggleView";

		[SerializeField]
		public string key;

		[SerializeField]
		public KeyCode keyCode;
		[SerializeField]
		public bool control;
		[SerializeField]
		public bool alt;
		[SerializeField]
		public bool shift;

		[NonSerialized]
		public bool useOverrides;
		[NonSerialized]
		public KeyCode keyCodeOverride;
		[NonSerialized]
		public bool controlOverride;
		[NonSerialized]
		public bool altOverride;
		[NonSerialized]
		public bool shiftOverride;

		public KeyCode KeyCode
		{
			get
			{
				return useOverrides ? keyCodeOverride : keyCode;
			}
		}

		public bool Control
		{
			get
			{
				return useOverrides ? controlOverride : control;
			}
		}

		public bool Alt
		{
			get
			{
				return useOverrides ? altOverride : alt;
			}
		}

		public bool Shift
		{
			get
			{
				return useOverrides ? shiftOverride : shift;
			}
		}

		[Conditional("UNITY_EDITOR")]
		public void SetKeyCodeOverride(KeyCode set)
		{
			SetUseOverrides();
			keyCodeOverride = set;
			Save();
		}

		[Conditional("UNITY_EDITOR")]
		public void SetControlOverride(bool set)
		{
			SetUseOverrides();
			controlOverride = set;
			Save();
		}

		[Conditional("UNITY_EDITOR")]
		public void SetAltOverride(bool set)
		{
			SetUseOverrides();
			altOverride = set;
			Save();
		}

		[Conditional("UNITY_EDITOR")]
		public void SetShiftOverride(bool set)
		{
			SetUseOverrides();
			shiftOverride = set;
			Save();
		}

		private void SetUseOverrides()
		{
			if(useOverrides)
			{
				return;
			}
			useOverrides = true;
			keyCodeOverride = KeyCode;
			controlOverride = control;
			altOverride = alt;
			shiftOverride = shift;
		}

		public KeyConfig(string prefsKey, KeyCode defaultKey, bool defaultControl = false, bool defaultAlt = false, bool defaultShift = false)
		{
			key = prefsKey;
			keyCode = defaultKey;

			control = defaultControl;
			alt = defaultAlt;
			shift = defaultShift;

			useOverrides = false;
			keyCodeOverride = defaultKey;
			controlOverride = defaultControl;
			altOverride = defaultAlt;
			shiftOverride = defaultShift;
		}

		public bool DetectInput(Event onGuiEvent)
		{
			if(onGuiEvent.type != EventType.KeyDown)
			{
				return false;
			}

			if(useOverrides)
			{
				return onGuiEvent.keyCode == keyCodeOverride && onGuiEvent.control == controlOverride && onGuiEvent.alt == altOverride && onGuiEvent.shift == shiftOverride;
			}

			return onGuiEvent.keyCode == keyCode && onGuiEvent.control == control && onGuiEvent.alt == alt && onGuiEvent.shift == shift;
		}

		public void Load()
		{
			if(PlayerPrefs.HasKey(ToggleViewKey))
			{
				useOverrides = true;
				keyCode = (KeyCode)PlayerPrefs.GetInt(ToggleViewKey);

				var modifiersKey = ToggleViewKey + "_modifiers";
				if(PlayerPrefs.HasKey(modifiersKey))
				{
					var modifiers = (EventModifiers)PlayerPrefs.GetInt(modifiersKey);
					control = modifiers.HasFlag(EventModifiers.Control);
					alt = modifiers.HasFlag(EventModifiers.Alt);
					shift = modifiers.HasFlag(EventModifiers.Shift);
				}
				else
				{
					controlOverride = false;
					altOverride = false;
					shiftOverride = false;
				}
			}
			else
			{
				var modifiersKey = ToggleViewKey + "_modifiers";
				if(PlayerPrefs.HasKey(modifiersKey))
				{
					useOverrides = true;
					keyCodeOverride = keyCode;

					var modifiers = (EventModifiers)PlayerPrefs.GetInt(modifiersKey);
					control = modifiers.HasFlag(EventModifiers.Control);
					alt = modifiers.HasFlag(EventModifiers.Alt);
					shift = modifiers.HasFlag(EventModifiers.Shift);
				}
				else
				{
					useOverrides = false;
				}
			}
		}

		[Conditional("UNITY_EDITOR")]
		public void Save()
		{
			if(!useOverrides)
			{
				return;
			}

			if(keyCodeOverride == KeyCode.None || keyCodeOverride == keyCode)
			{
				PlayerPrefs.DeleteKey(key);
				PlayerPrefs.DeleteKey(key + "_modifiers");
				return;
			}

			EventModifiers modifiers;
			if(controlOverride)
			{
				if(shiftOverride)
				{
					if(altOverride)
					{
						modifiers = EventModifiers.Control | EventModifiers.Shift | EventModifiers.Alt;
					}
					else
					{
						modifiers = EventModifiers.Control | EventModifiers.Shift;
					}
				}
				else if(altOverride)
				{
					modifiers = EventModifiers.Control | EventModifiers.Alt;
				}
				else
				{
					modifiers = EventModifiers.Control;
				}
			}
			else if(shiftOverride)
			{
				if(altOverride)
				{
					modifiers = EventModifiers.Shift | EventModifiers.Alt;
				}
				else
				{
					modifiers = EventModifiers.Shift;
				}
			}
			else if(altOverride)
			{
				modifiers = EventModifiers.Alt;
			}
			else
			{
				modifiers = EventModifiers.None;
			}

			PlayerPrefs.SetInt(key, (int)keyCodeOverride);
			PlayerPrefs.SetInt(key + "_modifiers", (int)modifiers);
		}
	}
}