using System;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus.Debugging
{
	public class MemberInfoValueTracker : IValueTracker
	{
		private static readonly StringBuilder sb = new StringBuilder();

		private readonly MemberInfo trackedMember;
		private readonly Func<object> valueGetter;
		private readonly Object context;
		private readonly bool pauseOnChanged;
		private readonly DebugFormatter formatter;

		private object lastValue;

		public MemberInfoValueTracker([CanBeNull]object memberOwner, MemberInfo visualizeMember, bool setPauseOnChanged, DebugFormatter setFormatter)
		{
			valueGetter = visualizeMember.GetValueDelegate(memberOwner);
			trackedMember = visualizeMember;
			context = memberOwner as Object;
			pauseOnChanged = setPauseOnChanged;
			formatter = setFormatter;

			lastValue = valueGetter();
		}

		public bool TargetEquals(object other)
		{
			return trackedMember == other as MemberInfo;
		}

		public void Update()
		{
			var newValue = valueGetter();
			if(ReferenceEquals(newValue, null))
			{
				if(ReferenceEquals(lastValue, null))
				{
					return;
				}
			}
			else if(newValue.Equals(lastValue))
			{
				return;
			}

			var unityObject = newValue as Object;
			if(unityObject == null)
			{
				unityObject = context;
			}

			sb.Append(trackedMember.Name);
			sb.Append(formatter.NameValueSeparator);
			formatter.ToStringColorized(newValue, sb, true);
			sb.Append("\n(was: ");
			formatter.ToStringColorized(lastValue, sb, true);
			sb.Append(")");
			string messageFormatted = formatter.Format(sb.ToString());
			sb.Length = 0;
			sb.Append(trackedMember.Name);
			sb.Append(formatter.NameValueSeparatorUnformatted);
			formatter.ToStringUncolorized(newValue, sb, true);
			sb.Append("\n(was: ");
			formatter.ToStringUncolorized(lastValue, sb, true);
			sb.Append(")");
			string messageUnformatted = sb.ToString();
			sb.Length = 0;

			Debug.LastMessageUnformatted = messageUnformatted;
			Debug.LastMessageContext = unityObject;
			UnityEngine.Debug.Log(messageFormatted, unityObject);

			lastValue = newValue;

			if(pauseOnChanged)
			{
				UnityEngine.Debug.Break();
			}
		}
	}
}