using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using JetBrains.Annotations;


namespace Sisus.Debugging
{
	public class MemberInfoValueDisplayer : IDisplayable, IUpdatable
	{
		public readonly MemberInfo visualizedMember;
		public readonly Func<object> valueGetter;

		private readonly DebugFormatter formatter;

		private object lastValue;
		private string text;

		private readonly StringBuilder sb = new StringBuilder(32);

		public bool WantsRepaint { get; set; }

		public MemberInfoValueDisplayer([CanBeNull]object memberOwner, MemberInfo visualizeMember, DebugFormatter setFormatter)
		{
			visualizedMember = visualizeMember;
			formatter = setFormatter;

			valueGetter = visualizedMember.GetValueDelegate(memberOwner);

			lastValue = valueGetter();
			UpdateText();
		}

		public bool TargetEquals(object other)
		{
			return visualizedMember == other as MemberInfo;
		}

		public void Draw(GUIStyle guiStyle)
		{
			GUILayout.Label(text, guiStyle);
		}

		public void Update()
		{
			var newValue = valueGetter();
			bool valueChanged;
			if(ReferenceEquals(newValue, null))
			{
				valueChanged = ReferenceEquals(lastValue, null);
			}
			else
			{
				valueChanged = !newValue.Equals(lastValue);
			}

			if(!valueChanged)
			{
				return;
			}

			#if UNITY_EDITOR
			if(newValue is Vector3 && lastValue is Vector3)
			{
				UnityEngine.Debug.DrawLine((Vector3)lastValue, (Vector3)newValue, Color.white, 5f);
			}
			#endif

			lastValue = newValue;

			UpdateText();
		}

		private void UpdateText()
		{
			sb.Append(visualizedMember.Name);
			sb.Append(formatter.NameValueSeparator);
			formatter.ToStringColorized(lastValue, sb, true);
			text = sb.ToString();
			sb.Length = 0;

			WantsRepaint = true;
		}

		public override bool Equals(object obj) => obj is MemberInfoValueDisplayer displayer && displayer.visualizedMember == visualizedMember;
		public override int GetHashCode() => visualizedMember.GetHashCode();
	}
}